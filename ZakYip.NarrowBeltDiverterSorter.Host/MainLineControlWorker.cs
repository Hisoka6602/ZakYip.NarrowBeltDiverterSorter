using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 主线控制工作器
/// 负责周期性调用控制服务并输出状态日志
/// </summary>
public class MainLineControlWorker : BackgroundService
{
    private readonly ILogger<MainLineControlWorker> _logger;
    private readonly IMainLineControlService _controlService;
    private readonly IMainLineDrive _mainLineDrive;
    private readonly IMainLineSetpointProvider _setpointProvider;
    private readonly MainLineControlOptions _options;
    private readonly bool _enableBringupLogging;

    public MainLineControlWorker(
        ILogger<MainLineControlWorker> logger,
        IMainLineControlService controlService,
        IMainLineDrive mainLineDrive,
        IMainLineSetpointProvider setpointProvider,
        IOptions<MainLineControlOptions> options,
        StartupModeConfiguration startupConfig)
    {
        _logger = logger;
        _controlService = controlService;
        _mainLineDrive = mainLineDrive;
        _setpointProvider = setpointProvider;
        _options = options.Value;
        _enableBringupLogging = startupConfig.EnableBringupLogging && 
                                startupConfig.Mode >= StartupMode.BringupMainline;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("主线控制工作器已启动");

        // 初始化主线驱动
        _logger.LogInformation("开始初始化主线驱动");
        var initialized = await _mainLineDrive.InitializeAsync(stoppingToken);
        if (!initialized)
        {
            _logger.LogError("主线驱动初始化失败，主线控制服务将不会启动");
            _logger.LogWarning("主线未就绪，系统无法进行正常分拣，请检查驱动连接和配置");
            return;
        }
        
        _logger.LogInformation("主线驱动初始化成功");

        // 启动控制服务
        var started = await _controlService.StartAsync(stoppingToken);
        if (!started)
        {
            _logger.LogError("主线控制服务启动失败");
            _logger.LogWarning("主线未就绪，系统无法进行正常分拣，请等待人工处理");
            return;
        }

        var loopPeriod = _options.LoopPeriod;
        var logCounter = 0;
        // Bring-up 模式：每秒输出，正常模式：每5秒输出
        var logInterval = _enableBringupLogging 
            ? (int)(TimeSpan.FromSeconds(1).TotalMilliseconds / loopPeriod.TotalMilliseconds)
            : (int)(TimeSpan.FromSeconds(5).TotalMilliseconds / loopPeriod.TotalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 读取设定点并更新目标速度
                var setpoint = _setpointProvider.IsEnabled ? _setpointProvider.TargetMmps : 0m;
                await _mainLineDrive.SetTargetSpeedAsync(setpoint, stoppingToken);

                // 执行控制循环
                var success = await _controlService.ExecuteControlLoopAsync(stoppingToken);
                
                if (!success && _controlService.IsRunning)
                {
                    _logger.LogWarning("控制循环执行失败");
                }

                // 定期输出状态日志（每5秒）
                logCounter++;
                if (logCounter >= logInterval)
                {
                    logCounter = 0;
                    LogCurrentStatus();
                }

                // 等待下一个控制周期
                await Task.Delay(loopPeriod, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 正常停止
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "主线控制循环发生异常");
                
                // 如果是通讯异常，标记主线未就绪（通过日志提醒）
                if (ex is TimeoutException || ex is System.IO.IOException || 
                    ex.Message.Contains("通讯") || ex.Message.Contains("Modbus"))
                {
                    _logger.LogWarning("主线通讯失败，主线未就绪，系统无法进行正常分拣");
                }
                
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        // 停止控制服务
        await _controlService.StopAsync(CancellationToken.None);
        
        // 安全关闭主线驱动
        _logger.LogInformation("开始安全关闭主线驱动");
        var shutdown = await _mainLineDrive.ShutdownAsync(CancellationToken.None);
        if (!shutdown)
        {
            _logger.LogWarning("主线驱动安全关闭失败");
        }
        
        _logger.LogInformation("主线控制工作器已停止");
    }

    /// <summary>
    /// 输出当前状态日志（中文）
    /// </summary>
    private void LogCurrentStatus()
    {
        var currentSpeed = _mainLineDrive.CurrentSpeedMmps;
        var targetSpeed = _mainLineDrive.TargetSpeedMmps;
        var isStable = _mainLineDrive.IsSpeedStable;

        if (_enableBringupLogging)
        {
            // Bring-up 模式：每秒输出当前目标速度、实际速度、IsSpeedStable
            _logger.LogInformation(
                "[主线状态] 目标速度: {TargetSpeed:F1} mm/s, 实际速度: {CurrentSpeed:F1} mm/s, 速度稳定: {IsStable}",
                targetSpeed, currentSpeed, isStable ? "是" : "否");
        }
        else
        {
            // 正常模式：保持原有日志格式
            if (isStable)
            {
                _logger.LogInformation(
                    "主线运行状态 - 当前速度: {CurrentSpeed:F1} mm/s, 目标速度: {TargetSpeed:F1} mm/s, 速度稳定: 是",
                    currentSpeed, targetSpeed);
            }
            else
            {
                _logger.LogInformation(
                    "主线运行状态 - 当前速度: {CurrentSpeed:F1} mm/s, 目标速度: {TargetSpeed:F1} mm/s, 速度稳定: 否",
                    currentSpeed, targetSpeed);
            }
        }
    }
}
