using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// 基于 MQTT 协议的分拣规则引擎客户端实现
/// 使用 MQTTnet 库连接到 MQTT Broker，与 RuleEngine 进行消息交互
/// </summary>
public class MqttSortingRuleEngineClient : ISortingRuleEngineClient, IDisposable
{
    private readonly ILogger<MqttSortingRuleEngineClient> _logger;
    private readonly MqttConfiguration _config;
    private readonly IMqttClient _mqttClient;
    private UpstreamConnectionState _connectionState;
    private Func<long, ValueTask<int>>? _sortingRequestHandler;

    public MqttSortingRuleEngineClient(
        ILogger<MqttSortingRuleEngineClient> logger,
        MqttConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();
        _connectionState = UpstreamConnectionState.Disconnected;

        // 注册连接和断开事件处理
        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
        _mqttClient.ConnectedAsync += OnConnectedAsync;
        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
    }

    public UpstreamConnectionState ConnectionState => _connectionState;

    public async ValueTask ConnectAsync(CancellationToken ct = default)
    {
        if (_connectionState == UpstreamConnectionState.Connected)
        {
            _logger.LogWarning("MQTT 客户端已连接，跳过重复连接");
            return;
        }

        try
        {
            _connectionState = UpstreamConnectionState.Connecting;
            _logger.LogInformation("正在连接 RuleEngine MQTT Broker: {Broker}:{Port}", _config.Broker, _config.Port);

            var clientId = $"{_config.ClientIdPrefix}-{Guid.NewGuid():N}";
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(_config.Broker, _config.Port)
                .WithClientId(clientId)
                .WithTimeout(TimeSpan.FromSeconds(_config.ConnectionTimeoutSeconds))
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(_config.KeepAliveSeconds))
                .WithCleanSession();

            // 如果配置了用户名和密码，添加认证
            if (!string.IsNullOrWhiteSpace(_config.Username))
            {
                optionsBuilder.WithCredentials(_config.Username, _config.Password);
            }

            var options = optionsBuilder.Build();
            await _mqttClient.ConnectAsync(options, ct);

            _logger.LogInformation("RuleEngine MQTT 连接成功，客户端ID: {ClientId}", clientId);
            _connectionState = UpstreamConnectionState.Connected;
        }
        catch (Exception ex)
        {
            _connectionState = UpstreamConnectionState.Error;
            _logger.LogError(ex, "RuleEngine MQTT 连接失败: {Message}", ex.Message);
            throw;
        }
    }

    public async ValueTask DisconnectAsync(CancellationToken ct = default)
    {
        try
        {
            if (_mqttClient.IsConnected)
            {
                _logger.LogInformation("正在断开 RuleEngine MQTT 连接");
                await _mqttClient.DisconnectAsync(cancellationToken: ct);
            }
            _connectionState = UpstreamConnectionState.Disconnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "断开 MQTT 连接时发生错误: {Message}", ex.Message);
        }
    }

    public async ValueTask PublishParcelCreatedAsync(ParcelCreatedMessage message, CancellationToken ct = default)
    {
        await PublishMessageAsync($"{_config.BaseTopic}/parcel-created", message, ct);
    }

    public async ValueTask PublishDwsDataAsync(DwsDataMessage message, CancellationToken ct = default)
    {
        await PublishMessageAsync($"{_config.BaseTopic}/dws-data", message, ct);
    }

    public async ValueTask PublishSortingResultAsync(SortingResultMessage message, CancellationToken ct = default)
    {
        await PublishMessageAsync($"{_config.BaseTopic}/sorting-result", message, ct);
    }

    public async ValueTask SubscribeToSortingRequestsAsync(Func<long, ValueTask<int>> onSortingRequest, CancellationToken ct = default)
    {
        _sortingRequestHandler = onSortingRequest ?? throw new ArgumentNullException(nameof(onSortingRequest));

        var topic = $"{_config.BaseTopic}/sorting-request";
        _logger.LogInformation("订阅分拣请求主题: {Topic}", topic);

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(topic)
            .Build();

        await _mqttClient.SubscribeAsync(subscribeOptions, ct);
    }

    private async ValueTask PublishMessageAsync<T>(string topic, T message, CancellationToken ct)
    {
        try
        {
            if (!_mqttClient.IsConnected)
            {
                _logger.LogWarning("MQTT 未连接，无法发布消息到主题: {Topic}", topic);
                return;
            }

            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(json)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(mqttMessage, ct);
            _logger.LogDebug("已发布消息到主题 {Topic}: {Payload}", topic, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布消息到主题 {Topic} 失败: {Message}", topic, ex.Message);
        }
    }

    private Task OnConnectedAsync(MqttClientConnectedEventArgs args)
    {
        _connectionState = UpstreamConnectionState.Connected;
        _logger.LogInformation("MQTT 客户端已连接");
        return Task.CompletedTask;
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        _connectionState = UpstreamConnectionState.Disconnected;
        _logger.LogWarning("MQTT 客户端已断开: {Reason}", args.Reason);
        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var topic = args.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

            _logger.LogDebug("收到 MQTT 消息，主题: {Topic}, 载荷: {Payload}", topic, payload);

            // 处理分拣请求响应
            if (topic == $"{_config.BaseTopic}/sorting-request" && _sortingRequestHandler != null)
            {
                // 假设载荷是 JSON 格式，包含 parcelId 字段
                var request = JsonSerializer.Deserialize<SortingRequestPayload>(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (request != null)
                {
                    var chuteNumber = await _sortingRequestHandler(request.ParcelId);
                    _logger.LogDebug("处理分拣请求 ParcelId={ParcelId}, 分配格口={ChuteNumber}", request.ParcelId, chuteNumber);

                    // 可以在这里发布响应消息（如果 RuleEngine 需要）
                    var response = new
                    {
                        parcelId = request.ParcelId,
                        chuteNumber = chuteNumber,
                        timestamp = DateTimeOffset.UtcNow
                    };
                    await PublishMessageAsync($"{_config.BaseTopic}/sorting-response", response, CancellationToken.None);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 MQTT 消息时发生错误: {Message}", ex.Message);
        }
    }

    public void Dispose()
    {
        _mqttClient?.Dispose();
    }

    // 内部 DTO 用于解析 MQTT 消息
    private class SortingRequestPayload
    {
        public long ParcelId { get; set; }
    }
}
