using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Shared.Kernel;

#pragma warning disable CS0618 // IPanelIoCoordinator 已过时：此类实现旧的 IPanelIoCoordinator 接口以保持向后兼容性。新代码应使用 Core.Abstractions.IPanelIoCoordinator。

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Panel;

/// <summary>
/// 面板 IO 协调器实现
/// 负责处理系统状态变化时的 IO 联动操作
/// </summary>
public class PanelIoCoordinator : IPanelIoCoordinator
{
    private readonly IFieldBusClient _fieldBusClient;
    private readonly PanelIoLinkageOptions _options;
    private readonly ILogger<PanelIoCoordinator> _logger;

    public PanelIoCoordinator(
        IFieldBusClient fieldBusClient,
        IOptions<PanelIoLinkageOptions> options,
        ILogger<PanelIoCoordinator> logger)
    {
        _fieldBusClient = fieldBusClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<OperationResult> ExecuteStartLinkageAsync()
    {
        if (_options.StartFollowOutputChannels.Count == 0)
        {
            _logger.LogDebug("未配置跟随启动的输出通道，跳过 IO 联动");
            return OperationResult.Success();
        }

        _logger.LogInformation("执行启动 IO 联动，设置 {Count} 个输出通道为 ON", _options.StartFollowOutputChannels.Count);

        var failures = new List<string>();
        foreach (var channel in _options.StartFollowOutputChannels)
        {
            try
            {
                var success = await _fieldBusClient.WriteSingleCoilAsync(channel, true);
                if (!success)
                {
                    var errorMsg = $"通道 {channel} 写入失败";
                    failures.Add(errorMsg);
                    _logger.LogWarning("启动联动 IO 写入失败：{ErrorMessage}", errorMsg);
                }
                else
                {
                    _logger.LogDebug("启动联动 IO 通道 {Channel} 设置为 ON", channel);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"通道 {channel} 写入异常: {ex.Message}";
                failures.Add(errorMsg);
                _logger.LogError(ex, "启动联动 IO 写入异常：{ErrorMessage}", errorMsg);
            }
        }

        if (failures.Count > 0)
        {
            return OperationResult.Failure($"启动联动 IO 写入部分失败：{string.Join("; ", failures)}");
        }

        return OperationResult.Success();
    }

    /// <inheritdoc/>
    public async Task<OperationResult> ExecuteStopLinkageAsync()
    {
        if (_options.StopFollowOutputChannels.Count == 0)
        {
            _logger.LogDebug("未配置跟随停止的输出通道，跳过 IO 联动");
            return OperationResult.Success();
        }

        _logger.LogInformation("执行停止 IO 联动，设置 {Count} 个输出通道为 OFF", _options.StopFollowOutputChannels.Count);

        var failures = new List<string>();
        foreach (var channel in _options.StopFollowOutputChannels)
        {
            try
            {
                var success = await _fieldBusClient.WriteSingleCoilAsync(channel, false);
                if (!success)
                {
                    var errorMsg = $"通道 {channel} 写入失败";
                    failures.Add(errorMsg);
                    _logger.LogWarning("停止联动 IO 写入失败：{ErrorMessage}", errorMsg);
                }
                else
                {
                    _logger.LogDebug("停止联动 IO 通道 {Channel} 设置为 OFF", channel);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"通道 {channel} 写入异常: {ex.Message}";
                failures.Add(errorMsg);
                _logger.LogError(ex, "停止联动 IO 写入异常：{ErrorMessage}", errorMsg);
            }
        }

        if (failures.Count > 0)
        {
            return OperationResult.Failure($"停止联动 IO 写入部分失败：{string.Join("; ", failures)}");
        }

        return OperationResult.Success();
    }

    /// <inheritdoc/>
    public async Task<OperationResult> ExecuteFirstStableSpeedLinkageAsync()
    {
        if (_options.FirstStableSpeedFollowOutputChannels.Count == 0)
        {
            _logger.LogDebug("未配置首次稳速时联动的输出通道，跳过 IO 联动");
            return OperationResult.Success();
        }

        _logger.LogInformation("执行首次稳速 IO 联动，设置 {Count} 个输出通道为 ON", _options.FirstStableSpeedFollowOutputChannels.Count);

        var failures = new List<string>();
        foreach (var channel in _options.FirstStableSpeedFollowOutputChannels)
        {
            try
            {
                var success = await _fieldBusClient.WriteSingleCoilAsync(channel, true);
                if (!success)
                {
                    var errorMsg = $"通道 {channel} 写入失败";
                    failures.Add(errorMsg);
                    _logger.LogWarning("首次稳速联动 IO 写入失败：{ErrorMessage}", errorMsg);
                }
                else
                {
                    _logger.LogDebug("首次稳速联动 IO 通道 {Channel} 设置为 ON", channel);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"通道 {channel} 写入异常: {ex.Message}";
                failures.Add(errorMsg);
                _logger.LogError(ex, "首次稳速联动 IO 写入异常：{ErrorMessage}", errorMsg);
            }
        }

        if (failures.Count > 0)
        {
            return OperationResult.Failure($"首次稳速联动 IO 写入部分失败：{string.Join("; ", failures)}");
        }

        return OperationResult.Success();
    }

    /// <inheritdoc/>
    public async Task<OperationResult> ExecuteUnstableAfterStableLinkageAsync()
    {
        if (_options.UnstableAfterStableFollowOutputChannels.Count == 0)
        {
            _logger.LogDebug("未配置稳速后不稳速时联动的输出通道，跳过 IO 联动");
            return OperationResult.Success();
        }

        _logger.LogInformation("执行稳速后不稳速 IO 联动，设置 {Count} 个输出通道为 ON", _options.UnstableAfterStableFollowOutputChannels.Count);

        var failures = new List<string>();
        foreach (var channel in _options.UnstableAfterStableFollowOutputChannels)
        {
            try
            {
                var success = await _fieldBusClient.WriteSingleCoilAsync(channel, true);
                if (!success)
                {
                    var errorMsg = $"通道 {channel} 写入失败";
                    failures.Add(errorMsg);
                    _logger.LogWarning("稳速后不稳速联动 IO 写入失败：{ErrorMessage}", errorMsg);
                }
                else
                {
                    _logger.LogDebug("稳速后不稳速联动 IO 通道 {Channel} 设置为 ON", channel);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"通道 {channel} 写入异常: {ex.Message}";
                failures.Add(errorMsg);
                _logger.LogError(ex, "稳速后不稳速联动 IO 写入异常：{ErrorMessage}", errorMsg);
            }
        }

        if (failures.Count > 0)
        {
            return OperationResult.Failure($"稳速后不稳速联动 IO 写入部分失败：{string.Join("; ", failures)}");
        }

        return OperationResult.Success();
    }
}
