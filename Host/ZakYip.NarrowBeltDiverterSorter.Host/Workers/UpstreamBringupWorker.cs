using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Communication;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 上游 Bring-up 诊断工作器
/// 周期性发送测试包裹到上游规则引擎，用于验证通讯链路
/// </summary>
public class UpstreamBringupWorker : BackgroundService
{
    private readonly ISortingRuleEnginePort _ruleEnginePort;
    private readonly ILogger<UpstreamBringupWorker> _logger;
    private readonly ZakYip.NarrowBeltDiverterSorter.Host.Configuration.IHostConfigurationProvider _configProvider;
    
    private int _successCount = 0;
    private int _failureCount = 0;
    private int _testParcelIdCounter = 1;

    public UpstreamBringupWorker(
        ISortingRuleEnginePort ruleEnginePort,
        ILogger<UpstreamBringupWorker> logger,
        ZakYip.NarrowBeltDiverterSorter.Host.Configuration.IHostConfigurationProvider configProvider)
    {
        _ruleEnginePort = ruleEnginePort ?? throw new ArgumentNullException(nameof(ruleEnginePort));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=================================================");
        _logger.LogInformation("上游 Bring-up 模式已启动");
        _logger.LogInformation("=================================================");

        // 获取上游配置
        var upstreamOptions = await _configProvider.GetUpstreamOptionsAsync();
        
        _logger.LogInformation("当前上游模式: {Mode}", upstreamOptions.Mode);
        
        if (upstreamOptions.Mode == UpstreamMode.Mqtt && upstreamOptions.Mqtt != null)
        {
            _logger.LogInformation("MQTT Broker: {Broker}:{Port}", upstreamOptions.Mqtt.Broker, upstreamOptions.Mqtt.Port);
            _logger.LogInformation("MQTT Base Topic: {BaseTopic}", upstreamOptions.Mqtt.BaseTopic);
        }
        else if (upstreamOptions.Mode == UpstreamMode.Tcp && upstreamOptions.Tcp != null)
        {
            _logger.LogInformation("TCP 目标: {Host}:{Port}", upstreamOptions.Tcp.Host, upstreamOptions.Tcp.Port);
        }
        else if (upstreamOptions.Mode == UpstreamMode.Disabled)
        {
            _logger.LogWarning("上游已禁用，Bring-up 模式将只验证本地配置，不会发送实际消息");
        }

        _logger.LogInformation("测试发送频率: 每 5 秒一次");
        _logger.LogInformation("测试包裹 ID 范围: BRINGUP-00001 开始递增");
        _logger.LogInformation("按 Ctrl+C 停止...");
        _logger.LogInformation("=================================================");

        // 周期性发送测试包裹
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, stoppingToken); // 每5秒发送一次

                var testParcelId = $"BRINGUP-{_testParcelIdCounter:D5}";
                var testBarcode = $"TEST-{_testParcelIdCounter:D5}";
                
                _logger.LogInformation("发送测试包裹 #{Counter}: ParcelId={ParcelId}, Barcode={Barcode}", 
                    _testParcelIdCounter, testParcelId, testBarcode);

                var requestArgs = new SortingRequestEventArgs
                {
                    ParcelId = _testParcelIdCounter,
                    CartNumber = 1,
                    Barcode = testBarcode,
                    Weight = 1.5m,
                    Length = 300m,
                    Width = 200m,
                    Height = 150m,
                    RequestTime = DateTimeOffset.Now
                };

                try
                {
                    await _ruleEnginePort.RequestSortingAsync(requestArgs, stoppingToken);
                    _successCount++;
                    _logger.LogInformation("✓ 测试包裹发送成功 (总成功: {SuccessCount}, 总失败: {FailureCount})", 
                        _successCount, _failureCount);
                }
                catch (Exception ex)
                {
                    _failureCount++;
                    _logger.LogWarning(ex, "✗ 测试包裹发送失败 (总成功: {SuccessCount}, 总失败: {FailureCount})", 
                        _successCount, _failureCount);
                }

                _testParcelIdCounter++;
            }
            catch (OperationCanceledException)
            {
                // 正常停止
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上游 Bring-up 工作器发生异常");
                await Task.Delay(5000, stoppingToken); // 错误后等待5秒再继续
            }
        }

        _logger.LogInformation("=================================================");
        _logger.LogInformation("上游 Bring-up 模式已停止");
        _logger.LogInformation("统计: 成功 {SuccessCount} 次, 失败 {FailureCount} 次", _successCount, _failureCount);
        _logger.LogInformation("=================================================");
    }
}
