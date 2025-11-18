using ZakYip.NarrowBeltDiverterSorter.Core.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 面板 IO 协调器接口
/// 负责处理系统状态变化时的 IO 联动操作
/// </summary>
public interface IPanelIoCoordinator
{
    /// <summary>
    /// 执行启动操作的 IO 联动
    /// 写入所有"跟随启动"的输出通道
    /// </summary>
    /// <returns>操作结果</returns>
    Task<OperationResult> ExecuteStartLinkageAsync();

    /// <summary>
    /// 执行停止操作的 IO 联动
    /// 写入所有"跟随停止"的输出通道
    /// </summary>
    /// <returns>操作结果</returns>
    Task<OperationResult> ExecuteStopLinkageAsync();
}
