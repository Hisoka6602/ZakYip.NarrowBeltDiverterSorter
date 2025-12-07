using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using ZakYip.NarrowBeltDiverterSorter.Observability.Events;

namespace ZakYip.NarrowBeltDiverterSorter.Ingress.Chute;

/// <summary>
/// 格口IO监视器
/// 只读轮询格口相关IO状态，用于观测发信器是否按预期开闭
/// 记录日志供诊断使用，通过IEventBus发布事件
/// </summary>
public class ChuteIoMonitor : IIoMonitor
{
    private readonly IFieldBusClient _fieldBusClient;
    private readonly ChuteIoMonitorConfiguration _configuration;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ChuteIoMonitor> _logger;
    private readonly ConcurrentDictionary<long, bool> _previousStates = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _monitoringTask;

    /// <summary>
    /// 创建格口IO监视器
    /// </summary>
    /// <param name="fieldBusClient">现场总线客户端</param>
    /// <param name="configuration">监控配置</param>
    /// <param name="eventBus">事件总线</param>
    /// <param name="logger">日志记录器</param>
    public ChuteIoMonitor(
        IFieldBusClient fieldBusClient,
        ChuteIoMonitorConfiguration configuration,
        IEventBus eventBus,
        ILogger<ChuteIoMonitor> logger)
    {
        _fieldBusClient = fieldBusClient ?? throw new ArgumentNullException(nameof(fieldBusClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 初始化所有格口的状态为false
        foreach (var chuteId in _configuration.MonitoredChuteIds)
        {
            _previousStates[chuteId] = false;
        }
    }

    /// <inheritdoc/>
    public bool IsRunning => _monitoringTask != null && !_monitoringTask.IsCompleted;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_monitoringTask != null)
        {
            _logger.LogWarning("格口IO监视器已经在运行中");
            return Task.CompletedTask;
        }

        _logger.LogInformation("启动格口IO监视器，监控 {Count} 个格口", _configuration.MonitoredChuteIds.Count);
        _cancellationTokenSource = new CancellationTokenSource();
        _monitoringTask = Task.Run(async () => await MonitoringLoopAsync(_cancellationTokenSource.Token), cancellationToken);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public async Task StopAsync()
    {
        if (_cancellationTokenSource == null || _monitoringTask == null)
        {
            return;
        }

        _logger.LogInformation("停止格口IO监视器");
        _cancellationTokenSource.Cancel();

        try
        {
            await _monitoringTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _monitoringTask = null;
        }

        _logger.LogInformation("格口IO监视器已停止");
    }

    private async Task MonitoringLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // 检查连接状态
                if (!_fieldBusClient.IsConnected())
                {
                    _logger.LogWarning("现场总线未连接，跳过本次轮询");
                    await Task.Delay(_configuration.PollingInterval, cancellationToken);
                    continue;
                }

                // 轮询所有配置的格口IO状态
                foreach (var chuteId in _configuration.MonitoredChuteIds)
                {
                    var ioAddress = _configuration.GetIoAddress(chuteId);
                    if (ioAddress == null)
                    {
                        _logger.LogWarning("格口 {ChuteId} 未配置IO地址映射", chuteId);
                        continue;
                    }

                    // 读取离散输入状态
                    var states = await _fieldBusClient.ReadDiscreteInputsAsync(ioAddress.Value, 1, cancellationToken);
                    if (states == null || states.Length == 0)
                    {
                        _logger.LogDebug("读取格口 {ChuteId} IO状态失败", chuteId);
                        continue;
                    }

                    var currentState = states[0];
                    var previousState = _previousStates.GetOrAdd(chuteId, false);

                    // 检测状态变化
                    if (currentState != previousState)
                    {
                        var timestamp = DateTimeOffset.Now;
                        _logger.LogInformation(
                            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] 格口 {ChuteId} IO状态变化: {PreviousState} -> {CurrentState}",
                            timestamp,
                            chuteId,
                            previousState ? "开启" : "关闭",
                            currentState ? "开启" : "关闭");

                        _previousStates[chuteId] = currentState;

                        // 发布传感器触发事件
                        var sensorEvent = new SensorTriggeredEventArgs
                        {
                            SensorId = $"Chute{chuteId}",
                            TriggerTime = timestamp,
                            IsTriggered = currentState,
                            IsRisingEdge = currentState && !previousState
                        };
                        _ = _eventBus.PublishAsync(sensorEvent);
                    }
                }

                // 等待下一次轮询
                await Task.Delay(_configuration.PollingInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "格口IO监控循环发生异常");
                // 继续运行，不中断监控
            }
        }
    }
}
