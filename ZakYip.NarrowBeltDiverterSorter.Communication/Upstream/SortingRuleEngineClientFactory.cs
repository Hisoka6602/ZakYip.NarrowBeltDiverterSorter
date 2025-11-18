using Microsoft.Extensions.Logging;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 分拣规则引擎客户端工厂
/// 根据配置的 Mode 创建相应的客户端实现
/// </summary>
public class SortingRuleEngineClientFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public SortingRuleEngineClientFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// 根据配置创建客户端实例
    /// </summary>
    public ISortingRuleEngineClient CreateClient(UpstreamOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return options.Mode switch
        {
            UpstreamMode.Disabled => new DisabledSortingRuleEngineClient(
                _loggerFactory.CreateLogger<DisabledSortingRuleEngineClient>(),
                options.DefaultChuteNumber),

            UpstreamMode.Mqtt => new MqttSortingRuleEngineClient(
                _loggerFactory.CreateLogger<MqttSortingRuleEngineClient>(),
                options.Mqtt ?? throw new InvalidOperationException("MQTT 配置不能为空")),

            UpstreamMode.Tcp => new TcpSortingRuleEngineClient(
                _loggerFactory.CreateLogger<TcpSortingRuleEngineClient>(),
                options.Tcp ?? throw new InvalidOperationException("TCP 配置不能为空")),

            _ => throw new NotSupportedException($"不支持的上游模式: {options.Mode}")
        };
    }
}
