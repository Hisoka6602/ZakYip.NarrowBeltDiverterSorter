using System.ComponentModel.DataAnnotations;

namespace ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

/// <summary>
/// SignalR 推送配置DTO
/// </summary>
public sealed record SignalRPushConfigurationDto
{
    [Required(ErrorMessage = "线速推送间隔不能为空")]
    [Range(10, 10000, ErrorMessage = "线速推送间隔必须在 10 到 10000 毫秒之间")]
    public int LineSpeedPushIntervalMs { get; set; } = 200;
    
    [Required(ErrorMessage = "格口小车推送间隔不能为空")]
    [Range(10, 10000, ErrorMessage = "格口小车推送间隔必须在 10 到 10000 毫秒之间")]
    public int ChuteCartPushIntervalMs { get; set; } = 100;
    
    [Required(ErrorMessage = "原点小车推送间隔不能为空")]
    [Range(10, 10000, ErrorMessage = "原点小车推送间隔必须在 10 到 10000 毫秒之间")]
    public int OriginCartPushIntervalMs { get; set; } = 100;
    
    [Required(ErrorMessage = "包裹创建推送间隔不能为空")]
    [Range(10, 10000, ErrorMessage = "包裹创建推送间隔必须在 10 到 10000 毫秒之间")]
    public int ParcelCreatedPushIntervalMs { get; set; } = 50;
    
    [Required(ErrorMessage = "包裹分拣推送间隔不能为空")]
    [Range(10, 10000, ErrorMessage = "包裹分拣推送间隔必须在 10 到 10000 毫秒之间")]
    public int ParcelDivertedPushIntervalMs { get; set; } = 50;
    
    [Required(ErrorMessage = "设备状态推送间隔不能为空")]
    [Range(10, 10000, ErrorMessage = "设备状态推送间隔必须在 10 到 10000 毫秒之间")]
    public int DeviceStatusPushIntervalMs { get; set; } = 500;
    
    [Required(ErrorMessage = "小车布局推送间隔不能为空")]
    [Range(10, 10000, ErrorMessage = "小车布局推送间隔必须在 10 到 10000 毫秒之间")]
    public int CartLayoutPushIntervalMs { get; set; } = 500;
    
    [Required(ErrorMessage = "在线包裹推送周期不能为空")]
    [Range(100, 60000, ErrorMessage = "在线包裹推送周期必须在 100 到 60000 毫秒之间")]
    public int OnlineParcelsPushPeriodMs { get; set; } = 1000;
    
    [Required(ErrorMessage = "启用在线包裹推送标志不能为空")]
    public bool EnableOnlineParcelsPush { get; set; } = true;
}
