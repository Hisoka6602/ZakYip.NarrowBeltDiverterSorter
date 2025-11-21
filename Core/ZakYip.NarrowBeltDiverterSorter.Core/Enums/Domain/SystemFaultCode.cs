using System.ComponentModel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

/// <summary>
/// 系统故障代码枚举
/// 用于标识不同类型的系统故障
/// </summary>
public enum SystemFaultCode
{
    /// <summary>
    /// 现场总线断开
    /// 当现场总线（FieldBus）长时间无法连接或连续读写失败时触发
    /// </summary>
    [Description("现场总线断开")]
    FieldBusDisconnected = 1,

    /// <summary>
    /// 规则引擎不可用
    /// 当上游规则引擎（RuleEngine）连续失败或超时时触发
    /// </summary>
    [Description("规则引擎不可用")]
    RuleEngineUnavailable = 2,

    /// <summary>
    /// 紧急停止激活
    /// 当面板紧急停止按钮被按下或安全回路断开时触发
    /// </summary>
    [Description("紧急停止激活")]
    EmergencyStopActive = 3,

    /// <summary>
    /// 格口IO配置缺失
    /// 当关键配置（如格口IO地址）未正确配置时触发
    /// </summary>
    [Description("格口IO配置缺失")]
    ChuteIoConfigMissing = 4,

    /// <summary>
    /// 主线驱动故障
    /// 当主线驱动器出现故障或通信失败时触发
    /// </summary>
    [Description("主线驱动故障")]
    MainLineDriveFault = 5
}
