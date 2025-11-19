namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// 仿真配置DTO
/// </summary>
public sealed record SimulationConfigurationDto
{
    public int TimeBetweenParcelsMs { get; set; } = 300;
    public int TotalParcels { get; set; } = 1000;
    public decimal MinParcelLengthMm { get; set; } = 200m;
    public decimal MaxParcelLengthMm { get; set; } = 800m;
    public int? RandomSeed { get; set; }
    public int ParcelTtlSeconds { get; set; } = 60;
}
