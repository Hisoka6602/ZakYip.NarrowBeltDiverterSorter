using ZakYip.NarrowBeltDiverterSorter.Shared.Kernel;

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

    /// <summary>
    /// 执行首次稳速时的 IO 联动
    /// 写入所有"首次稳速时联动"的输出通道
    /// 在一次启动→运行周期中，当线体速度首次进入稳速状态时触发
    /// </summary>
    /// <returns>操作结果</returns>
    Task<OperationResult> ExecuteFirstStableSpeedLinkageAsync();

    /// <summary>
    /// 执行稳速后不稳速时的 IO 联动
    /// 写入所有"稳速后的每次不稳速时联动"的输出通道
    /// 前提：本次运行中线体已至少有一次进入过稳速状态
    /// 每次从稳速状态退化为不稳速状态时触发
    /// </summary>
    /// <returns>操作结果</returns>
    Task<OperationResult> ExecuteUnstableAfterStableLinkageAsync();
}
