namespace ZakYip.NarrowBeltDiverterSorter.Shared.Math;

/// <summary>
/// 物理量单位换算扩展方法
/// 提供常见的速度、频率等物理量单位之间的换算
/// </summary>
/// <remarks>
/// 这些扩展方法仅包含通用的物理量换算，不包含任何设备或业务相关的参数。
/// 所有方法为纯函数，无副作用，可安全并发调用。
/// </remarks>
public static class PhysicsConversionExtensions
{
    /// <summary>
    /// 将毫米每秒(mm/s)转换为米每秒(m/s)
    /// </summary>
    /// <param name="mmps">速度值，单位：mm/s</param>
    /// <returns>速度值，单位：m/s</returns>
    /// <remarks>
    /// 转换公式：m/s = mm/s ÷ 1000
    /// 性能：O(1)，无内存分配
    /// </remarks>
    public static decimal ToMps(this decimal mmps) => mmps / 1000m;

    /// <summary>
    /// 将米每秒(m/s)转换为毫米每秒(mm/s)
    /// </summary>
    /// <param name="mps">速度值，单位：m/s</param>
    /// <returns>速度值，单位：mm/s</returns>
    /// <remarks>
    /// 转换公式：mm/s = m/s × 1000
    /// 性能：O(1)，无内存分配
    /// </remarks>
    public static decimal ToMmps(this decimal mps) => mps * 1000m;

    /// <summary>
    /// 将毫米每秒(mm/s)转换为厘米每秒(cm/s)
    /// </summary>
    /// <param name="mmps">速度值，单位：mm/s</param>
    /// <returns>速度值，单位：cm/s</returns>
    /// <remarks>
    /// 转换公式：cm/s = mm/s ÷ 10
    /// 性能：O(1)，无内存分配
    /// </remarks>
    public static decimal ToCmps(this decimal mmps) => mmps / 10m;

    /// <summary>
    /// 将厘米每秒(cm/s)转换为毫米每秒(mm/s)
    /// </summary>
    /// <param name="cmps">速度值，单位：cm/s</param>
    /// <returns>速度值，单位：mm/s</returns>
    /// <remarks>
    /// 转换公式：mm/s = cm/s × 10
    /// 性能：O(1)，无内存分配
    /// </remarks>
    public static decimal CmpsToMmps(this decimal cmps) => cmps * 10m;
}
