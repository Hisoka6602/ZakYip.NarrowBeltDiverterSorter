using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Execution.Mainline;
using ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Simulated;
using ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Rema;

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
    private readonly MainLineDriveOptions _driveOptions;

    public MainLineControlWorker(
        ILogger<MainLineControlWorker> logger,
        IMainLineControlService controlService,
        IMainLineDrive mainLineDrive,
        IMainLineSetpointProvider setpointProvider,
        IOptions<MainLineControlOptions> options,
        IOptions<MainLineDriveOptions> driveOptions,
        StartupModeConfiguration startupConfig)
    {
        _logger = logger;
        _controlService = controlService;
        _mainLineDrive = mainLineDrive;
        _setpointProvider = setpointProvider;
        _options = options.Value;
        _driveOptions = driveOptions.Value;
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
            
            // 如果是 Rema 驱动，输出额外的诊断信息
            if (_driveOptions.Implementation == MainLineDriveImplementation.RemaLm1000H &&
                _mainLineDrive is RemaLm1000HMainLineDrive remaDrive)
            {
                LogRemaDiagnosticInfo(remaDrive);
            }
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
    
    /// <summary>
    /// 输出 Rema 驱动的诊断信息（Bring-up 模式专用）
    /// </summary>
    private void LogRemaDiagnosticInfo(RemaLm1000HMainLineDrive remaDrive)
    {
        // 输出串口配置和站号
        if (_driveOptions.Rema != null)
        {
            _logger.LogInformation(
                "[Rema 连接] 串口: {PortName}, 波特率: {BaudRate}, 站号: {SlaveAddress}",
                _driveOptions.Rema.PortName,
                _driveOptions.Rema.BaudRate,
                _driveOptions.Rema.SlaveAddress);
        }
        
        // 输出最近一次成功下发的目标速度
        var lastSuccessfulSpeed = remaDrive.LastSuccessfulTargetSpeedMmps;
        var lastSetTime = remaDrive.LastSuccessfulSpeedSetTime;
        if (lastSetTime != DateTime.MinValue)
        {
            var elapsed = DateTime.UtcNow - lastSetTime;
            _logger.LogInformation(
                "[Rema 命令] 最后成功下发速度: {LastSpeed:F1} mm/s ({ElapsedSeconds:F1}秒前)",
                lastSuccessfulSpeed,
                elapsed.TotalSeconds);
        }
        else
        {
            _logger.LogInformation("[Rema 命令] 尚未成功下发速度命令");
        }
        
        // 输出 C0.26 反馈频率和换算后的线速度
        var encoderFreqRaw = remaDrive.LastEncoderFreqRegisterValue;
        var encoderFreqHz = remaDrive.LastEncoderFreqHz;
        var feedbackAvailable = remaDrive.IsFeedbackAvailable;
        
        if (feedbackAvailable)
        {
            _logger.LogInformation(
                "[Rema 反馈] C0.26 寄存器值: {RegisterValue}, 反馈频率: {FreqHz:F2} Hz, 换算线速: {CurrentSpeed:F1} mm/s",
                encoderFreqRaw,
                encoderFreqHz,
                remaDrive.CurrentSpeedMmps);
        }
        else
        {
            _logger.LogWarning("[Rema 反馈] 速度反馈不可用（通讯失败）");
        }
    }
}
