namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime;

/// <summary>
/// 主线控制运行时接口
/// 提供主线控制循环的可重用实现，不依赖于 ASP.NET Hosting
/// </summary>
public interface IMainLineRuntime
{
    /// <summary>
    /// 运行主线控制循环
    /// </summary>
    /// <param name="cancellationToken">取消令牌，用于停止运行时</param>
    /// <returns>异步任务</returns>
    Task RunAsync(CancellationToken cancellationToken);
}
