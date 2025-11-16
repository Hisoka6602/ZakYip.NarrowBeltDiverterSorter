namespace ZakYip.NarrowBeltDiverterSorter.Execution.MainLine.Rema;

/// <summary>
/// 雷马 LM1000H 变频驱动器客户端接口
/// 封装对 LM1000H 的高级业务操作，隔离底层 Modbus 通讯细节
/// 参考：LM1000H 说明书 - Modbus 通讯协议
/// </summary>
public interface IRemaLm1000HClient : IDisposable
{
    /// <summary>
    /// 连接到变频驱动器
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task<OperationResult> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开与变频驱动器的连接
    /// </summary>
    Task<OperationResult> DisconnectAsync();

    /// <summary>
    /// 检查连接状态
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 启动电机（正转）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task<OperationResult> StartForwardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止电机（减速停机）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task<OperationResult> StopDecelerateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置目标频率（Hz）
    /// 写入 P0.07 限速频率寄存器
    /// 参考：LM1000H 说明书 P0.07 - 限速频率
    /// </summary>
    /// <param name="targetHz">目标频率（Hz）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<OperationResult> SetTargetFrequencyAsync(decimal targetHz, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置目标线速（mm/s）
    /// 自动转换为频率后写入 P0.07 限速频率寄存器
    /// 参考：LM1000H 说明书 P0.07 - 限速频率，C0.26 换算公式
    /// </summary>
    /// <param name="targetMmps">目标线速（mm/s）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<OperationResult> SetTargetSpeedAsync(decimal targetMmps, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取当前实际频率（Hz）
    /// 从 C0.26 编码器反馈频率寄存器读取
    /// 参考：LM1000H 说明书 C0.26 - 编码器反馈频率
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task<OperationResult<decimal>> ReadCurrentFrequencyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取当前实际线速（mm/s）
    /// 从 C0.26 编码器反馈频率读取并转换为线速
    /// 参考：LM1000H 说明书 C0.26 换算公式
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task<OperationResult<decimal>> ReadCurrentSpeedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置扭矩上限
    /// 写入 P3.10 转矩给定值寄存器
    /// 参考：LM1000H 说明书 P3.10 - 转矩给定值（0-1000 = 0-100% 额定电流）
    /// </summary>
    /// <param name="torqueLimit">扭矩上限（0-1000）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<OperationResult> SetTorqueLimitAsync(int torqueLimit, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取输出电流（A）
    /// 从 C0.01 输出电流寄存器读取
    /// 参考：LM1000H 说明书 C0.01 - 输出电流
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task<OperationResult<decimal>> ReadOutputCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取运行状态
    /// 从 C0.32 运行状态寄存器读取
    /// 参考：LM1000H 说明书 C0.32 - 运行状态（1=正转, 2=反转, 3=停止, 4=调谐, 5=故障）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task<OperationResult<int>> ReadRunStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 初始化变频驱动器参数
    /// 设置运行命令源、限速频率、扭矩上限等参数
    /// 参考：LM1000H 说明书 - 参数设定
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task<OperationResult> InitializeAsync(CancellationToken cancellationToken = default);
}
