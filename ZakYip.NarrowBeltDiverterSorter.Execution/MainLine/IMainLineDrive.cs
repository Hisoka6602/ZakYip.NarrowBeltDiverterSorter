namespace ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;

/// <summary>
/// 主驱动线控制接口。
/// - 负责设置目标线速；
/// - 负责读取当前线速；
/// - 对外暴露"是否稳定"等状态。
/// </summary>
public interface IMainLineDrive
{
    /// <summary>设置目标线速，单位：mm/s。</summary>
    Task SetTargetSpeedAsync(decimal targetSpeedMmps, CancellationToken cancellationToken = default);

    /// <summary>当前实际线速，单位：mm/s。</summary>
    decimal CurrentSpeedMmps { get; }

    /// <summary>当前目标线速，单位：mm/s。</summary>
    decimal TargetSpeedMmps { get; }

    /// <summary>当前是否满足"稳速"判定。</summary>
    bool IsSpeedStable { get; }
    
    /// <summary>
    /// 异步读取当前速度，单位：mm/s。
    /// 对于仿真模式，返回内部模拟速度；对于 Rema 模式，从硬件读取真实速度。
    /// </summary>
    Task<decimal> GetCurrentSpeedAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 初始化驱动器，执行安全启动流程。
    /// 对于 Rema 模式：
    /// 1. 确保先发送停止命令（保证当前频率为 0，避免带载启动）
    /// 2. 读取关键参数（例如 P0.05 顶频、P2.06 电机额定电流）并校验
    /// 3. 设置限频/限扭矩相关参数
    /// 对于仿真模式：仅日志输出，不发真实命令
    /// </summary>
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 关闭驱动器，执行安全停机流程。
    /// 对于 Rema 模式：
    /// 1. 将目标速度设置为 0
    /// 2. 等待当前速度降到阈值以下
    /// 3. 发送停止命令
    /// 对于仿真模式：仅日志输出，不发真实命令
    /// </summary>
    Task<bool> ShutdownAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 主线是否已就绪（是否已成功初始化）
    /// </summary>
    bool IsReady { get; }
}
