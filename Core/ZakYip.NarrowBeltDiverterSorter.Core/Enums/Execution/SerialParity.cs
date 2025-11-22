using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Execution;

/// <summary>
/// 串口奇偶校验位
/// </summary>
public enum SerialParity
{
    /// <summary>无校验</summary>
    [Description("无校验")]
    None = 0,
    /// <summary>奇校验</summary>
    [Description("奇校验")]
    Odd = 1,
    /// <summary>偶校验</summary>
    [Description("偶校验")]
    Even = 2,
    /// <summary>标记</summary>
    [Description("标记")]
    Mark = 3,
    /// <summary>空格</summary>
    [Description("空格")]
    Space = 4
}
