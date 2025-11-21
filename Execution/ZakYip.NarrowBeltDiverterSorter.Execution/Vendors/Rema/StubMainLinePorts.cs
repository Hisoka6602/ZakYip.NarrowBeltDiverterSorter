using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Vendors.Rema;

/// <summary>
/// 占位符主线驱动端口
/// 用于 RemaLm1000H 模式下满足依赖注入要求
/// 实际控制逻辑在 RemaLm1000HMainLineDrive 中实现
/// </summary>
public sealed class StubMainLineDrivePort : IMainLineDrivePort
{
    public Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        // RemaLm1000H 驱动自行管理启动逻辑
        return Task.FromResult(true);
    }

    public Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        // RemaLm1000H 驱动自行管理停止逻辑
        return Task.FromResult(true);
    }

    public Task<bool> EmergencyStopAsync(CancellationToken cancellationToken = default)
    {
        // RemaLm1000H 驱动自行管理急停逻辑
        return Task.FromResult(true);
    }

    public Task<bool> SetTargetSpeedAsync(double targetSpeedMmps, CancellationToken cancellationToken = default)
    {
        // RemaLm1000H 驱动通过 IMainLineDrive 接口直接设置速度
        return Task.FromResult(true);
    }
}

/// <summary>
/// 占位符主线反馈端口
/// 用于 RemaLm1000H 模式下满足依赖注入要求
/// 实际反馈数据在 RemaLm1000HMainLineDrive 中管理
/// </summary>
public sealed class StubMainLineFeedbackPort : IMainLineFeedbackPort
{
    public double GetCurrentSpeed()
    {
        // 返回 0，实际速度通过 IMainLineDrive.CurrentSpeedMmps 获取
        return 0.0;
    }

    public MainLineStatus GetCurrentStatus()
    {
        // 返回停止状态，实际状态在 RemaLm1000HMainLineDrive 中管理
        return MainLineStatus.Stopped;
    }

    public int? GetFaultCode()
    {
        // 无故障
        return null;
    }
}
