using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;

namespace ZakYip.NarrowBeltDiverterSorter.Ingress.Safety;

/// <summary>
/// 面板按钮监控器
/// 监控面板上的启动/停止/急停按钮，并触发相应的系统状态变化和IO联动
/// </summary>
public class PanelButtonMonitor
{
    private readonly IFieldBusClient _fieldBusClient;
    private readonly ISystemRunStateService _systemRunStateService;
    private readonly ISystemFaultService _systemFaultService;
    private readonly IPanelIoCoordinator _panelIoCoordinator;
    private readonly PanelButtonConfiguration _config;
    private readonly ILogger<PanelButtonMonitor> _logger;

    // 按钮状态追踪（用于边缘检测）
    private bool _lastStartButtonState = false;
    private bool _lastStopButtonState = false;
    private bool _lastEmergencyStopState = false;
    private bool _lastEmergencyResetState = false;
    
    // 面板启动 IO 配置有效性标志
    private bool _panelStartConfigured = false;

    public PanelButtonMonitor(
        IFieldBusClient fieldBusClient,
        ISystemRunStateService systemRunStateService,
        ISystemFaultService systemFaultService,
        IPanelIoCoordinator panelIoCoordinator,
        PanelButtonConfiguration config,
        ILogger<PanelButtonMonitor> logger)
    {
        _fieldBusClient = fieldBusClient ?? throw new ArgumentNullException(nameof(fieldBusClient));
        _systemRunStateService = systemRunStateService ?? throw new ArgumentNullException(nameof(systemRunStateService));
        _systemFaultService = systemFaultService ?? throw new ArgumentNullException(nameof(systemFaultService));
        _panelIoCoordinator = panelIoCoordinator ?? throw new ArgumentNullException(nameof(panelIoCoordinator));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 启动监控
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("面板按钮监控器已启动，监控周期: {Period}ms", _config.MonitorPeriodMs);

        // 检查面板启动输入配置有效性
        // 如果启动按钮地址为 0 或负数，视为未配置
        if (_config.StartButtonAddress <= 0)
        {
            _logger.LogWarning("面板启动输入未配置，系统将保持停止状态，主线不会进入运行模式");
            _panelStartConfigured = false;
        }
        else
        {
            _logger.LogInformation("面板启动输入已配置，地址: {Address}", _config.StartButtonAddress);
            _panelStartConfigured = true;
        }

        var period = TimeSpan.FromMilliseconds(_config.MonitorPeriodMs);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await CheckButtonsAsync(cancellationToken);
                await Task.Delay(period, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "面板按钮监控发生异常");
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        _logger.LogInformation("面板按钮监控器已停止");
    }

    /// <summary>
    /// 检查所有按钮状态
    /// </summary>
    private async Task CheckButtonsAsync(CancellationToken cancellationToken)
    {
        // 读取按钮状态（按钮按下时为 true）
        var startButtonPressed = await ReadButtonAsync(_config.StartButtonAddress, cancellationToken);
        var stopButtonPressed = await ReadButtonAsync(_config.StopButtonAddress, cancellationToken);
        var emergencyStopPressed = await ReadButtonAsync(_config.EmergencyStopButtonAddress, cancellationToken);
        var emergencyResetPressed = await ReadButtonAsync(_config.EmergencyResetButtonAddress, cancellationToken);

        // 启动按钮上升沿检测（从未按下到按下）
        if (startButtonPressed && !_lastStartButtonState)
        {
            _logger.LogInformation("检测到启动按钮按下");
            await HandleStartButtonPressedAsync();
        }

        // 停止按钮上升沿检测
        if (stopButtonPressed && !_lastStopButtonState)
        {
            _logger.LogInformation("检测到停止按钮按下");
            await HandleStopButtonPressedAsync();
        }

        // 急停按钮上升沿检测
        if (emergencyStopPressed && !_lastEmergencyStopState)
        {
            _logger.LogInformation("检测到急停按钮按下");
            await HandleEmergencyStopPressedAsync();
        }

        // 急停复位按钮上升沿检测
        if (emergencyResetPressed && !_lastEmergencyResetState)
        {
            _logger.LogInformation("检测到急停复位按钮按下");
            await HandleEmergencyResetPressedAsync();
        }

        // 更新上一次状态
        _lastStartButtonState = startButtonPressed;
        _lastStopButtonState = stopButtonPressed;
        _lastEmergencyStopState = emergencyStopPressed;
        _lastEmergencyResetState = emergencyResetPressed;
    }

    /// <summary>
    /// 读取按钮状态
    /// </summary>
    private async Task<bool> ReadButtonAsync(int address, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _fieldBusClient.ReadDiscreteInputsAsync(address, 1, cancellationToken);
            return result != null && result.Length > 0 && result[0];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取按钮状态失败，地址: {Address}", address);
            return false;
        }
    }

    /// <summary>
    /// 处理启动按钮按下
    /// </summary>
    private async Task HandleStartButtonPressedAsync()
    {
        // 未配置启动 IO 时，不允许触发 SetRunning()
        if (!_panelStartConfigured)
        {
            _logger.LogWarning("面板启动输入未配置，无法启动系统");
            return;
        }
        
        var result = _systemRunStateService.TryHandleStart();
        if (result.IsSuccess)
        {
            _logger.LogInformation("系统启动成功，当前状态: {State}", _systemRunStateService.Current);
            
            // 执行启动 IO 联动
            var linkageResult = await _panelIoCoordinator.ExecuteStartLinkageAsync();
            if (!linkageResult.IsSuccess)
            {
                _logger.LogWarning("启动 IO 联动失败: {Message}", linkageResult.ErrorMessage);
            }
        }
        else
        {
            _logger.LogWarning("系统启动失败: {Message}", result.ErrorMessage);
        }
    }

    /// <summary>
    /// 处理停止按钮按下
    /// </summary>
    private async Task HandleStopButtonPressedAsync()
    {
        var result = _systemRunStateService.TryHandleStop();
        if (result.IsSuccess)
        {
            _logger.LogInformation("系统停止成功，当前状态: {State}", _systemRunStateService.Current);
            
            // 执行停止 IO 联动
            var linkageResult = await _panelIoCoordinator.ExecuteStopLinkageAsync();
            if (!linkageResult.IsSuccess)
            {
                _logger.LogWarning("停止 IO 联动失败: {Message}", linkageResult.ErrorMessage);
            }
        }
        else
        {
            _logger.LogWarning("系统停止失败: {Message}", result.ErrorMessage);
        }
    }

    /// <summary>
    /// 处理急停按钮按下
    /// </summary>
    private async Task HandleEmergencyStopPressedAsync()
    {
        var result = _systemRunStateService.TryHandleEmergencyStop();
        if (result.IsSuccess)
        {
            _logger.LogWarning("检测到急停，系统进入故障状态，当前状态: {State}", _systemRunStateService.Current);
            
            // 注册急停故障
            _systemFaultService.RegisterFault(
                SystemFaultCode.EmergencyStopActive,
                "面板急停按钮被按下",
                isBlocking: true);
            
            // 执行停止 IO 联动（急停时也需要关闭相关设备）
            var linkageResult = await _panelIoCoordinator.ExecuteStopLinkageAsync();
            if (!linkageResult.IsSuccess)
            {
                _logger.LogWarning("急停 IO 联动失败: {Message}", linkageResult.ErrorMessage);
            }
        }
        else
        {
            _logger.LogWarning("急停处理失败: {Message}", result.ErrorMessage);
        }
    }

    /// <summary>
    /// 处理急停复位按钮按下
    /// </summary>
    private async Task HandleEmergencyResetPressedAsync()
    {
        var result = _systemRunStateService.TryHandleEmergencyReset();
        if (result.IsSuccess)
        {
            _logger.LogInformation("急停已解除，系统进入停止状态，当前状态: {State}（需要按启动按钮恢复运行）", _systemRunStateService.Current);
            
            // 清除急停故障
            _systemFaultService.ClearFault(SystemFaultCode.EmergencyStopActive);
        }
        else
        {
            _logger.LogWarning("急停解除失败: {Message}", result.ErrorMessage);
        }
        
        // 急停解除后不自动启动，需要重新按启动按钮
        await Task.CompletedTask;
    }
}
