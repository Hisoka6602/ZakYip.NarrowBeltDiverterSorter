using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 主驱动线反馈端口接口
/// 对应物理连接：主线驱动器反馈通道（读取实际运行状态和速度）
/// </summary>
public interface IMainLineFeedbackPort
{
    /// <summary>
    /// 获取当前实际线速
    /// </summary>
    /// <returns>当前线速（mm/s）</returns>
    double GetCurrentSpeed();

    /// <summary>
    /// 获取当前主线状态
    /// </summary>
    /// <returns>主线状态</returns>
    MainLineStatus GetCurrentStatus();

    /// <summary>
    /// 读取故障码
    /// </summary>
    /// <returns>故障码（如果没有故障返回null）</returns>
    int? GetFaultCode();
}
