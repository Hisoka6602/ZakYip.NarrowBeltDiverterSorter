using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Infeed;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Origin;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 仿真编排器
/// 协调整个仿真系统的启动和运行
/// </summary>
public class SimulationOrchestrator : BackgroundService
{
    private readonly SimulationConfiguration _config;
    private readonly IMainLineDrive _mainLineDrive;
    private readonly FakeMainLineDrivePort _fakeMainLineDrivePort;
    private readonly FakeInfeedConveyorPort _infeedConveyor;
    private readonly FakeInfeedSensorPort _infeedSensor;
    private readonly FakeFieldBusClient _fieldBus;
    private readonly OriginSensorMonitor _originMonitor;
    private readonly InfeedSensorMonitor _infeedMonitor;
    private readonly ICartRingBuilder _cartRingBuilder;
    private readonly ICartPositionTracker _cartPositionTracker;
    private readonly IMainLineControlService _mainLineControl;
    private readonly SimulationMainLineSetpoint _setpointProvider;
    private readonly IChuteIoService? _chuteIoService;
    private readonly ILogger<SimulationOrchestrator> _logger;

    public SimulationOrchestrator(
        SimulationConfiguration config,
        IMainLineDrive mainLineDrive,
        FakeMainLineDrivePort fakeMainLineDrivePort,
        FakeInfeedConveyorPort infeedConveyor,
        FakeInfeedSensorPort infeedSensor,
        FakeFieldBusClient fieldBus,
        OriginSensorMonitor originMonitor,
        InfeedSensorMonitor infeedMonitor,
        ICartRingBuilder cartRingBuilder,
        ICartPositionTracker cartPositionTracker,
        IMainLineControlService mainLineControl,
        SimulationMainLineSetpoint setpointProvider,
        ILogger<SimulationOrchestrator> logger,
        IChuteIoService? chuteIoService = null)
    {
        _config = config;
        _mainLineDrive = mainLineDrive;
        _fakeMainLineDrivePort = fakeMainLineDrivePort;
        _infeedConveyor = infeedConveyor;
        _infeedSensor = infeedSensor;
        _fieldBus = fieldBus;
        _originMonitor = originMonitor;
        _infeedMonitor = infeedMonitor;
        _cartRingBuilder = cartRingBuilder;
        _cartPositionTracker = cartPositionTracker;
        _mainLineControl = mainLineControl;
        _setpointProvider = setpointProvider;
        _chuteIoService = chuteIoService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("仿真编排器已启动");

        try
        {
            // 1. 连接现场总线
            Console.WriteLine("[仿真启动] 步骤 1/7: 连接现场总线...");
            await _fieldBus.ConnectAsync(stoppingToken);
            await Task.Delay(500, stoppingToken);

            // 2. 启动入口输送线
            Console.WriteLine("[仿真启动] 步骤 2/7: 启动入口输送线...");
            await _infeedConveyor.SetSpeedAsync(_config.InfeedConveyorSpeedMmPerSec, stoppingToken);
            await _infeedConveyor.StartAsync(stoppingToken);
            await Task.Delay(500, stoppingToken);

            // 3. 启动入口传感器监听
            Console.WriteLine("[仿真启动] 步骤 3/7: 启动入口传感器监听...");
            await _infeedSensor.StartMonitoringAsync(stoppingToken);
            await _infeedMonitor.StartAsync(stoppingToken);
            await Task.Delay(500, stoppingToken);

            // 4. 启动原点传感器监听
            Console.WriteLine("[仿真启动] 步骤 4/7: 启动原点传感器监听...");
            _originMonitor.Start();
            await Task.Delay(500, stoppingToken);

            // 5. 启动主线并等待速度稳定
            Console.WriteLine("[仿真启动] 步骤 5/7: 设置主线速度并启动...");
            _setpointProvider.SetSetpoint(true, (decimal)_config.MainLineSpeedMmPerSec);
            await _fakeMainLineDrivePort.StartAsync(stoppingToken);
            
            // 等待主线速度稳定
            Console.WriteLine("[仿真预热] 等待主线速度稳定...");
            await WaitForMainLineStableAsync(stoppingToken);
            _logger.LogInformation("主线已启动并稳定，当前速度: {Speed:F1} mm/s", _mainLineDrive.CurrentSpeedMmps);

            // 6. 等待小车环构建完成
            Console.WriteLine("[仿真启动] 步骤 6/7: 等待小车环构建完成...");
            var warmupStart = DateTimeOffset.UtcNow;
            await WaitForCartRingReadyAsync(stoppingToken);
            var warmupDuration = (DateTimeOffset.UtcNow - warmupStart).TotalSeconds;
            
            var snapshot = _cartRingBuilder.CurrentSnapshot;
            if (snapshot != null)
            {
                _logger.LogInformation(
                    "[CartRing] 小车环已就绪，长度={CartCount}，节距={SpacingMm}mm",
                    snapshot.RingLength.Value,
                    _config.CartSpacingMm);
                _logger.LogInformation("[Simulation] 小车环预热完成，耗时 {WarmupDuration:F2} 秒", warmupDuration);
                
                Console.WriteLine($"[仿真预热] 小车环已就绪 - 小车数量: {snapshot.RingLength.Value}, 耗时: {warmupDuration:F2}秒");
            }

            // 7. 系统就绪
            Console.WriteLine("[仿真启动] 步骤 7/7: 系统就绪\n");
            Console.WriteLine("════════════════════════════════════════");
            Console.WriteLine("  仿真系统运行中...");
            Console.WriteLine("════════════════════════════════════════\n");

            // 等待仿真结束或取消
            if (_config.SimulationDurationSeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.SimulationDurationSeconds + 5), stoppingToken);
                
                Console.WriteLine("\n════════════════════════════════════════");
                Console.WriteLine("  仿真结束");
                Console.WriteLine("════════════════════════════════════════");
                
                // 停止系统
                await StopSystemAsync();
            }
            else
            {
                // 无限运行，等待取消
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("仿真被取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "仿真编排过程中发生错误");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopSystemAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task StopSystemAsync()
    {
        Console.WriteLine("\n[仿真停止] 正在停止系统...");
        
        // 关闭所有格口（安全策略）
        if (_chuteIoService != null)
        {
            Console.WriteLine("[仿真停止] 关闭所有格口...");
            _logger.LogInformation("仿真停止: 调用 IChuteIoService.CloseAllAsync");
            try
            {
                await _chuteIoService.CloseAllAsync();
                Console.WriteLine("[仿真停止] 已关闭所有格口");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "仿真停止: 关闭所有格口时发生异常");
            }
        }
        
        _setpointProvider.SetSetpoint(false, 0);
        await _fakeMainLineDrivePort.StopAsync();
        await _infeedConveyor.StopAsync();
        await _infeedSensor.StopMonitoringAsync();
        await _originMonitor.StopAsync();
        await _infeedMonitor.StopAsync();
        await _fieldBus.DisconnectAsync();
        
        Console.WriteLine("[仿真停止] 系统已停止");
    }

    /// <summary>
    /// 等待主线控制启动并速度稳定
    /// </summary>
    private async Task WaitForMainLineStableAsync(CancellationToken cancellationToken)
    {
        const int maxWaitSeconds = 10;
        var timeout = DateTimeOffset.UtcNow.AddSeconds(maxWaitSeconds);

        while (DateTimeOffset.UtcNow < timeout && !cancellationToken.IsCancellationRequested)
        {
            if (_mainLineControl.IsRunning && _mainLineDrive.IsSpeedStable)
            {
                return;
            }

            await Task.Delay(100, cancellationToken);
        }

        _logger.LogWarning("主线未能在 {MaxWaitSeconds} 秒内稳定，继续执行", maxWaitSeconds);
    }

    /// <summary>
    /// 等待小车环构建完成
    /// </summary>
    private async Task WaitForCartRingReadyAsync(CancellationToken cancellationToken)
    {
        const int maxWaitSeconds = 30;
        var timeout = DateTimeOffset.UtcNow.AddSeconds(maxWaitSeconds);
        var lastLogTime = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow < timeout && !cancellationToken.IsCancellationRequested)
        {
            // 检查小车环是否已构建完成
            var snapshot = _cartRingBuilder.CurrentSnapshot;
            if (snapshot != null && _cartPositionTracker.IsInitialized)
            {
                return;
            }

            // 每5秒输出一次等待日志
            if ((DateTimeOffset.UtcNow - lastLogTime).TotalSeconds >= 5)
            {
                _logger.LogDebug(
                    "等待小车环就绪... (快照: {HasSnapshot}, 跟踪器初始化: {IsInitialized})",
                    snapshot != null,
                    _cartPositionTracker.IsInitialized);
                lastLogTime = DateTimeOffset.UtcNow;
            }

            await Task.Delay(200, cancellationToken);
        }

        _logger.LogWarning("小车环未能在 {MaxWaitSeconds} 秒内完成构建", maxWaitSeconds);
    }
}
