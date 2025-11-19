using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

namespace ZakYip.NarrowBeltDiverterSorter.Ingress.Safety;

/// <summary>
/// 基于现场总线的安全输入源实现
/// 从 Modbus/FieldBus 读取安全相关的输入信号
/// </summary>
public class FieldBusSafetyInputSource : ISafetyInputSource
{
    private readonly IFieldBusClient _fieldBusClient;
    private readonly SafetyInputAddressConfiguration _addressConfig;
    private readonly ILogger<FieldBusSafetyInputSource> _logger;

    public FieldBusSafetyInputSource(
        IFieldBusClient fieldBusClient,
        SafetyInputAddressConfiguration addressConfig,
        ILogger<FieldBusSafetyInputSource> logger)
    {
        _fieldBusClient = fieldBusClient ?? throw new ArgumentNullException(nameof(fieldBusClient));
        _addressConfig = addressConfig ?? throw new ArgumentNullException(nameof(addressConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> ReadEmergencyStopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _fieldBusClient.ReadDiscreteInputsAsync(
                _addressConfig.EmergencyStopAddress,
                1,
                cancellationToken);

            if (result == null || result.Length == 0)
            {
                _logger.LogWarning("无法读取急停状态");
                return false; // 读取失败视为不安全
            }

            // true = 急停未触发（安全），false = 急停触发（不安全）
            return result[0];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取急停状态时发生异常");
            return false; // 异常视为不安全
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ReadSafetyDoorAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _fieldBusClient.ReadDiscreteInputsAsync(
                _addressConfig.SafetyDoorAddress,
                1,
                cancellationToken);

            if (result == null || result.Length == 0)
            {
                _logger.LogWarning("无法读取安全门状态");
                return false; // 读取失败视为不安全
            }

            // true = 安全门关闭（安全），false = 安全门打开（不安全）
            return result[0];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取安全门状态时发生异常");
            return false; // 异常视为不安全
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ReadInterlockAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _fieldBusClient.ReadDiscreteInputsAsync(
                _addressConfig.InterlockAddress,
                1,
                cancellationToken);

            if (result == null || result.Length == 0)
            {
                _logger.LogWarning("无法读取联锁状态");
                return false; // 读取失败视为不安全
            }

            // true = 联锁正常（安全），false = 联锁异常（不安全）
            return result[0];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取联锁状态时发生异常");
            return false; // 异常视为不安全
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAllSafeAsync(CancellationToken cancellationToken = default)
    {
        var emergencyStopSafe = await ReadEmergencyStopAsync(cancellationToken);
        var safetyDoorSafe = await ReadSafetyDoorAsync(cancellationToken);
        var interlockSafe = await ReadInterlockAsync(cancellationToken);

        return emergencyStopSafe && safetyDoorSafe && interlockSafe;
    }
}

/// <summary>
/// 安全输入地址配置
/// 定义安全输入在现场总线上的地址映射
/// </summary>
public sealed record SafetyInputAddressConfiguration
{
    /// <summary>
    /// 急停输入地址
    /// </summary>
    public required int EmergencyStopAddress { get; init; }

    /// <summary>
    /// 安全门输入地址
    /// </summary>
    public required int SafetyDoorAddress { get; init; }

    /// <summary>
    /// 联锁输入地址
    /// </summary>
    public required int InterlockAddress { get; init; }

    /// <summary>
    /// 创建默认配置（用于测试）
    /// </summary>
    public static SafetyInputAddressConfiguration CreateDefault()
    {
        return new SafetyInputAddressConfiguration
        {
            EmergencyStopAddress = 1000,
            SafetyDoorAddress = 1001,
            InterlockAddress = 1002
        };
    }
}
