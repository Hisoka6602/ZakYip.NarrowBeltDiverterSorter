using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

namespace ZakYip.NarrowBeltDiverterSorter.Drivers.Cart;

/// <summary>
/// 小车参数驱动实现
/// 实现 ICartParameterPort，使用IFieldBusClient与现场总线通信
/// 所有小车共用同一套参数配置，不支持针对单个小车的独立控制
/// </summary>
public class CartParameterDriver : ICartParameterPort
{
    private readonly IFieldBusClient _fieldBusClient;
    private readonly CartParameterRegisterConfiguration _registerConfiguration;
    private readonly ILogger<CartParameterDriver> _logger;

    /// <summary>
    /// 创建小车参数驱动实例
    /// </summary>
    /// <param name="fieldBusClient">现场总线客户端</param>
    /// <param name="registerConfiguration">寄存器地址配置</param>
    /// <param name="logger">日志记录器</param>
    public CartParameterDriver(
        IFieldBusClient fieldBusClient,
        CartParameterRegisterConfiguration registerConfiguration,
        ILogger<CartParameterDriver> logger)
    {
        _fieldBusClient = fieldBusClient ?? throw new ArgumentNullException(nameof(fieldBusClient));
        _registerConfiguration = registerConfiguration ?? throw new ArgumentNullException(nameof(registerConfiguration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> SetEjectionDistanceAsync(double distanceMm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "设置吐件距离: {DistanceMm}mm，寄存器地址 {Address}",
                distanceMm,
                _registerConfiguration.EjectionDistanceRegisterAddress);

            // 将double值转换为ushort（假设单位为mm，精度为整数）
            if (distanceMm < 0 || distanceMm > ushort.MaxValue)
            {
                _logger.LogError("吐件距离 {DistanceMm}mm 超出有效范围 [0, {MaxValue}]", distanceMm, ushort.MaxValue);
                return false;
            }

            var registerValue = (ushort)Math.Round(distanceMm);

            var success = await _fieldBusClient.WriteSingleRegisterAsync(
                _registerConfiguration.EjectionDistanceRegisterAddress,
                registerValue,
                cancellationToken);

            if (success)
            {
                _logger.LogInformation("吐件距离设置成功: {DistanceMm}mm", distanceMm);
            }
            else
            {
                _logger.LogError("吐件距离设置失败: {DistanceMm}mm", distanceMm);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置吐件距离时发生异常: {DistanceMm}mm", distanceMm);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SetEjectionDelayAsync(int delayMs, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "设置吐件延迟: {DelayMs}ms，寄存器地址 {Address}",
                delayMs,
                _registerConfiguration.EjectionDelayRegisterAddress);

            if (delayMs < 0 || delayMs > ushort.MaxValue)
            {
                _logger.LogError("吐件延迟 {DelayMs}ms 超出有效范围 [0, {MaxValue}]", delayMs, ushort.MaxValue);
                return false;
            }

            var registerValue = (ushort)delayMs;

            var success = await _fieldBusClient.WriteSingleRegisterAsync(
                _registerConfiguration.EjectionDelayRegisterAddress,
                registerValue,
                cancellationToken);

            if (success)
            {
                _logger.LogInformation("吐件延迟设置成功: {DelayMs}ms", delayMs);
            }
            else
            {
                _logger.LogError("吐件延迟设置失败: {DelayMs}ms", delayMs);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置吐件延迟时发生异常: {DelayMs}ms", delayMs);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SetMaxConsecutiveActionCartsAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "设置最大连续动作小车数: {MaxCount}，寄存器地址 {Address}",
                maxCount,
                _registerConfiguration.MaxConsecutiveActionCartsRegisterAddress);

            if (maxCount < 0 || maxCount > ushort.MaxValue)
            {
                _logger.LogError("最大连续动作小车数 {MaxCount} 超出有效范围 [0, {MaxValue}]", maxCount, ushort.MaxValue);
                return false;
            }

            var registerValue = (ushort)maxCount;

            var success = await _fieldBusClient.WriteSingleRegisterAsync(
                _registerConfiguration.MaxConsecutiveActionCartsRegisterAddress,
                registerValue,
                cancellationToken);

            if (success)
            {
                _logger.LogInformation("最大连续动作小车数设置成功: {MaxCount}", maxCount);
            }
            else
            {
                _logger.LogError("最大连续动作小车数设置失败: {MaxCount}", maxCount);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置最大连续动作小车数时发生异常: {MaxCount}", maxCount);
            return false;
        }
    }
}
