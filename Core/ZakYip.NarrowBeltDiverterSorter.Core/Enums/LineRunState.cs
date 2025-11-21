using System.ComponentModel;
namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums;
public enum LineRunState
{
    [Description("未知")] Unknown = 0,
    [Description("空闲")] Idle = 1,
    [Description("启动中")] Starting = 2,
    [Description("运行中")] Running = 3,
    [Description("已暂停")] Paused = 4,
    [Description("停止中")] Stopping = 5,
    [Description("安全停机")] SafetyStopped = 6,
    [Description("故障")] Faulted = 7,
    [Description("恢复中")] Recovering = 8
}
