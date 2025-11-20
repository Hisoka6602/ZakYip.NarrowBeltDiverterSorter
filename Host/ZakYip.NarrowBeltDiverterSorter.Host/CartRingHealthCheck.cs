using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 小车环健康检查
/// 检查小车环配置是否匹配
/// </summary>
public class CartRingHealthCheck : IHealthCheck
{
    private readonly ICartRingHealthService _healthService;
    private readonly ILogger<CartRingHealthCheck> _logger;

    public CartRingHealthCheck(
        ICartRingHealthService healthService,
        ILogger<CartRingHealthCheck> logger)
    {
        _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = _healthService.GetHealthStatus();
            var data = new Dictionary<string, object>();

            if (status.IsHealthy)
            {
                data["小车环配置状态"] = "正常";
                return Task.FromResult(new HealthCheckResult(
                    HealthStatus.Healthy,
                    "小车环配置正常",
                    data: data));
            }
            else
            {
                data["小车环配置状态"] = "不匹配";
                if (status.ExpectedCartCount.HasValue)
                {
                    data["期望小车数量"] = status.ExpectedCartCount.Value;
                }
                if (status.DetectedCartCount.HasValue)
                {
                    data["实际小车数量"] = status.DetectedCartCount.Value;
                }

                return Task.FromResult(new HealthCheckResult(
                    HealthStatus.Unhealthy,
                    status.ErrorMessage ?? "小车环配置不匹配",
                    data: data));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "小车环健康检查发生异常");
            return Task.FromResult(new HealthCheckResult(
                HealthStatus.Unhealthy,
                "小车环健康检查发生异常",
                ex));
        }
    }
}
