namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 格口 IO 映射与参数配置选项
/// 用于消除代码中的魔法数字，统一管理格口 IO 相关参数
/// </summary>
public sealed class ChuteIoMappingOptions
{
    /// <summary>
    /// 强排口格口ID
    /// </summary>
    public required long StrongEjectChuteId { get; init; }

    /// <summary>
    /// 格口ID → IO 通道/线圈号 映射
    /// </summary>
    public required IReadOnlyDictionary<long, int> ChuteIdToIoChannel { get; init; }

    /// <summary>
    /// 吐件保持时间（毫秒），用于脉冲型 IO
    /// </summary>
    public int PulseDurationMilliseconds { get; init; } = 200;

    /// <summary>
    /// 创建默认配置
    /// </summary>
    /// <param name="numberOfChutes">格口数量</param>
    /// <param name="strongEjectChuteId">强排口格口ID</param>
    /// <returns>默认配置实例</returns>
    public static ChuteIoMappingOptions CreateDefault(int numberOfChutes = 10, long strongEjectChuteId = 10)
    {
        var mapping = new Dictionary<long, int>();
        for (int i = 1; i <= numberOfChutes; i++)
        {
            mapping[i] = i; // 默认：格口ID直接映射到IO通道号
        }

        return new ChuteIoMappingOptions
        {
            StrongEjectChuteId = strongEjectChuteId,
            ChuteIdToIoChannel = mapping,
            PulseDurationMilliseconds = 200
        };
    }
}
