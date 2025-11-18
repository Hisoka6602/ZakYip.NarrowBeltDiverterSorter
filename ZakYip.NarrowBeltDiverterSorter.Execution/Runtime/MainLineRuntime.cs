using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;
using ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Rema;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Runtime;

/// <summary>
/// 主线控制运行时实现
/// 负责周期性调用控制服务并输出状态日志
/// </summary>
public class MainLineRuntime : IMainLineRuntime
{
    private readonly ILogger<MainLineRuntime> _logger;
    private readonly IMainLineControlService _controlService;
    private readonly IMainLineDrive _mainLineDrive;
    private readonly IMainLineSetpointProvider _setpointProvider;
    private readonly ISystemRunStateService _systemRunStateService;
    private readonly MainLineControlOptions _options;
    private readonly bool _enableBringupLogging;

    public MainLineRuntime(
        ILogger<MainLineRuntime> logger,
        IMainLineControlService controlService,
        IMainLineDrive mainLineDrive,
        IMainLineSetpointProvider setpointProvider,
        ISystemRunStateService systemRunStateService,
        IOptions<MainLineControlOptions> options,
        StartupModeConfiguration startupConfig)
    {
        _logger = logger;
        _controlService = controlService;
        _mainLineDrive = mainLineDrive;
        _setpointProvider = setpointProvider;
        _systemRunStateService = systemRunStateService;
        _options = options.Value;
        _enableBringupLogging = startupConfig.EnableBringupLogging && 
                                startupConfig.Mode >= StartupMode.BringupMainline;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("主线控制运行时已启动");
        _logger.LogInformation("主线将受系统运行状态控制，仅在面板启动按钮置为运行时才允许设定非零速度");

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

        _logger.LogInformation("主线控制服务已启动（待机模式），等待系统运行状态变更为 Running");

        var loopPeriod = _options.LoopPeriod;
        var logCounter = 0;
        // Bring-up 模式：每秒输出，正常模式：每5秒输出
        var logInterval = _enableBringupLogging 
            ? (int)(TimeSpan.FromSeconds(1).TotalMilliseconds / loopPeriod.TotalMilliseconds)
            : (int)(TimeSpan.FromSeconds(5).TotalMilliseconds / loopPeriod.TotalMilliseconds);

        var lastState = _systemRunStateService.Current;
        _logger.LogInformation("当前系统运行状态: {State}，主线目标速度为 0 mm/s", lastState);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 检查系统运行状态
                var currentState = _systemRunStateService.Current;
                
                // 状态切换时输出日志
                if (currentState != lastState)
                {
                    _logger.LogInformation("系统运行状态已变更: {OldState} → {NewState}", lastState, currentState);
                    lastState = currentState;
                    
                    if (currentState == SystemRunState.Running)
                    {
                        _logger.LogInformation("系统已进入运行状态，主线将开始设定目标速度");
                    }
                    else if (currentState == SystemRunState.Stopped)
                    {
                        _logger.LogInformation("系统已停止，主线目标速度将保持为 0 mm/s");
                    }
                    else if (currentState == SystemRunState.Fault)
                    {
                        _logger.LogWarning("系统进入故障状态（紧急停止），主线目标速度将保持为 0 mm/s");
                    }
                }
                
                // 只有在 Running 状态时才设定非零速度
                if (currentState == SystemRunState.Running)
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
                }
                else
                {
                    // 非运行状态：确保主线停止（目标速度为0）
                    await _mainLineDrive.SetTargetSpeedAsync(0m, stoppingToken);
                    
                    // 记录状态等待日志（每5秒一次，避免刷屏）
                    logCounter++;
                    if (logCounter >= logInterval)
                    {
                        logCounter = 0;
                        _logger.LogInformation("系统未处于运行状态（当前: {State}），主线目标速度保持为 0 mm/s", currentState);
                    }
                }

                // 定期输出状态日志
                if (currentState == SystemRunState.Running)
                {
                    logCounter++;
                    if (logCounter >= logInterval)
                    {
                        logCounter = 0;
                        LogCurrentStatus();
                    }
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
        
        _logger.LogInformation("主线控制运行时已停止");
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
            if (_mainLineDrive is RemaLm1000HMainLineDrive remaDrive)
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
