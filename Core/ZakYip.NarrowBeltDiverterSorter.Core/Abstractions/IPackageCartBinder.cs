namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 包裹小车绑定服务接口
/// 负责根据格口绑定小车号，确保所有包裹创建路径统一经过此接口
/// </summary>
public interface IPackageCartBinder
{
    /// <summary>
    /// 为新包裹绑定小车号
    /// </summary>
    /// <param name="packageId">包裹ID</param>
    /// <param name="chuteId">格口ID</param>
    /// <returns>绑定的小车号（1 基索引）</returns>
    /// <exception cref="InvalidOperationException">当小车状态未准备好时抛出</exception>
    int BindCartForNewPackage(long packageId, long chuteId);
}
