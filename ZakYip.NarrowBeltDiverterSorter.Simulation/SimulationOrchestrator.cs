using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
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
    private readonly FakeMainLineDrivePort _mainLineDrive;
    private readonly FakeInfeedConveyorPort _infeedConveyor;
    private readonly FakeInfeedSensorPort _infeedSensor;
    private readonly FakeFieldBusClient _fieldBus;
    private readonly OriginSensorMonitor _originMonitor;
    private readonly InfeedSensorMonitor _infeedMonitor;
    private readonly ILogger<SimulationOrchestrator> _logger;

    public SimulationOrchestrator(
        SimulationConfiguration config,
        FakeMainLineDrivePort mainLineDrive,
        FakeInfeedConveyorPort infeedConveyor,
        FakeInfeedSensorPort infeedSensor,
        FakeFieldBusClient fieldBus,
        OriginSensorMonitor originMonitor,
        InfeedSensorMonitor infeedMonitor,
        ILogger<SimulationOrchestrator> logger)
    {
        _config = config;
        _mainLineDrive = mainLineDrive;
        _infeedConveyor = infeedConveyor;
        _infeedSensor = infeedSensor;
        _fieldBus = fieldBus;
        _originMonitor = originMonitor;
        _infeedMonitor = infeedMonitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("仿真编排器已启动");

        try
        {
            // 1. 连接现场总线
            Console.WriteLine("[仿真启动] 步骤 1/6: 连接现场总线...");
            await _fieldBus.ConnectAsync(stoppingToken);
            await Task.Delay(500, stoppingToken);

            // 2. 启动入口输送线
            Console.WriteLine("[仿真启动] 步骤 2/6: 启动入口输送线...");
            await _infeedConveyor.SetSpeedAsync(_config.InfeedConveyorSpeedMmPerSec, stoppingToken);
            await _infeedConveyor.StartAsync(stoppingToken);
            await Task.Delay(500, stoppingToken);

            // 3. 启动入口传感器监听
            Console.WriteLine("[仿真启动] 步骤 3/6: 启动入口传感器监听...");
            await _infeedSensor.StartMonitoringAsync(stoppingToken);
            await _infeedMonitor.StartAsync(stoppingToken);
            await Task.Delay(500, stoppingToken);

            // 4. 启动原点传感器监听
            Console.WriteLine("[仿真启动] 步骤 4/6: 启动原点传感器监听...");
            _originMonitor.Start();
            await Task.Delay(500, stoppingToken);

            // 5. 启动主线
            Console.WriteLine("[仿真启动] 步骤 5/6: 设置主线速度并启动...");
            await _mainLineDrive.SetTargetSpeedAsync(_config.MainLineSpeedMmPerSec, stoppingToken);
            await _mainLineDrive.StartAsync(stoppingToken);
            await Task.Delay(500, stoppingToken);

            // 6. 系统就绪
            Console.WriteLine("[仿真启动] 步骤 6/6: 系统就绪\n");
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
        
        await _mainLineDrive.StopAsync();
        await _infeedConveyor.StopAsync();
        await _infeedSensor.StopMonitoringAsync();
        await _originMonitor.StopAsync();
        await _infeedMonitor.StopAsync();
        await _fieldBus.DisconnectAsync();
        
        Console.WriteLine("[仿真停止] 系统已停止");
    }
}
