using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 系统健康检查
/// 检查主线状态、小车环状态和事件总线积压量
/// </summary>
public class SystemHealthCheck : IHealthCheck
{
    private readonly IMainLineFeedbackPort _mainLineFeedback;
    private readonly ICartRingBuilder _cartRingBuilder;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SystemHealthCheck> _logger;

    public SystemHealthCheck(
        IMainLineFeedbackPort mainLineFeedback,
        ICartRingBuilder cartRingBuilder,
        IEventBus eventBus,
        ILogger<SystemHealthCheck> logger)
    {
        _mainLineFeedback = mainLineFeedback;
        _cartRingBuilder = cartRingBuilder;
        _eventBus = eventBus;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>();
            var isHealthy = true;
            var messages = new List<string>();

            // 检查主线状态
            var mainLineStatus = _mainLineFeedback.GetCurrentStatus();
            var mainLineSpeed = _mainLineFeedback.GetCurrentSpeed();
            var faultCode = _mainLineFeedback.GetFaultCode();

            data["主线状态"] = mainLineStatus.ToString();
            data["主线速度_mm/s"] = mainLineSpeed;
            
            if (faultCode.HasValue)
            {
                data["故障代码"] = faultCode.Value;
                messages.Add($"主线故障代码: {faultCode.Value}");
                isHealthy = false;
            }

            if (mainLineStatus != MainLineStatus.Running)
            {
                messages.Add($"主线未运行，当前状态: {mainLineStatus}");
                isHealthy = false;
            }

            // 检查小车环状态
            var cartRingSnapshot = _cartRingBuilder.CurrentSnapshot;
            if (cartRingSnapshot == null)
            {
                data["小车环状态"] = "未构建";
                messages.Add("小车环尚未构建");
                isHealthy = false;
            }
            else
            {
                data["小车环状态"] = "已构建";
                data["小车数量"] = cartRingSnapshot.RingLength.Value;
                data["构建时间"] = cartRingSnapshot.BuiltAt.ToString("yyyy-MM-dd HH:mm:ss");
            }

            // 检查事件总线积压量
            var backlogCount = _eventBus.GetBacklogCount();
            data["事件总线积压量"] = backlogCount;

            if (backlogCount > 1000)
            {
                messages.Add($"事件总线积压过多: {backlogCount} 个事件");
                isHealthy = false;
            }
            else if (backlogCount > 100)
            {
                messages.Add($"事件总线积压较多: {backlogCount} 个事件");
            }

            var description = messages.Count > 0
                ? string.Join("; ", messages)
                : "系统运行正常";

            var status = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;

            return Task.FromResult(new HealthCheckResult(
                status,
                description,
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "健康检查发生异常");
            return Task.FromResult(new HealthCheckResult(
                HealthStatus.Unhealthy,
                "健康检查发生异常",
                ex));
        }
    }
}
