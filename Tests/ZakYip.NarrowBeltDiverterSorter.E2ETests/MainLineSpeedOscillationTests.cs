using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Execution.Mainline;
using ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Simulated;
using ZakYip.NarrowBeltDiverterSorter.Simulation;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;
using FakeMainLineDrivePortFromSim = ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes.FakeMainLineDrivePort;
using FakeMainLineFeedbackPortFromSim = ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes.FakeMainLineFeedbackPort;

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests;

/// <summary>
/// 测试主线速度振荡修复
/// </summary>
public class MainLineSpeedOscillationTests
{
    /// <summary>
    /// 测试：固定设定点情况下速度不应该振荡
    /// 固定 setpoint 为 1000 mm/s，运行若干个 tick
    /// 断言：一段时间内采样的速度列表没有在相邻采样之间出现"从 >500 mm/s 跳到 0 又跳回 >500"的模式
    /// </summary>
    [Fact]
    public async Task MainLine_WithFixedSetpoint_ShouldNotOscillate()
    {
        // Arrange
        const decimal targetSpeed = 1000m;
        const int numberOfTicks = 50; // 运行50个tick
        const int tickIntervalMs = 100; // 每个tick间隔100ms
        
        // 创建fake硬件
        var fakeMainLineDrive = new FakeMainLineDrivePortFromSim();
        var fakeMainLineFeedback = new FakeMainLineFeedbackPortFromSim(fakeMainLineDrive);
        
        // 创建设定点提供者
        var setpointProvider = new SimulationMainLineSetpoint();
        setpointProvider.SetSetpoint(true, targetSpeed);
        
        // 创建控制服务
        var controlOptions = Options.Create(new MainLineControlOptions
        {
            TargetSpeedMmps = targetSpeed,
            LoopPeriod = TimeSpan.FromMilliseconds(tickIntervalMs),
            StableDeadbandMmps = 50m,
            ProportionalGain = 0.5m,
            IntegralGain = 0.1m,
            DerivativeGain = 0.01m,
            IntegralLimit = 500m,
            MinOutputMmps = 0m,
            MaxOutputMmps = 3000m
        });
        
        var controlService = new MainLineControlService(
            NullLogger<MainLineControlService>.Instance,
            fakeMainLineDrive,
            fakeMainLineFeedback,
            controlOptions);
        
        // 启动控制服务
        await controlService.StartAsync();
        await fakeMainLineDrive.StartAsync();
        
        // 记录速度采样
        var speedSamples = new List<decimal>();
        
        // Act - 运行多个tick并采样速度
        for (int i = 0; i < numberOfTicks; i++)
        {
            // 从设定点读取目标速度并设置
            var setpoint = setpointProvider.IsEnabled ? setpointProvider.TargetMmps : 0m;
            controlService.SetTargetSpeed(setpoint);
            
            // 执行控制循环
            await controlService.ExecuteControlLoopAsync();
            
            // 采样当前速度
            var currentSpeed = (decimal)fakeMainLineFeedback.GetCurrentSpeed();
            speedSamples.Add(currentSpeed);
            
            // 等待下一个tick
            await Task.Delay(tickIntervalMs);
        }
        
        // Assert - 验证没有振荡
        // 振荡模式定义：相邻采样之间从 >500 mm/s 跳到 0 又跳回 >500 mm/s
        bool hasOscillation = false;
        const decimal oscillationThreshold = 500m;
        
        for (int i = 2; i < speedSamples.Count; i++)
        {
            var prev2 = speedSamples[i - 2];
            var prev1 = speedSamples[i - 1];
            var current = speedSamples[i];
            
            // 检测模式：高速 -> 0 -> 高速
            if (prev2 > oscillationThreshold && 
                prev1 == 0 && 
                current > oscillationThreshold)
            {
                hasOscillation = true;
                break;
            }
        }
        
        Assert.False(hasOscillation, 
            $"检测到速度振荡！速度序列: {string.Join(", ", speedSamples.Select(s => $"{s:F1}"))}");
        
        // 验证平均速度为非零正值
        var averageSpeed = speedSamples.Average();
        Assert.True(averageSpeed > 0, 
            $"平均速度应该为正值，实际值: {averageSpeed:F1} mm/s");
        
        // 验证速度最终稳定在目标附近（允许±100 mm/s的误差）
        var lastFewSamples = speedSamples.TakeLast(10).ToList();
        var finalAverageSpeed = lastFewSamples.Average();
        Assert.True(Math.Abs(finalAverageSpeed - targetSpeed) <= 100m,
            $"最终速度应该稳定在目标值附近。目标: {targetSpeed} mm/s, 实际平均: {finalAverageSpeed:F1} mm/s");
        
        // 停止
        await controlService.StopAsync();
    }
    
    /// <summary>
    /// 测试：FakeMainLineFeedbackPort应该提供一阶惯性（渐进式速度变化）
    /// </summary>
    [Fact]
    public async Task FakeMainLineFeedback_ShouldProvide_FirstOrderInertia()
    {
        // Arrange
        var fakeMainLineDrive = new FakeMainLineDrivePortFromSim();
        var fakeMainLineFeedback = new FakeMainLineFeedbackPortFromSim(fakeMainLineDrive);
        
        await fakeMainLineDrive.StartAsync();
        await fakeMainLineDrive.SetTargetSpeedAsync(1000.0);
        
        // Act - 采样速度变化
        var speedSamples = new List<decimal>();
        for (int i = 0; i < 20; i++)
        {
            var speed = (decimal)fakeMainLineFeedback.GetCurrentSpeed();
            speedSamples.Add(speed);
            await Task.Delay(50); // 每50ms采样一次
        }
        
        // Assert - 验证速度是渐进变化而非瞬间跳变
        // 第一个采样应该远小于目标速度（不是瞬间到达）
        Assert.True(speedSamples[0] < 500m, 
            $"初始速度应该远小于目标值，实际: {speedSamples[0]:F1} mm/s");
        
        // 速度应该单调递增（或最终稳定）
        bool isMonotonicIncreasing = true;
        for (int i = 1; i < speedSamples.Count; i++)
        {
            if (speedSamples[i] < speedSamples[i - 1] - 1) // 允许小的数值误差
            {
                isMonotonicIncreasing = false;
                break;
            }
        }
        
        Assert.True(isMonotonicIncreasing, 
            $"速度应该单调递增，速度序列: {string.Join(", ", speedSamples.Select(s => $"{s:F1}"))}");
        
        // 最后的速度应该接近目标值
        var finalSpeed = speedSamples.Last();
        Assert.True(Math.Abs(finalSpeed - 1000m) < 100m,
            $"最终速度应该接近目标值 1000 mm/s，实际: {finalSpeed:F1} mm/s");
    }
    
    /// <summary>
    /// 测试：当setpoint从启用变为禁用时，速度应该平滑降至0
    /// </summary>
    [Fact]
    public async Task MainLine_WhenSetpointDisabled_ShouldDecelerateSmooth()
    {
        // Arrange
        var fakeMainLineDrive = new FakeMainLineDrivePortFromSim();
        var fakeMainLineFeedback = new FakeMainLineFeedbackPortFromSim(fakeMainLineDrive);
        var setpointProvider = new SimulationMainLineSetpoint();
        
        var controlOptions = Options.Create(new MainLineControlOptions
        {
            TargetSpeedMmps = 1000m,
            LoopPeriod = TimeSpan.FromMilliseconds(100),
            StableDeadbandMmps = 50m
        });
        
        var controlService = new MainLineControlService(
            NullLogger<MainLineControlService>.Instance,
            fakeMainLineDrive,
            fakeMainLineFeedback,
            controlOptions);
        
        await controlService.StartAsync();
        await fakeMainLineDrive.StartAsync();
        
        // 先启用setpoint，让速度升到1000 mm/s
        setpointProvider.SetSetpoint(true, 1000m);
        
        for (int i = 0; i < 30; i++)
        {
            var setpoint = setpointProvider.IsEnabled ? setpointProvider.TargetMmps : 0m;
            controlService.SetTargetSpeed(setpoint);
            await controlService.ExecuteControlLoopAsync();
            await Task.Delay(100);
        }
        
        // 验证速度已经接近1000
        var speedBeforeDisable = (decimal)fakeMainLineFeedback.GetCurrentSpeed();
        Assert.True(speedBeforeDisable > 900m, 
            $"禁用前速度应该接近1000 mm/s，实际: {speedBeforeDisable:F1} mm/s");
        
        // Act - 禁用setpoint
        setpointProvider.SetSetpoint(false, 0m);
        
        var speedSamplesAfterDisable = new List<decimal>();
        for (int i = 0; i < 30; i++)
        {
            var setpoint = setpointProvider.IsEnabled ? setpointProvider.TargetMmps : 0m;
            controlService.SetTargetSpeed(setpoint);
            await controlService.ExecuteControlLoopAsync();
            
            var speed = (decimal)fakeMainLineFeedback.GetCurrentSpeed();
            speedSamplesAfterDisable.Add(speed);
            await Task.Delay(100);
        }
        
        // Assert - 验证速度平滑降至0
        // 速度应该单调递减（允许在接近0时有小幅波动）
        bool isMonotonicDecreasing = true;
        for (int i = 1; i < speedSamplesAfterDisable.Count; i++)
        {
            // 如果当前速度已经很小（<20 mm/s），允许小幅波动
            if (speedSamplesAfterDisable[i - 1] < 20m)
            {
                continue;
            }
            
            // 否则速度应该递减
            if (speedSamplesAfterDisable[i] > speedSamplesAfterDisable[i - 1] + 1)
            {
                isMonotonicDecreasing = false;
                break;
            }
        }
        
        Assert.True(isMonotonicDecreasing, 
            $"速度应该单调递减，速度序列: {string.Join(", ", speedSamplesAfterDisable.Select(s => $"{s:F1}"))}");
        
        // 最终速度应该接近0
        var finalSpeed = speedSamplesAfterDisable.Last();
        Assert.True(finalSpeed < 50m,
            $"最终速度应该接近0，实际: {finalSpeed:F1} mm/s");
    }
}
