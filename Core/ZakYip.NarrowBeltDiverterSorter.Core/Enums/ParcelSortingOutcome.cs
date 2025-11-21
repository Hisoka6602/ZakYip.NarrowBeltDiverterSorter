using System.ComponentModel;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums;

/// <summary>
/// 包裹分拣结果
/// </summary>
public enum ParcelSortingOutcome
{
    /// <summary>
    /// 正常落格
    /// </summary>
    [Description("正常落格")]
    NormalSort = 0,

    /// <summary>
    /// 强排
    /// </summary>
    [Description("强排")]
    ForceEject = 1,

    /// <summary>
    /// 误分
    /// </summary>
    [Description("误分")]
    Missort = 2,

    /// <summary>
    /// 未处理
    /// </summary>
    [Description("未处理")]
    Unprocessed = 3
}
