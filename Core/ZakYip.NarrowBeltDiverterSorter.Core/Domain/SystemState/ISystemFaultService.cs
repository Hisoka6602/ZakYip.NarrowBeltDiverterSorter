using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;

/// <summary>
/// 系统故障服务接口
/// 负责管理系统故障的注册、查询和清除
/// </summary>
public interface ISystemFaultService
{
    /// <summary>
    /// 获取当前所有活动故障
    /// </summary>
    IReadOnlyList<SystemFault> GetActiveFaults();

    /// <summary>
    /// 检查是否存在阻断运行的故障
    /// </summary>
    bool HasBlockingFault();

    /// <summary>
    /// 注册一个新故障
    /// </summary>
    /// <param name="faultCode">故障代码</param>
    /// <param name="message">故障消息</param>
    /// <param name="isBlocking">是否阻断系统运行</param>
    /// <param name="exception">关联的异常（可选）</param>
    void RegisterFault(SystemFaultCode faultCode, string message, bool isBlocking = true, Exception? exception = null);

    /// <summary>
    /// 清除指定故障
    /// </summary>
    /// <param name="faultCode">要清除的故障代码</param>
    /// <returns>是否成功清除</returns>
    bool ClearFault(SystemFaultCode faultCode);

    /// <summary>
    /// 清除所有故障
    /// </summary>
    void ClearAllFaults();

    /// <summary>
    /// 故障添加事件
    /// </summary>
    event EventHandler<SystemFaultEventArgs>? FaultAdded;

    /// <summary>
    /// 故障清除事件
    /// </summary>
    event EventHandler<SystemFaultCode>? FaultCleared;
}
