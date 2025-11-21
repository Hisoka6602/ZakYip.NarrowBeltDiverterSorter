using System.ComponentModel.DataAnnotations;

namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 长跑高负载测试选项 DTO。
/// </summary>
public sealed record LongRunLoadTestOptionsDto
{
    /// <summary>
    /// 目标包裹总数。
    /// </summary>
    [Required(ErrorMessage = "目标包裹总数不能为空")]
    [Range(1, 1000000, ErrorMessage = "目标包裹总数必须在 1 到 1000000 之间")]
    public required int TargetParcelCount { get; init; }

    /// <summary>
    /// 包裹创建间隔（毫秒）。
    /// </summary>
    [Required(ErrorMessage = "包裹创建间隔不能为空")]
    [Range(1, 60000, ErrorMessage = "包裹创建间隔必须在 1 到 60000 之间")]
    public required int ParcelCreationIntervalMs { get; init; }

    /// <summary>
    /// 格口数量。
    /// </summary>
    [Required(ErrorMessage = "格口数量不能为空")]
    [Range(1, 1000, ErrorMessage = "格口数量必须在 1 到 1000 之间")]
    public required int ChuteCount { get; init; }

    /// <summary>
    /// 单个格口宽度（毫米）。
    /// </summary>
    [Required(ErrorMessage = "格口宽度不能为空")]
    [Range(1, 10000, ErrorMessage = "格口宽度必须在 1 到 10000 之间")]
    public required decimal ChuteWidthMm { get; init; }

    /// <summary>
    /// 主线稳态速度（毫米/秒）。
    /// </summary>
    [Required(ErrorMessage = "主线速度不能为空")]
    [Range(0.1, 10000, ErrorMessage = "主线速度必须在 0.1 到 10000 之间")]
    public required decimal MainLineSpeedMmps { get; init; }

    /// <summary>
    /// 小车宽度（毫米）。
    /// </summary>
    [Required(ErrorMessage = "小车宽度不能为空")]
    [Range(1, 10000, ErrorMessage = "小车宽度必须在 1 到 10000 之间")]
    public required decimal CartWidthMm { get; init; }

    /// <summary>
    /// 小车节距（毫米）。
    /// </summary>
    [Required(ErrorMessage = "小车节距不能为空")]
    [Range(1, 10000, ErrorMessage = "小车节距必须在 1 到 10000 之间")]
    public required decimal CartSpacingMm { get; init; }

    /// <summary>
    /// 小车数量。
    /// </summary>
    [Required(ErrorMessage = "小车数量不能为空")]
    [Range(1, 10000, ErrorMessage = "小车数量必须在 1 到 10000 之间")]
    public required int CartCount { get; init; }

    /// <summary>
    /// 异常口格口编号。
    /// </summary>
    [Required(ErrorMessage = "异常口编号不能为空")]
    public required int ExceptionChuteId { get; init; }

    /// <summary>
    /// 包裹长度最小值（毫米）。
    /// </summary>
    [Required(ErrorMessage = "包裹最小长度不能为空")]
    [Range(1, 10000, ErrorMessage = "包裹最小长度必须在 1 到 10000 之间")]
    public required decimal MinParcelLengthMm { get; init; }

    /// <summary>
    /// 包裹长度最大值（毫米）。
    /// </summary>
    [Required(ErrorMessage = "包裹最大长度不能为空")]
    [Range(1, 10000, ErrorMessage = "包裹最大长度必须在 1 到 10000 之间")]
    public required decimal MaxParcelLengthMm { get; init; }

    /// <summary>
    /// 当预测无法安全分拣时是否强制改派至异常口。
    /// </summary>
    [Required(ErrorMessage = "强制异常口标志不能为空")]
    public required bool ForceToExceptionChuteOnConflict { get; init; }

    /// <summary>
    /// 入口到落车点距离（毫米）。
    /// </summary>
    [Required(ErrorMessage = "入口到落车点距离不能为空")]
    [Range(1, 100000, ErrorMessage = "入口到落车点距离必须在 1 到 100000 之间")]
    public required decimal InfeedToDropDistanceMm { get; init; }

    /// <summary>
    /// 入口输送线速度（毫米/秒）。
    /// </summary>
    [Required(ErrorMessage = "入口输送线速度不能为空")]
    [Range(0.1, 10000, ErrorMessage = "入口输送线速度必须在 0.1 到 10000 之间")]
    public required decimal InfeedConveyorSpeedMmps { get; init; }
}
