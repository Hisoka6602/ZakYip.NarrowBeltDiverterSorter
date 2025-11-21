using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.LiteDb;

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests;

/// <summary>
/// 验证配置热更新对包裹处理的影响
/// 测试更新 TTL 和异常格口后，新创建的包裹使用新配置
/// </summary>
public class ConfigHotReloadIntegrationTests : IDisposable
{
    private readonly LiteDbSorterConfigurationStore _configStore;
    private readonly LiteDbUpstreamRoutingConfigProvider _configProvider;
    private readonly UpstreamRequestTracker _requestTracker;
    private readonly string _testDbPath;

    public ConfigHotReloadIntegrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_hot_reload.{Guid.NewGuid()}.db");
        
        var storeLogger = NullLogger<LiteDbSorterConfigurationStore>.Instance;
        var providerLogger = NullLogger<LiteDbUpstreamRoutingConfigProvider>.Instance;
        
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
        
        _configStore = new LiteDbSorterConfigurationStore(storeLogger, _testDbPath);
        _configProvider = new LiteDbUpstreamRoutingConfigProvider(_configStore, providerLogger);
        _requestTracker = new UpstreamRequestTracker();
    }

    [Fact]
    public async Task NewParcels_Should_Use_Updated_TTL_After_Config_Change()
    {
        // Arrange - 获取初始配置
        var initialOptions = _configProvider.GetCurrentOptions();
        Assert.Equal(TimeSpan.FromSeconds(30), initialOptions.UpstreamResultTtl);

        // 模拟创建第一个包裹（使用初始 TTL）
        var parcelId1 = new ParcelId(1001);
        var requestedAt1 = DateTimeOffset.Now;
        var deadline1 = requestedAt1.Add(initialOptions.UpstreamResultTtl);
        _requestTracker.RecordRequest(parcelId1, requestedAt1, deadline1);

        var record1 = _requestTracker.GetRecord(parcelId1);
        Assert.NotNull(record1);
        Assert.Equal(deadline1, record1.Deadline);
        Assert.Equal(TimeSpan.FromSeconds(30), record1.Deadline - requestedAt1);

        // Act - 更新配置：将 TTL 改为 60 秒
        var newOptions = new UpstreamRoutingOptions
        {
            UpstreamResultTtl = TimeSpan.FromSeconds(60),
            ErrorChuteId = 9999
        };
        await _configProvider.UpdateOptionsAsync(newOptions);

        var updatedOptions = _configProvider.GetCurrentOptions();
        Assert.Equal(TimeSpan.FromSeconds(60), updatedOptions.UpstreamResultTtl);

        // 模拟创建第二个包裹（应该使用新的 TTL）
        var parcelId2 = new ParcelId(1002);
        var requestedAt2 = DateTimeOffset.Now;
        var deadline2 = requestedAt2.Add(updatedOptions.UpstreamResultTtl);
        _requestTracker.RecordRequest(parcelId2, requestedAt2, deadline2);

        var record2 = _requestTracker.GetRecord(parcelId2);
        Assert.NotNull(record2);
        Assert.Equal(deadline2, record2.Deadline);
        
        // Assert - 验证新包裹使用新 TTL (60秒)，而不是旧 TTL (30秒)
        var actualTtl = record2.Deadline - requestedAt2;
        Assert.Equal(TimeSpan.FromSeconds(60), actualTtl);
        Assert.NotEqual(TimeSpan.FromSeconds(30), actualTtl);

        // 验证两个包裹的 TTL 不同
        var ttl1 = record1.Deadline - requestedAt1;
        var ttl2 = record2.Deadline - requestedAt2;
        Assert.NotEqual(ttl1, ttl2);
    }

    [Fact]
    public async Task NewTimeouts_Should_Use_Updated_ErrorChuteId_After_Config_Change()
    {
        // Arrange - 获取初始配置
        var initialOptions = _configProvider.GetCurrentOptions();
        Assert.Equal(9999, initialOptions.ErrorChuteId);

        // Act - 更新配置：将异常格口改为 8888
        var newOptions = new UpstreamRoutingOptions
        {
            UpstreamResultTtl = TimeSpan.FromSeconds(30),
            ErrorChuteId = 8888
        };
        await _configProvider.UpdateOptionsAsync(newOptions);

        var updatedOptions = _configProvider.GetCurrentOptions();

        // Assert - 验证新的异常格口 ID 已生效
        Assert.Equal(8888, updatedOptions.ErrorChuteId);
        Assert.NotEqual(initialOptions.ErrorChuteId, updatedOptions.ErrorChuteId);
    }

    [Fact]
    public async Task Config_Change_Should_Trigger_Event_For_Running_Components()
    {
        // Arrange
        bool eventTriggered = false;
        UpstreamRoutingOptions? receivedOptions = null;

        _configProvider.ConfigChanged += (sender, args) =>
        {
            eventTriggered = true;
            receivedOptions = args.NewOptions;
        };

        // Act - 更新配置
        var newOptions = new UpstreamRoutingOptions
        {
            UpstreamResultTtl = TimeSpan.FromSeconds(45),
            ErrorChuteId = 7777
        };
        await _configProvider.UpdateOptionsAsync(newOptions);

        // Assert - 验证事件被触发，运行中的组件可以收到通知
        Assert.True(eventTriggered);
        Assert.NotNull(receivedOptions);
        Assert.Equal(TimeSpan.FromSeconds(45), receivedOptions.UpstreamResultTtl);
        Assert.Equal(7777, receivedOptions.ErrorChuteId);
    }

    [Fact]
    public async Task Short_TTL_Should_Cause_More_Parcels_To_Timeout()
    {
        // Arrange - 设置非常短的 TTL（1秒）
        var shortTtlOptions = new UpstreamRoutingOptions
        {
            UpstreamResultTtl = TimeSpan.FromSeconds(1),
            ErrorChuteId = 9999
        };
        await _configProvider.UpdateOptionsAsync(shortTtlOptions);

        var options = _configProvider.GetCurrentOptions();
        Assert.Equal(TimeSpan.FromSeconds(1), options.UpstreamResultTtl);

        // 创建包裹（使用短 TTL）
        var parcelId = new ParcelId(2001);
        var requestedAt = DateTimeOffset.Now;
        var deadline = requestedAt.Add(options.UpstreamResultTtl);
        _requestTracker.RecordRequest(parcelId, requestedAt, deadline);

        // Act - 等待超过 TTL 时间
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        // 检查超时
        var currentTime = DateTimeOffset.Now;
        var timedOutRequests = _requestTracker.GetTimedOutRequests(currentTime);

        // Assert - 验证包裹确实超时了
        Assert.NotEmpty(timedOutRequests);
        Assert.Contains(timedOutRequests, r => r.ParcelId.Value == 2001);
    }

    [Fact]
    public async Task Long_TTL_Should_Give_More_Time_Before_Timeout()
    {
        // Arrange - 设置较长的 TTL（120秒）
        var longTtlOptions = new UpstreamRoutingOptions
        {
            UpstreamResultTtl = TimeSpan.FromSeconds(120),
            ErrorChuteId = 9999
        };
        await _configProvider.UpdateOptionsAsync(longTtlOptions);

        var options = _configProvider.GetCurrentOptions();
        Assert.Equal(TimeSpan.FromSeconds(120), options.UpstreamResultTtl);

        // 创建包裹（使用长 TTL）
        var parcelId = new ParcelId(3001);
        var requestedAt = DateTimeOffset.Now;
        var deadline = requestedAt.Add(options.UpstreamResultTtl);
        _requestTracker.RecordRequest(parcelId, requestedAt, deadline);

        // Act - 等待 1 秒（远小于 TTL）
        await Task.Delay(TimeSpan.FromSeconds(1));

        // 检查超时
        var currentTime = DateTimeOffset.Now;
        var timedOutRequests = _requestTracker.GetTimedOutRequests(currentTime);

        // Assert - 验证包裹没有超时
        Assert.DoesNotContain(timedOutRequests, r => r.ParcelId.Value == 3001);

        // 验证包裹仍在等待中
        var record = _requestTracker.GetRecord(parcelId);
        Assert.NotNull(record);
        Assert.True(currentTime < record.Deadline, "当前时间应该小于截止时间");
    }

    public void Dispose()
    {
        _configStore?.Dispose();
        
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }
}
