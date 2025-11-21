using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Mainline;

/// <summary>
/// 生产环境主线设定点提供者
/// 从配置中读取目标速度，只有在系统运行状态下才启用
/// </summary>
public class ProductionMainLineSetpointProvider : IMainLineSetpointProvider
{
    private readonly MainLineControlOptions _options;
    private readonly ISystemRunStateService _systemRunStateService;

    public ProductionMainLineSetpointProvider(
        IOptions<MainLineControlOptions> options,
        ISystemRunStateService systemRunStateService)
    {
        _options = options.Value;
        _systemRunStateService = systemRunStateService;
    }

    /// <inheritdoc/>
    /// <summary>
    /// 只有在系统运行状态下才启用主线
    /// </summary>
    public bool IsEnabled => _systemRunStateService.Current == SystemRunState.Running;

    /// <inheritdoc/>
    public decimal TargetMmps => _options.TargetSpeedMmps;
}
