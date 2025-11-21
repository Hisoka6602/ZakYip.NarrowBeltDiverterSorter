using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Chutes;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Chute;

/// <summary>
/// 格口发信器驱动实现
/// 实现 IChuteTransmitterPort，使用IFieldBusClient与现场总线通信
/// </summary>
public class ChuteTransmitterDriver : IChuteTransmitterPort
{
    private readonly IFieldBusClient _fieldBusClient;
    private readonly ChuteMappingConfiguration _mappingConfiguration;
    private readonly ILogger<ChuteTransmitterDriver> _logger;
    private readonly object _bindingsLock = new();
    private List<ChuteTransmitterBinding> _bindings = new();

    /// <summary>
    /// 创建格口发信器驱动实例
    /// </summary>
    /// <param name="fieldBusClient">现场总线客户端</param>
    /// <param name="mappingConfiguration">格口映射配置</param>
    /// <param name="logger">日志记录器</param>
    public ChuteTransmitterDriver(
        IFieldBusClient fieldBusClient,
        ChuteMappingConfiguration mappingConfiguration,
        ILogger<ChuteTransmitterDriver> logger)
    {
        _fieldBusClient = fieldBusClient ?? throw new ArgumentNullException(nameof(fieldBusClient));
        _mappingConfiguration = mappingConfiguration ?? throw new ArgumentNullException(nameof(mappingConfiguration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 注册格口发信器绑定配置。
    /// </summary>
    public void RegisterBindings(IEnumerable<ChuteTransmitterBinding> bindings)
    {
        lock (_bindingsLock)
        {
            _bindings.Clear();
            _bindings.AddRange(bindings);
            _logger.LogInformation("已注册 {Count} 条格口发信器绑定配置", _bindings.Count);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<ChuteTransmitterBinding> GetRegisteredBindings()
    {
        lock (_bindingsLock)
        {
            return _bindings.ToList();
        }
    }

    /// <inheritdoc/>
    public async Task OpenWindowAsync(ChuteId chuteId, TimeSpan openDuration, CancellationToken cancellationToken = default)
    {
        try
        {
            // 获取格口对应的线圈地址
            var coilAddress = _mappingConfiguration.GetCoilAddress(chuteId.Value);
            if (coilAddress == null)
            {
                _logger.LogError("格口 {ChuteId} 未配置地址映射", chuteId.Value);
                return;
            }

            _logger.LogInformation(
                "打开格口 {ChuteId} 的窗口，持续时间 {OpenDuration}ms，地址 {Address}",
                chuteId.Value,
                openDuration.TotalMilliseconds,
                coilAddress);

            // 写线圈打开窗口
            var success = await _fieldBusClient.WriteSingleCoilAsync(coilAddress.Value, true, cancellationToken);
            if (!success)
            {
                _logger.LogError("打开格口 {ChuteId} 窗口失败：写线圈失败", chuteId.Value);
                return;
            }

            // 等待指定时长
            await Task.Delay(openDuration, cancellationToken);

            // 自动关闭窗口
            success = await _fieldBusClient.WriteSingleCoilAsync(coilAddress.Value, false, cancellationToken);
            if (!success)
            {
                _logger.LogWarning("自动关闭格口 {ChuteId} 窗口失败：写线圈失败", chuteId.Value);
            }
            else
            {
                _logger.LogInformation("格口 {ChuteId} 窗口已自动关闭", chuteId.Value);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("打开格口 {ChuteId} 窗口操作已取消", chuteId.Value);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开格口 {ChuteId} 窗口时发生异常", chuteId.Value);
        }
    }

    /// <inheritdoc/>
    public async Task ForceCloseAsync(ChuteId chuteId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 获取格口对应的线圈地址
            var coilAddress = _mappingConfiguration.GetCoilAddress(chuteId.Value);
            if (coilAddress == null)
            {
                _logger.LogError("格口 {ChuteId} 未配置地址映射", chuteId.Value);
                return;
            }

            _logger.LogInformation("强制关闭格口 {ChuteId} 的窗口，地址 {Address}", chuteId.Value, coilAddress);

            // 写线圈关闭窗口
            var success = await _fieldBusClient.WriteSingleCoilAsync(coilAddress.Value, false, cancellationToken);
            if (!success)
            {
                _logger.LogError("强制关闭格口 {ChuteId} 窗口失败：写线圈失败", chuteId.Value);
                return;
            }

            _logger.LogInformation("格口 {ChuteId} 窗口已强制关闭", chuteId.Value);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("强制关闭格口 {ChuteId} 窗口操作已取消", chuteId.Value);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "强制关闭格口 {ChuteId} 窗口时发生异常", chuteId.Value);
        }
    }
}
