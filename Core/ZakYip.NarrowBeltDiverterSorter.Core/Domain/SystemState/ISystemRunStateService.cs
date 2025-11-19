using ZakYip.NarrowBeltDiverterSorter.Shared.Kernel;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;

/// <summary>
/// 系统运行状态服务接口
/// 负责管理系统状态机和按钮操作的验证逻辑
/// </summary>
public interface ISystemRunStateService
{
    /// <summary>
    /// 获取当前系统状态
    /// </summary>
    SystemRunState Current { get; }

    /// <summary>
    /// 处理启动按钮操作
    /// </summary>
    /// <returns>操作结果</returns>
    OperationResult TryHandleStart();

    /// <summary>
    /// 处理停止按钮操作
    /// </summary>
    /// <returns>操作结果</returns>
    OperationResult TryHandleStop();

    /// <summary>
    /// 处理急停按钮操作
    /// </summary>
    /// <returns>操作结果</returns>
    OperationResult TryHandleEmergencyStop();

    /// <summary>
    /// 处理急停解除操作
    /// </summary>
    /// <returns>操作结果</returns>
    OperationResult TryHandleEmergencyReset();

    /// <summary>
    /// 验证是否可以创建包裹
    /// </summary>
    /// <returns>操作结果</returns>
    OperationResult ValidateCanCreateParcel();

    /// <summary>
    /// 强制系统进入故障状态
    /// 由外部故障管理服务调用
    /// </summary>
    /// <param name="reason">故障原因</param>
    void ForceToFaultState(string reason);
}
