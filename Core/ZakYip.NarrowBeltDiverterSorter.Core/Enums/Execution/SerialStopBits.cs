using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Execution;

/// <summary>
/// 串口停止位
/// </summary>
public enum SerialStopBits
{
    /// <summary>无停止位</summary>
    [Description("无")]
    None = 0,
    /// <summary>1 个停止位</summary>
    [Description("1位")]
    One = 1,
    /// <summary>2 个停止位</summary>
    [Description("2位")]
    Two = 2,
    /// <summary>1.5 个停止位</summary>
    [Description("1.5位")]
    OnePointFive = 3
}
