using System.ComponentModel;
namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums;
public enum SafetyState
{
    [Description("安全")] Safe = 0,
    [Description("安全输入异常")] UnsafeInput = 1,
    [Description("急停触发")] EmergencyStop = 2,
    [Description("驱动故障")] DriveFault = 3,
    [Description("联锁断开")] InterlockOpen = 4,
    [Description("未知")] Unknown = 99
}
