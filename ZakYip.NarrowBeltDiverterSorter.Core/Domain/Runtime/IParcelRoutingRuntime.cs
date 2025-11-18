namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Runtime;

/// <summary>
/// 包裹路由运行时接口
/// 提供包裹路由循环的可重用实现，不依赖于 ASP.NET Hosting
/// </summary>
public interface IParcelRoutingRuntime
{
    /// <summary>
    /// 运行包裹路由循环
    /// </summary>
    /// <param name="cancellationToken">取消令牌，用于停止运行时</param>
    /// <returns>异步任务</returns>
    Task RunAsync(CancellationToken cancellationToken);
}
