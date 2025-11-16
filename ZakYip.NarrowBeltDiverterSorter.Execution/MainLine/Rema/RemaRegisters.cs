namespace ZakYip.NarrowBeltDiverterSorter.Execution.MainLine.Rema;

/// <summary>
/// 雷马 LM1000H Modbus 寄存器地址定义
/// 所有地址参考：LM1000H 说明书 Modbus 地址映射表
/// </summary>
public static class RemaRegisters
{
    // ===== 控制字寄存器 =====

    /// <summary>
    /// 控制字寄存器地址
    /// 参考：LM1000H 说明书 - 控制字定义
    /// 写入值：1=正转运行, 2=反转运行, 3=正转点动, 4=反转点动, 5=减速停机, 6=自由停机, 7=故障复位
    /// </summary>
    public const ushort ControlWord = 0x2000;

    // ===== 设定参数（P组）=====

    /// <summary>
    /// P0.01 - 运行命令源选择
    /// 参考：LM1000H 说明书 P0.01
    /// 0=操作面板, 1=外部端子（出厂默认）, 2=RS485通讯
    /// </summary>
    public const ushort P0_01_RunCmdSource = 0xF001;

    /// <summary>
    /// P0.04 - 最大输出频率（Hz）
    /// 参考：LM1000H 说明书 P0.04
    /// 有些固件把基准/最大输出频率放在此参数
    /// </summary>
    public const ushort P0_04_MaxOutputHz = 0xF004;

    /// <summary>
    /// P0.05 - 基准频率（最高频率）
    /// 参考：LM1000H 说明书 P0.05
    /// 单位：Hz/Count = 0.01（即寄存器值÷100得到实际Hz）
    /// </summary>
    public const ushort P0_05_BaseFrequency = 0xF005;

    /// <summary>
    /// P0.07 - 限速频率（Hz）
    /// 参考：LM1000H 说明书 P0.07
    /// 设备运行频率 + 2~5Hz，用于限制最高运行速度
    /// 单位：Hz/Count = 0.01（即寄存器值÷100得到实际Hz）
    /// </summary>
    public const ushort P0_07_LimitFrequency = 0xF007;

    /// <summary>
    /// P2.06 - 电机额定电流（A）
    /// 参考：LM1000H 说明书 P2.06
    /// 典型值：4A（一拖二）, 6A（一拖三）
    /// </summary>
    public const ushort P2_06_RatedCurrent = 0xF206;

    /// <summary>
    /// P2.26 - 编码器报警设定
    /// 参考：LM1000H 说明书 P2.26
    /// 1=编码器故障报警, 11=编码器故障报警+次级装反报警
    /// </summary>
    public const ushort P2_26_EncoderAlarm = 0xF21A;

    /// <summary>
    /// P3.10 - 转矩给定值
    /// 参考：LM1000H 说明书 P3.10
    /// PLC控制：1000=额定（P2.06），启动≤2000，连续运行≤1000
    /// 面板控制：100=额定，启动≤200，连续运行≤100
    /// 物理上限：1000（=100%额定电流）
    /// </summary>
    public const ushort P3_10_TorqueRef = 0x030A;

    /// <summary>
    /// P3.12 - 制动转矩设定（%）
    /// 参考：LM1000H 说明书 P3.12
    /// 默认200，范围0~200%，基数为P2.06
    /// </summary>
    public const ushort P3_12_BrakeTorque = 0xF30C;

    /// <summary>
    /// P5.33 - X1端子响应延迟时间（秒）
    /// 参考：LM1000H 说明书 P5.33
    /// 默认0，可让使能信号延时断开/闭合
    /// </summary>
    public const ushort P5_33_X1DelaySeconds = 0xF521;

    /// <summary>
    /// P6.02 - 继电器定义
    /// 参考：LM1000H 说明书 P6.02
    /// 3=故障闭合(TC/TA), 23=上电闭合，故障/掉电断开
    /// </summary>
    public const ushort P6_02_RelayDefine = 0xF602;

    /// <summary>
    /// P7.07 - LED面板运行显示位
    /// 参考：LM1000H 说明书 P7.07
    /// 建议设置 0x4006 → 只显示：运行频率/运行电流/运行电压/功率
    /// 千/百/十/个位分别包含 Bit0..Bit3 选择位
    /// </summary>
    public const ushort P7_07_PanelDisplayBits = 0xF706;

    // ===== 通讯参数（Pd组）=====

    /// <summary>
    /// Pd.01 - 通讯波特率选择
    /// 参考：LM1000H 说明书 Pd.01
    /// 1=600, 2=1200, 3=2400, 4=4800, 5=9600(默认), 6=19200, 7=38400, 8=57600, 9=115200
    /// </summary>
    public const ushort Pd_01_BaudRate = 0xFD01;

    /// <summary>
    /// Pd.02 - 数据格式
    /// 参考：LM1000H 说明书 Pd.02
    /// 0=8N2, 1=8E1, 2=8O1, 3=8N1(默认)
    /// </summary>
    public const ushort Pd_02_DataFormat = 0xFD02;

    // ===== 监控参数（C组）=====

    /// <summary>
    /// C0.01 - 输出电流（A）
    /// 参考：LM1000H 说明书 C0.01
    /// 实时监控变频器输出电流
    /// </summary>
    public const ushort C0_01_OutputCurrent = 0x5001;

    /// <summary>
    /// C0.14 - 变频器输出功率（kW）
    /// 参考：LM1000H 说明书 C0.14
    /// 实时监控输出功率
    /// </summary>
    public const ushort C0_14_OutputPower = 0x500E;

    /// <summary>
    /// C0.26 - 编码器反馈频率（Hz）
    /// 参考：LM1000H 说明书 C0.26
    /// 换算公式：线速度(m/s) = 频率(Hz) ÷ 10；线速度(mm/s) = 频率(Hz) × 100
    /// 单位：Hz/Count = 0.01（即寄存器值÷100得到实际Hz）
    /// </summary>
    public const ushort C0_26_EncoderFrequency = 0x501A;

    /// <summary>
    /// C0.29 - 转矩给定值（监控）
    /// 参考：LM1000H 说明书 C0.29
    /// P2.06 的百分比监控
    /// </summary>
    public const ushort C0_29_TorqueGiven = 0x501D;

    /// <summary>
    /// C0.32 - 运行状态
    /// 参考：LM1000H 说明书 C0.32
    /// 1=正转, 2=反转, 3=停止, 4=调谐, 5=故障
    /// </summary>
    public const ushort C0_32_RunStatus = 0x5020;

    // ===== 高级参数（HD组）=====

    /// <summary>
    /// HD.31 - 制动/减速停机切换频率
    /// 参考：LM1000H 说明书 HD.31
    /// 默认40（=4Hz），制动生效且频率降至此值时，切换为自由停机
    /// </summary>
    public const ushort HD_31_BrakeSwitchFreq = 0xAD1F;

    // ===== 故障基址 =====

    /// <summary>
    /// 故障信息表基址
    /// 参考：LM1000H 说明书 - 故障代码定义
    /// 0x0000=无故障, 0x0001=E001加速过流, 0x0002=E002减速过流, ...
    /// </summary>
    public const ushort Fault_Base = 0x3100;
}
