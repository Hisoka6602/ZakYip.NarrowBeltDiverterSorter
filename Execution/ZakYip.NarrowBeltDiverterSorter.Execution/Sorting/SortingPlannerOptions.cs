namespace ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

/// <summary>
/// 分拣规划器配置选项
/// </summary>
public class SortingPlannerOptions
{
    /// <summary>
    /// 小车间距（毫米）
    /// </summary>
    public decimal CartSpacingMm { get; set; } = 500m;
}
