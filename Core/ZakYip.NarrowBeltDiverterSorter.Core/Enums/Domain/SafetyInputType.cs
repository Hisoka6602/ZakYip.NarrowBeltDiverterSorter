using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

/// <summary>
/// 安全输入类型
/// </summary>
public enum SafetyInputType
{
    /// <summary>
    /// 急停按钮
    /// </summary>
    [Description("急停按钮")]
    EmergencyStop,

    /// <summary>
    /// 安全门
    /// </summary>
    [Description("安全门")]
    SafetyDoor,

    /// <summary>
    /// 驱动故障信号
    /// </summary>
    [Description("驱动故障")]
    DriveFault,

    /// <summary>
    /// 外部联锁
    /// </summary>
    [Description("外部联锁")]
    ExternalInterlock
}
