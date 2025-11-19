using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;

/// <summary>
/// MQTT 协议的规则引擎客户端实现
/// </summary>
public class MqttSortingRuleEngineClient : ISortingRuleEngineClient
{
    private readonly MqttOptions _options;
    private readonly ILogger<MqttSortingRuleEngineClient> _logger;
    private IMqttClient? _mqttClient;
    private bool _disposed;

    public MqttSortingRuleEngineClient(
        MqttOptions options,
        ILogger<MqttSortingRuleEngineClient> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public bool IsConnected => _mqttClient?.IsConnected ?? false;

    /// <inheritdoc/>
    public event EventHandler<SortingResultMessage>? SortingResultReceived;

    /// <inheritdoc/>
    public event EventHandler<ChuteAssignmentNotificationEventArgs>? ChuteAssignmentReceived;

    /// <inheritdoc/>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("正在连接 RuleEngine MQTT Broker: {Broker}:{Port}", _options.Broker, _options.Port);

            // 创建 MQTT 客户端
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            // 构建连接选项
            var clientId = _options.ClientId ?? $"NarrowBeltSorter_{Guid.NewGuid():N}";
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(_options.Broker, _options.Port)
                .WithClientId(clientId)
                .WithCleanSession();

            // 添加认证信息（如果提供）
            if (!string.IsNullOrEmpty(_options.User))
            {
                optionsBuilder.WithCredentials(_options.User, _options.Password);
            }

            // 添加 TLS（如果启用）
            if (_options.UseTls)
            {
                optionsBuilder.WithTlsOptions(o => o.UseTls());
            }

            var mqttOptions = optionsBuilder.Build();

            // 连接到 Broker
            var result = await _mqttClient.ConnectAsync(mqttOptions, cancellationToken);

            if (result.ResultCode == MqttClientConnectResultCode.Success)
            {
                _logger.LogInformation("RuleEngine MQTT 连接成功");
                
                // 订阅分拣结果主题
                await SubscribeToSortingResultsAsync(cancellationToken);
                
                return true;
            }
            else
            {
                _logger.LogError("RuleEngine MQTT 连接失败: {ResultCode}", result.ResultCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接 RuleEngine MQTT Broker 时发生异常");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync()
    {
        if (_mqttClient != null && _mqttClient.IsConnected)
        {
            try
            {
                await _mqttClient.DisconnectAsync();
                _logger.LogInformation("已断开 RuleEngine MQTT 连接");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "断开 RuleEngine MQTT 连接时发生异常");
            }
        }
    }

    /// <summary>
    /// 订阅分拣结果主题
    /// </summary>
    private async Task SubscribeToSortingResultsAsync(CancellationToken cancellationToken = default)
    {
        if (_mqttClient == null)
        {
            return;
        }

        try
        {
            var resultTopic = $"{_options.BaseTopic}/sorting-result-response";
            var chuteAssignmentTopic = $"{_options.BaseTopic}/chute-assignment";
            
            // 注册消息处理器
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
            
            // 订阅分拣结果主题
            await _mqttClient.SubscribeAsync(resultTopic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, cancellationToken);
            _logger.LogInformation("已订阅分拣结果主题: {Topic}", resultTopic);
            
            // 订阅格口分配主题
            await _mqttClient.SubscribeAsync(chuteAssignmentTopic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, cancellationToken);
            _logger.LogInformation("已订阅格口分配主题: {Topic}", chuteAssignmentTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订阅主题失败");
        }
    }

    /// <summary>
    /// MQTT 消息接收处理
    /// </summary>
    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            
            _logger.LogDebug("收到 MQTT 消息: Topic={Topic}", topic);
            
            // 检查是否为分拣结果消息
            if (topic.EndsWith("/sorting-result-response"))
            {
                var sortingResult = JsonSerializer.Deserialize<SortingResultMessage>(payload);
                if (sortingResult != null)
                {
                    _logger.LogInformation(
                        "收到分拣结果: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}, Success={Success}",
                        sortingResult.ParcelId, sortingResult.ChuteNumber, sortingResult.Success);
                    
                    // 触发事件
                    SortingResultReceived?.Invoke(this, sortingResult);
                }
            }
            // 检查是否为格口分配消息
            else if (topic.EndsWith("/chute-assignment"))
            {
                var chuteAssignment = JsonSerializer.Deserialize<ChuteAssignmentNotificationEventArgs>(payload);
                if (chuteAssignment != null)
                {
                    _logger.LogInformation(
                        "收到格口分配: ParcelId={ParcelId}, ChuteId={ChuteId}",
                        chuteAssignment.ParcelId, chuteAssignment.ChuteId);
                    
                    // 触发事件
                    ChuteAssignmentReceived?.Invoke(this, chuteAssignment);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 MQTT 消息时发生异常");
        }
        
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<bool> SendParcelCreatedAsync(ParcelCreatedMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("MQTT 客户端未连接，无法发送包裹创建消息");
            return false;
        }

        try
        {
            var topic = $"{_options.BaseTopic}/parcel-created";
            var payload = JsonSerializer.Serialize(message);
            
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient!.PublishAsync(mqttMessage, cancellationToken);
            
            _logger.LogDebug("已发送包裹创建消息到主题 {Topic}: ParcelId={ParcelId}", topic, message.ParcelId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送包裹创建消息失败: ParcelId={ParcelId}", message.ParcelId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SendDwsDataAsync(DwsDataMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("MQTT 客户端未连接，无法发送 DWS 数据消息");
            return false;
        }

        try
        {
            var topic = $"{_options.BaseTopic}/dws-data";
            var payload = JsonSerializer.Serialize(message);
            
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient!.PublishAsync(mqttMessage, cancellationToken);
            
            _logger.LogDebug("已发送 DWS 数据消息到主题 {Topic}: ParcelId={ParcelId}", topic, message.ParcelId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送 DWS 数据消息失败: ParcelId={ParcelId}", message.ParcelId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SendSortingResultAsync(SortingResultMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("MQTT 客户端未连接，无法发送分拣结果消息");
            return false;
        }

        try
        {
            var topic = $"{_options.BaseTopic}/sorting-result";
            var payload = JsonSerializer.Serialize(message);
            
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient!.PublishAsync(mqttMessage, cancellationToken);
            
            _logger.LogDebug("已发送分拣结果消息到主题 {Topic}: ParcelId={ParcelId}", topic, message.ParcelId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送分拣结果消息失败: ParcelId={ParcelId}", message.ParcelId);
            return false;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _mqttClient?.Dispose();
        _disposed = true;
    }
}
