using System.ComponentModel.DataAnnotations;

namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 仿真配置DTO
/// </summary>
public sealed record SimulationConfigurationDto
{
    [Required(ErrorMessage = "包裹间隔不能为空")]
    [Range(1, 60000, ErrorMessage = "包裹间隔必须在 1 到 60000 之间")]
    public int TimeBetweenParcelsMs { get; set; } = 300;
    
    [Required(ErrorMessage = "总包裹数不能为空")]
    [Range(1, 1000000, ErrorMessage = "总包裹数必须在 1 到 1000000 之间")]
    public int TotalParcels { get; set; } = 1000;
    
    [Required(ErrorMessage = "最小包裹长度不能为空")]
    [Range(1, 10000, ErrorMessage = "最小包裹长度必须在 1 到 10000 之间")]
    public decimal MinParcelLengthMm { get; set; } = 200m;
    
    [Required(ErrorMessage = "最大包裹长度不能为空")]
    [Range(1, 10000, ErrorMessage = "最大包裹长度必须在 1 到 10000 之间")]
    public decimal MaxParcelLengthMm { get; set; } = 800m;
    
    public int? RandomSeed { get; set; }
    
    [Required(ErrorMessage = "包裹生存时间不能为空")]
    [Range(1, 86400, ErrorMessage = "包裹生存时间必须在 1 到 86400 之间")]
    public int ParcelTtlSeconds { get; set; } = 60;
}
