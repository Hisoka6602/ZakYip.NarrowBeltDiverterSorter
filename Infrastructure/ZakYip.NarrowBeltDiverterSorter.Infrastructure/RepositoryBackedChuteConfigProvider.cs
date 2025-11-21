using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure;

/// <summary>
/// 基于配置仓储的格口配置提供者
/// </summary>
public class RepositoryBackedChuteConfigProvider : IChuteConfigProvider
{
    private readonly IChuteConfigRepository _repository;
    private ChuteConfigSet? _configSet;
    private readonly object _lock = new();

    public RepositoryBackedChuteConfigProvider(IChuteConfigRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc/>
    public IReadOnlyList<ChuteConfig> GetAllConfigs()
    {
        EnsureLoaded();
        return _configSet!.Configs.AsReadOnly();
    }

    /// <inheritdoc/>
    public ChuteConfig? GetConfig(ChuteId chuteId)
    {
        EnsureLoaded();
        return _configSet!.Configs.FirstOrDefault(c => c.ChuteId.Equals(chuteId));
    }

    private void EnsureLoaded()
    {
        if (_configSet == null)
        {
            lock (_lock)
            {
                if (_configSet == null)
                {
                    _configSet = _repository.LoadAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
        }
    }
}
