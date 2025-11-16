namespace ZakYip.NarrowBeltDiverterSorter.Drivers.Cart;

/// <summary>
/// 小车参数寄存器地址配置
/// </summary>
public class CartParameterRegisterConfiguration
{
    /// <summary>
    /// 吐件距离寄存器地址（单位：mm）
    /// </summary>
    public int EjectionDistanceRegisterAddress { get; set; } = 1000;

    /// <summary>
    /// 吐件延迟寄存器地址（单位：ms）
    /// </summary>
    public int EjectionDelayRegisterAddress { get; set; } = 1001;

    /// <summary>
    /// 最大连续动作小车数寄存器地址
    /// </summary>
    public int MaxConsecutiveActionCartsRegisterAddress { get; set; } = 1002;
}
