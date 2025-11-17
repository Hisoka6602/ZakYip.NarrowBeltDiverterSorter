using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine.Rema;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Tests.MainLine.Rema;

/// <summary>
/// 雷马 LM1000H 主线驱动单元测试
/// </summary>
public class RemaLm1000HMainLineDriveTests
{
    private readonly RemaLm1000HOptions _defaultOptions;

    public RemaLm1000HMainLineDriveTests()
    {
        // 创建默认配置
        _defaultOptions = new RemaLm1000HOptions
        {
            LoopPeriod = TimeSpan.FromMilliseconds(100),
            TorqueMax = 1000,
            TorqueMaxWhenOverLimit = 800,
            TorqueMaxWhenOverCurrent = 600,
            TorqueMaxUnderHighLoad = 700,
            LimitHz = 50.0m,
            LimitOvershootHz = 0.35m,
            StableDeadbandMmps = 10m,
            StableHold = TimeSpan.FromSeconds(2),
            UnstableThresholdMmps = 50m,
            UnstableHold = TimeSpan.FromSeconds(5),
            MicroBandMmps = 20m,
            TorqueSlewPerLoop = 15,
            Pid = new PidGains { Kp = 1.0m, Ki = 0.1m, Kd = 0.01m },
            PidIntegralClamp = 1000m,
            MinMmps = 0m,
            MaxMmps = 5000m,
            RatedCurrentScale = 1.0m,
            FallbackRatedCurrentA = 6.0m,
            CurrentLimitRatio = 1.2m,
            OverCurrentIntegralDecay = 0.5m,
            HighLoadRatio = 0.9m,
            HighLoadHold = TimeSpan.FromSeconds(10),
            LowSpeedBandMmps = 350m,
            FrictionCmd = 50m,
            LowSpeedKiBoost = 1.5m,
            StartMoveCmdFloor = 60
        };
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var logger = NullLogger<RemaLm1000HMainLineDrive>.Instance;
        var options = Options.Create(_defaultOptions);
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);

        // Act
        var drive = new RemaLm1000HMainLineDrive(logger, options, transport);

        // Assert
        Assert.NotNull(drive);
        Assert.Equal(0m, drive.CurrentSpeedMmps);
        Assert.Equal(0m, drive.TargetSpeedMmps);
        Assert.False(drive.IsSpeedStable);
    }

    [Theory]
    [InlineData(1000, 10.0)]   // 1000 mm/s = 10 Hz
    [InlineData(1500, 15.0)]   // 1500 mm/s = 15 Hz
    [InlineData(2000, 20.0)]   // 2000 mm/s = 20 Hz
    [InlineData(500, 5.0)]     // 500 mm/s = 5 Hz
    [InlineData(0, 0.0)]       // 0 mm/s = 0 Hz
    public async Task SetTargetSpeedAsync_ConvertsSpeedCorrectly(decimal mmps, decimal expectedHz)
    {
        // Arrange
        var logger = NullLogger<RemaLm1000HMainLineDrive>.Instance;
        var options = Options.Create(_defaultOptions);
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var drive = new RemaLm1000HMainLineDrive(logger, options, transport);

        // Act
        await drive.SetTargetSpeedAsync(mmps);

        // Assert
        Assert.Equal(mmps, drive.TargetSpeedMmps);
        
        // 验证寄存器值
        var registerValue = await transport.ReadRegisterAsync(RemaRegisters.P0_07_LimitFrequency);
        var actualHz = registerValue * RemaScaling.P005_HzPerCount;
        Assert.Equal(expectedHz, actualHz);
    }

    [Theory]
    [InlineData(-100)]  // 负数
    [InlineData(6000)]  // 超过上限
    public async Task SetTargetSpeedAsync_ClampsSpeedToValidRange(decimal invalidMmps)
    {
        // Arrange
        var logger = NullLogger<RemaLm1000HMainLineDrive>.Instance;
        var options = Options.Create(_defaultOptions);
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var drive = new RemaLm1000HMainLineDrive(logger, options, transport);

        // Act
        await drive.SetTargetSpeedAsync(invalidMmps);

        // Assert
        var targetSpeed = drive.TargetSpeedMmps;
        Assert.InRange(targetSpeed, _defaultOptions.MinMmps, _defaultOptions.MaxMmps);
    }

    [Fact]
    public async Task StartAsync_InitializesRegistersCorrectly()
    {
        // Arrange
        var logger = NullLogger<RemaLm1000HMainLineDrive>.Instance;
        var options = Options.Create(_defaultOptions);
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var drive = new RemaLm1000HMainLineDrive(logger, options, transport);

        // Act
        await drive.InitializeAsync(); // Initialize first
        await drive.StartAsync();

        // Assert
        // 验证 P0.01 - 运行命令源选择 (RS485 通讯 = 2)
        var runCmdSource = await transport.ReadRegisterAsync(RemaRegisters.P0_01_RunCmdSource);
        Assert.Equal(2, runCmdSource);

        // 验证 P0.07 - 限速频率
        var limitFreq = await transport.ReadRegisterAsync(RemaRegisters.P0_07_LimitFrequency);
        var expectedLimitRegister = (ushort)Math.Round(_defaultOptions.LimitHz / RemaScaling.P005_HzPerCount);
        Assert.Equal(expectedLimitRegister, limitFreq);

        // 验证 P3.10 - 转矩给定值
        var torqueRef = await transport.ReadRegisterAsync(RemaRegisters.P3_10_TorqueRef);
        Assert.Equal(_defaultOptions.TorqueMax, torqueRef);

        // 验证控制字 - 正转运行
        var controlWord = await transport.ReadRegisterAsync(RemaRegisters.ControlWord);
        Assert.Equal(RemaScaling.ControlCmd_Forward, controlWord);
        
        // Cleanup
        await drive.ShutdownAsync(); // Use ShutdownAsync instead of StopAsync
        drive.Dispose();
    }

    [Fact]
    public async Task StopAsync_SetsSafeSpeedAndStopsMotor()
    {
        // Arrange
        var logger = NullLogger<RemaLm1000HMainLineDrive>.Instance;
        var options = Options.Create(_defaultOptions);
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var drive = new RemaLm1000HMainLineDrive(logger, options, transport);

        await drive.StartAsync();

        // Act
        await drive.StopAsync();

        // Assert
        // 验证 P0.07 设置为 0 Hz
        var limitFreq = await transport.ReadRegisterAsync(RemaRegisters.P0_07_LimitFrequency);
        Assert.Equal(0, limitFreq);

        // 验证控制字 - 减速停机
        var controlWord = await transport.ReadRegisterAsync(RemaRegisters.ControlWord);
        Assert.Equal(RemaScaling.ControlCmd_Decelerate, controlWord);
        
        // Cleanup
        drive.Dispose();
    }

    [Fact]
    public async Task SpeedStabilityDetection_WorksAsExpected()
    {
        // Arrange
        var options = new RemaLm1000HOptions
        {
            LoopPeriod = TimeSpan.FromMilliseconds(50), // 更快的循环周期用于测试
            TorqueMax = 1000,
            TorqueMaxWhenOverLimit = 800,
            TorqueMaxWhenOverCurrent = 600,
            TorqueMaxUnderHighLoad = 700,
            LimitHz = 50.0m,
            LimitOvershootHz = 0.35m,
            StableDeadbandMmps = 10m,
            StableHold = TimeSpan.FromMilliseconds(200), // 更短的稳定保持时间用于测试
            UnstableThresholdMmps = 50m,
            UnstableHold = TimeSpan.FromSeconds(5),
            MicroBandMmps = 20m,
            TorqueSlewPerLoop = 15,
            Pid = new PidGains { Kp = 1.0m, Ki = 0.1m, Kd = 0.01m },
            PidIntegralClamp = 1000m,
            MinMmps = 0m,
            MaxMmps = 5000m,
            RatedCurrentScale = 1.0m,
            FallbackRatedCurrentA = 6.0m,
            CurrentLimitRatio = 1.2m,
            OverCurrentIntegralDecay = 0.5m,
            HighLoadRatio = 0.9m,
            HighLoadHold = TimeSpan.FromSeconds(10),
            LowSpeedBandMmps = 350m,
            FrictionCmd = 50m,
            LowSpeedKiBoost = 1.5m,
            StartMoveCmdFloor = 60
        };
        
        var logger = NullLogger<RemaLm1000HMainLineDrive>.Instance;
        var optionsWrapper = Options.Create(options);
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var drive = new RemaLm1000HMainLineDrive(logger, optionsWrapper, transport);

        // 模拟速度达到目标
        var targetMmps = 1000m;
        var targetHz = targetMmps * RemaScaling.MmpsToHz; // 10 Hz
        var targetRegisterValue = (ushort)Math.Round(targetHz / RemaScaling.C026_HzPerCount);
        
        // 写入模拟的编码器反馈频率
        await transport.WriteRegisterAsync(RemaRegisters.C0_26_EncoderFrequency, targetRegisterValue);

        // Act
        await drive.StartAsync();
        await drive.SetTargetSpeedAsync(targetMmps);

        // 等待足够的时间让控制循环运行并检测稳定性
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Assert
        // 验证速度稳定性最终会被检测到
        var isStable = drive.IsSpeedStable;
        Assert.True(isStable, "速度应该在稳定保持时间后被检测为稳定");

        // Cleanup
        await drive.StopAsync();
        drive.Dispose();
    }

    [Fact]
    public void MmpsToHzConversion_IsCorrect()
    {
        // Test the conversion formula: mm/s × 0.01 = Hz
        // Example: 1000 mm/s = 10 Hz
        Assert.Equal(10.0m, 1000m * RemaScaling.MmpsToHz);
        Assert.Equal(15.0m, 1500m * RemaScaling.MmpsToHz);
        Assert.Equal(20.0m, 2000m * RemaScaling.MmpsToHz);
    }

    [Fact]
    public void HzToMmpsConversion_IsCorrect()
    {
        // Test the conversion formula: Hz × 100 = mm/s
        // Example: 10 Hz = 1000 mm/s
        Assert.Equal(1000m, 10.0m * RemaScaling.HzToMmps);
        Assert.Equal(1500m, 15.0m * RemaScaling.HzToMmps);
        Assert.Equal(2000m, 20.0m * RemaScaling.HzToMmps);
    }

    [Fact]
    public void RegisterValueConversion_IsCorrect()
    {
        // Test register value conversion: register × 0.01 = Hz
        // Example: register 1000 = 10.00 Hz
        Assert.Equal(10.00m, 1000 * RemaScaling.P005_HzPerCount);
        Assert.Equal(15.00m, 1500 * RemaScaling.C026_HzPerCount);
        Assert.Equal(20.00m, 2000 * RemaScaling.P005_HzPerCount);
    }

    [Fact]
    public async Task GetCurrentSpeedAsync_ReadsFromC026AndConvertsToMmps()
    {
        // Arrange
        var logger = NullLogger<RemaLm1000HMainLineDrive>.Instance;
        var options = Options.Create(_defaultOptions);
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var drive = new RemaLm1000HMainLineDrive(logger, options, transport);

        // 模拟编码器反馈：1500 mm/s = 15 Hz = 1500 register value
        var targetHz = 15.0m;
        var targetRegisterValue = (ushort)Math.Round(targetHz / RemaScaling.C026_HzPerCount);
        await transport.WriteRegisterAsync(RemaRegisters.C0_26_EncoderFrequency, targetRegisterValue);

        // Act
        var currentSpeed = await drive.GetCurrentSpeedAsync();

        // Assert
        Assert.Equal(1500m, currentSpeed);
        Assert.True(drive.IsFeedbackAvailable);
        
        // Cleanup
        drive.Dispose();
    }

    [Fact]
    public async Task GetCurrentSpeedAsync_OnReadFailure_MarksFeedbackUnavailableAfterThreshold()
    {
        // Arrange
        var logger = NullLogger<RemaLm1000HMainLineDrive>.Instance;
        var options = Options.Create(_defaultOptions);
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var drive = new RemaLm1000HMainLineDrive(logger, options, transport);

        // 模拟连续读取失败
        transport.SimulateReadFailure = true;

        // Act - 连续读取 6 次（超过阈值 5）
        for (int i = 0; i < 6; i++)
        {
            try
            {
                await drive.GetCurrentSpeedAsync();
            }
            catch
            {
                // 忽略异常
            }
        }

        // Assert - 反馈应该被标记为不可用
        Assert.False(drive.IsFeedbackAvailable);

        // Act - 恢复读取
        transport.SimulateReadFailure = false;
        await transport.WriteRegisterAsync(RemaRegisters.C0_26_EncoderFrequency, 1000);
        var speed = await drive.GetCurrentSpeedAsync();

        // Assert - 反馈应该恢复可用
        Assert.True(drive.IsFeedbackAvailable);
        Assert.Equal(1000m, speed);
        
        // Cleanup
        drive.Dispose();
    }

    [Fact]
    public async Task ControlLoop_OnReadFailure_MarksFeedbackUnavailableAfterThreshold()
    {
        // Arrange
        var options = new RemaLm1000HOptions
        {
            LoopPeriod = TimeSpan.FromMilliseconds(50), // 快速循环用于测试
            TorqueMax = 1000,
            TorqueMaxWhenOverLimit = 800,
            TorqueMaxWhenOverCurrent = 600,
            TorqueMaxUnderHighLoad = 700,
            LimitHz = 50.0m,
            LimitOvershootHz = 0.35m,
            StableDeadbandMmps = 10m,
            StableHold = TimeSpan.FromMilliseconds(100),
            UnstableThresholdMmps = 50m,
            UnstableHold = TimeSpan.FromSeconds(5),
            MicroBandMmps = 20m,
            TorqueSlewPerLoop = 15,
            Pid = new PidGains { Kp = 1.0m, Ki = 0.1m, Kd = 0.01m },
            PidIntegralClamp = 1000m,
            MinMmps = 0m,
            MaxMmps = 5000m,
            RatedCurrentScale = 1.0m,
            FallbackRatedCurrentA = 6.0m,
            CurrentLimitRatio = 1.2m,
            OverCurrentIntegralDecay = 0.5m,
            HighLoadRatio = 0.9m,
            HighLoadHold = TimeSpan.FromSeconds(10),
            LowSpeedBandMmps = 350m,
            FrictionCmd = 50m,
            LowSpeedKiBoost = 1.5m,
            StartMoveCmdFloor = 60
        };
        
        var logger = NullLogger<RemaLm1000HMainLineDrive>.Instance;
        var optionsWrapper = Options.Create(options);
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var drive = new RemaLm1000HMainLineDrive(logger, optionsWrapper, transport);

        // 模拟读取失败
        transport.SimulateReadFailure = true;

        // Act - 启动驱动，控制循环会尝试读取
        await drive.StartAsync();
        await drive.SetTargetSpeedAsync(1000m);

        // 等待足够的循环来触发失败阈值（5次失败）
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        // Assert - 反馈应该被标记为不可用
        Assert.False(drive.IsFeedbackAvailable);

        // Cleanup
        await drive.StopAsync();
        drive.Dispose();
    }
}
