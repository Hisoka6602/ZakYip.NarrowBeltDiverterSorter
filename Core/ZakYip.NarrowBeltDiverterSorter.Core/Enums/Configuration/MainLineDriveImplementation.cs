using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Configuration;

/// <summary>
/// 主线驱动实现类型
/// </summary>
public enum MainLineDriveImplementation
{
    /// <summary>
    /// 仿真主线驱动
    /// </summary>
    [Description("仿真主线驱动")]
    Simulation,
    
    /// <summary>
    /// 雷马 LM1000H 主线驱动
    /// </summary>
    [Description("雷马 LM1000H")]
    RemaLm1000H
}
