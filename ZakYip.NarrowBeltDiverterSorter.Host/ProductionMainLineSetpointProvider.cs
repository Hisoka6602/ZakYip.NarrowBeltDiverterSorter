using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 生产环境主线设定点提供者
/// 从配置中读取目标速度，通过启动模式控制是否启用
/// </summary>
public class ProductionMainLineSetpointProvider : IMainLineSetpointProvider
{
    private readonly MainLineControlOptions _options;
    private readonly StartupModeConfiguration _startupConfig;

    public ProductionMainLineSetpointProvider(
        IOptions<MainLineControlOptions> options,
        StartupModeConfiguration startupConfig)
    {
        _options = options.Value;
        _startupConfig = startupConfig;
    }

    /// <inheritdoc/>
    public bool IsEnabled => _startupConfig.ShouldStartMainLineControl();

    /// <inheritdoc/>
    public decimal TargetMmps => _options.TargetSpeedMmps;
}
