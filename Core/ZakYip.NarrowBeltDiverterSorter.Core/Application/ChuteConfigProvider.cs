using System.Collections.Concurrent;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Application;

/// <summary>
/// 格口配置提供者实现
/// 使用内存存储管理格口配置
/// </summary>
public class ChuteConfigProvider : IChuteConfigProvider
{
    private readonly ConcurrentDictionary<ChuteId, ChuteConfig> _configs = new();

    /// <inheritdoc/>
    public IReadOnlyList<ChuteConfig> GetAllConfigs()
    {
        return _configs.Values.ToList();
    }

    /// <inheritdoc/>
    public ChuteConfig? GetConfig(ChuteId chuteId)
    {
        return _configs.TryGetValue(chuteId, out var config) ? config : null;
    }

    /// <summary>
    /// 添加或更新格口配置
    /// </summary>
    /// <param name="config">格口配置</param>
    public void AddOrUpdate(ChuteConfig config)
    {
        _configs.AddOrUpdate(config.ChuteId, config, (_, _) => config);
    }

    /// <summary>
    /// 移除格口配置
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <returns>是否成功移除</returns>
    public bool Remove(ChuteId chuteId)
    {
        return _configs.TryRemove(chuteId, out _);
    }
}
