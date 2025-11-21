namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Execution;

/// <summary>
/// 串口奇偶校验位
/// </summary>
public enum SerialParity
{
    /// <summary>无校验</summary>
    None = 0,
    /// <summary>奇校验</summary>
    Odd = 1,
    /// <summary>偶校验</summary>
    Even = 2,
    /// <summary>标记</summary>
    Mark = 3,
    /// <summary>空格</summary>
    Space = 4
}
