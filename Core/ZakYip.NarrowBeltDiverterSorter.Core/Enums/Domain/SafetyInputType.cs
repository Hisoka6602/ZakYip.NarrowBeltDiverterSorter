namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

/// <summary>
/// 安全输入类型
/// </summary>
public enum SafetyInputType
{
    /// <summary>
    /// 急停按钮
    /// </summary>
    EmergencyStop,

    /// <summary>
    /// 安全门
    /// </summary>
    SafetyDoor,

    /// <summary>
    /// 驱动故障信号
    /// </summary>
    DriveFault,

    /// <summary>
    /// 外部联锁
    /// </summary>
    ExternalInterlock
}
