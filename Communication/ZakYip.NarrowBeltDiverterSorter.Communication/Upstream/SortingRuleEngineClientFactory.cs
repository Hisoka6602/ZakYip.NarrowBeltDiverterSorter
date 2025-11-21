using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Communication;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 规则引擎客户端工厂
/// 根据配置 Mode 创建具体的客户端实现
/// </summary>
public class SortingRuleEngineClientFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public SortingRuleEngineClientFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// 根据上游配置创建客户端
    /// </summary>
    /// <param name="options">上游配置选项</param>
    /// <returns>规则引擎客户端实例</returns>
    public ISortingRuleEngineClient CreateClient(UpstreamOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        return options.Mode switch
        {
            UpstreamMode.Disabled => CreateDisabledClient(),
            UpstreamMode.Mqtt => CreateMqttClient(options.Mqtt),
            UpstreamMode.Tcp => CreateTcpClient(options.Tcp),
            _ => throw new NotSupportedException($"不支持的上游模式: {options.Mode}")
        };
    }

    private ISortingRuleEngineClient CreateDisabledClient()
    {
        var logger = _loggerFactory.CreateLogger<DisabledSortingRuleEngineClient>();
        return new DisabledSortingRuleEngineClient(logger);
    }

    private ISortingRuleEngineClient CreateMqttClient(MqttOptions? mqttOptions)
    {
        if (mqttOptions == null)
        {
            throw new InvalidOperationException("MQTT 模式下必须提供 MQTT 配置");
        }

        var logger = _loggerFactory.CreateLogger<MqttSortingRuleEngineClient>();
        return new MqttSortingRuleEngineClient(mqttOptions, logger);
    }

    private ISortingRuleEngineClient CreateTcpClient(TcpOptions? tcpOptions)
    {
        if (tcpOptions == null)
        {
            throw new InvalidOperationException("TCP 模式下必须提供 TCP 配置");
        }

        var logger = _loggerFactory.CreateLogger<TcpSortingRuleEngineClient>();
        return new TcpSortingRuleEngineClient(tcpOptions, logger);
    }
}
