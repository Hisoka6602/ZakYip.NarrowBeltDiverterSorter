namespace ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Rema;

/// <summary>
/// 雷马 LM1000H 单位换算常量
/// 所有换算系数参考：LM1000H 说明书 Modbus 地址映射表与参数定义
/// </summary>
public static class RemaScaling
{
    // ===== 频率相关换算 =====

    /// <summary>
    /// P0.05/P0.07 频率寄存器换算系数（Hz/Count）
    /// 参考：LM1000H 说明书 P0.05、P0.07 参数定义
    /// 寄存器值 × 0.01 = 实际频率(Hz)
    /// 例如：寄存器值2500 → 25.00 Hz
    /// </summary>
    public const decimal P005_HzPerCount = 0.01m;

    /// <summary>
    /// C0.26 编码器反馈频率换算系数（Hz/Count）
    /// 参考：LM1000H 说明书 C0.26 参数定义
    /// 寄存器值 × 0.01 = 实际频率(Hz)
    /// 例如：寄存器值1500 → 15.00 Hz
    /// </summary>
    public const decimal C026_HzPerCount = 0.01m;

    /// <summary>
    /// 频率(Hz) 到 线速度(mm/s) 换算系数
    /// 参考：LM1000H 说明书 C0.26 参数定义
    /// 线速度(mm/s) = 频率(Hz) × 100
    /// 例如：15.00 Hz → 1500 mm/s
    /// </summary>
    public const decimal HzToMmps = 100m;

    /// <summary>
    /// 线速度(mm/s) 到 频率(Hz) 换算系数
    /// 参考：LM1000H 说明书 C0.26 参数定义
    /// 频率(Hz) = 线速度(mm/s) × 0.01
    /// 例如：1500 mm/s → 15.00 Hz
    /// </summary>
    public const decimal MmpsToHz = 0.01m;

    /// <summary>
    /// 频率(Hz) 到 线速度(m/s) 换算系数
    /// 参考：LM1000H 说明书 C0.26 参数定义
    /// 线速度(m/s) = 频率(Hz) ÷ 10
    /// 例如：15.00 Hz → 1.5 m/s
    /// </summary>
    public const decimal HzToMps = 0.1m;

    // ===== 扭矩相关常量 =====

    /// <summary>
    /// P3.10 转矩给定值物理硬上限（寄存器值）
    /// 参考：LM1000H 说明书 P3.10 参数定义
    /// 物理上限为1000，表示100%额定电流(P2.06)
    /// 超过1000会被硬件自动限制到1000
    /// </summary>
    public const int TorqueMaxAbsolute = 1000;

    /// <summary>
    /// P3.10 转矩给定值换算：PLC控制模式下的额定值
    /// 参考：LM1000H 说明书 P3.10 参数定义
    /// 1000 = 100% 额定电流(P2.06)
    /// 启动时可达2000（200%），连续运行应≤1000（100%）
    /// </summary>
    public const int TorqueRatedPLC = 1000;

    /// <summary>
    /// P3.10 转矩给定值换算：面板控制模式下的额定值
    /// 参考：LM1000H 说明书 P3.10 参数定义
    /// 100 = 100% 额定电流(P2.06)
    /// 启动时可达200（200%），连续运行应≤100（100%）
    /// </summary>
    public const int TorqueRatedPanel = 100;

    // ===== 制动相关 =====

    /// <summary>
    /// HD.31 制动切换频率换算系数（Hz/Count）
    /// 参考：LM1000H 说明书 HD.31 参数定义
    /// 寄存器值 × 0.1 = 实际频率(Hz)
    /// 例如：寄存器值40 → 4.0 Hz
    /// </summary>
    public const decimal HD31_HzPerCount = 0.1m;

    // ===== 运行状态代码 =====

    /// <summary>
    /// C0.32 运行状态 - 正转
    /// 参考：LM1000H 说明书 C0.32 参数定义
    /// </summary>
    public const int RunStatus_Forward = 1;

    /// <summary>
    /// C0.32 运行状态 - 反转
    /// 参考：LM1000H 说明书 C0.32 参数定义
    /// </summary>
    public const int RunStatus_Reverse = 2;

    /// <summary>
    /// C0.32 运行状态 - 停止
    /// 参考：LM1000H 说明书 C0.32 参数定义
    /// </summary>
    public const int RunStatus_Stopped = 3;

    /// <summary>
    /// C0.32 运行状态 - 调谐
    /// 参考：LM1000H 说明书 C0.32 参数定义
    /// </summary>
    public const int RunStatus_Tuning = 4;

    /// <summary>
    /// C0.32 运行状态 - 故障
    /// 参考：LM1000H 说明书 C0.32 参数定义
    /// </summary>
    public const int RunStatus_Fault = 5;

    // ===== 控制命令代码 =====

    /// <summary>
    /// 控制字 - 正转运行
    /// 参考：LM1000H 说明书 控制字定义
    /// </summary>
    public const int ControlCmd_Forward = 1;

    /// <summary>
    /// 控制字 - 反转运行
    /// 参考：LM1000H 说明书 控制字定义
    /// </summary>
    public const int ControlCmd_Reverse = 2;

    /// <summary>
    /// 控制字 - 正转点动
    /// 参考：LM1000H 说明书 控制字定义
    /// </summary>
    public const int ControlCmd_ForwardJog = 3;

    /// <summary>
    /// 控制字 - 反转点动
    /// 参考：LM1000H 说明书 控制字定义
    /// </summary>
    public const int ControlCmd_ReverseJog = 4;

    /// <summary>
    /// 控制字 - 减速停机
    /// 参考：LM1000H 说明书 控制字定义
    /// </summary>
    public const int ControlCmd_Decelerate = 5;

    /// <summary>
    /// 控制字 - 自由停机
    /// 参考：LM1000H 说明书 控制字定义
    /// </summary>
    public const int ControlCmd_FreeStop = 6;

    /// <summary>
    /// 控制字 - 故障复位
    /// 参考：LM1000H 说明书 控制字定义
    /// </summary>
    public const int ControlCmd_FaultReset = 7;
}
