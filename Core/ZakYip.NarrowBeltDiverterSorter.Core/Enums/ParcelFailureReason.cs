using System.ComponentModel;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;
namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums;
public enum ParcelFailureReason
{
    [Description("无失败")] None = 0,
    [Description("上游指令超时")] UpstreamTimeout = 1,
    [Description("等待上游分配结果超时")] WaitingUpstreamResultTimeout = 10,
    [Description("无分拣计划")] NoPlan = 2,
    [Description("计划已过期")] PlanExpired = 3,
    [Description("错过分拣窗口")] MissedWindow = 4,
    [Description("窗口冲突")] WindowConflict = 5,
    [Description("小车不匹配")] CartMismatch = 6,
    [Description("设备故障")] DeviceFault = 7,
    [Description("安全停机")] SafetyStop = 8,
    [Description("人工干预")] ManualIntervention = 9,
    [Description("未知原因")] Unknown = 99
}
