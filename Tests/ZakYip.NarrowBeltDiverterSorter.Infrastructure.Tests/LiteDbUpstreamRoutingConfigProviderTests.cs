using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.LiteDb;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Tests;

/// <summary>
/// 上游路由配置提供器单元测试
/// 测试配置的获取、更新和事件触发
/// </summary>
public class LiteDbUpstreamRoutingConfigProviderTests : IDisposable
{
    private readonly LiteDbSorterConfigurationStore _configStore;
    private readonly LiteDbUpstreamRoutingConfigProvider _configProvider;
    private readonly string _testDbPath;

    public LiteDbUpstreamRoutingConfigProviderTests()
    {
        // 为每个测试使用一个唯一的数据库文件
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_upstream_routing.{Guid.NewGuid()}.db");
        
        var storeLogger = NullLogger<LiteDbSorterConfigurationStore>.Instance;
        var providerLogger = NullLogger<LiteDbUpstreamRoutingConfigProvider>.Instance;
        
        // 删除测试数据库（如果存在）
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
        
        _configStore = new LiteDbSorterConfigurationStore(storeLogger, _testDbPath);
        _configProvider = new LiteDbUpstreamRoutingConfigProvider(_configStore, providerLogger);
    }

    [Fact]
    public void GetCurrentOptions_Should_Return_Default_Options_Initially()
    {
        // Act
        var options = _configProvider.GetCurrentOptions();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromSeconds(30), options.UpstreamResultTtl);
        Assert.Equal(9999, options.ErrorChuteId);
    }

    [Fact]
    public async Task UpdateOptionsAsync_Should_Update_Configuration_Successfully()
    {
        // Arrange
        var newOptions = new UpstreamRoutingOptions
        {
            UpstreamResultTtl = TimeSpan.FromSeconds(45),
            ErrorChuteId = 8888
        };

        // Act
        await _configProvider.UpdateOptionsAsync(newOptions);
        var currentOptions = _configProvider.GetCurrentOptions();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(45), currentOptions.UpstreamResultTtl);
        Assert.Equal(8888, currentOptions.ErrorChuteId);
    }

    [Fact]
    public async Task UpdateOptionsAsync_Should_Persist_To_Database()
    {
        // Arrange
        var newOptions = new UpstreamRoutingOptions
        {
            UpstreamResultTtl = TimeSpan.FromSeconds(60),
            ErrorChuteId = 7777
        };

        // Act - 更新配置
        await _configProvider.UpdateOptionsAsync(newOptions);

        // 创建新的提供器实例（模拟应用重启）
        var providerLogger = NullLogger<LiteDbUpstreamRoutingConfigProvider>.Instance;
        var newProvider = new LiteDbUpstreamRoutingConfigProvider(_configStore, providerLogger);
        var loadedOptions = newProvider.GetCurrentOptions();

        // Assert - 验证配置持久化
        Assert.Equal(TimeSpan.FromSeconds(60), loadedOptions.UpstreamResultTtl);
        Assert.Equal(7777, loadedOptions.ErrorChuteId);
    }

    [Fact]
    public async Task UpdateOptionsAsync_Should_Trigger_ConfigChanged_Event()
    {
        // Arrange
        UpstreamRoutingConfigChangedEventArgs? eventArgs = null;
        _configProvider.ConfigChanged += (sender, args) => eventArgs = args;

        var newOptions = new UpstreamRoutingOptions
        {
            UpstreamResultTtl = TimeSpan.FromSeconds(90),
            ErrorChuteId = 6666
        };

        // Act
        await _configProvider.UpdateOptionsAsync(newOptions);

        // Assert - 验证事件触发
        Assert.NotNull(eventArgs);
        Assert.Equal(TimeSpan.FromSeconds(90), eventArgs.NewOptions.UpstreamResultTtl);
        Assert.Equal(6666, eventArgs.NewOptions.ErrorChuteId);
        Assert.True(eventArgs.ChangedAt <= DateTimeOffset.Now);
    }

    [Fact]
    public async Task UpdateOptionsAsync_Should_Handle_Multiple_Updates()
    {
        // Arrange
        var updates = new[]
        {
            new UpstreamRoutingOptions { UpstreamResultTtl = TimeSpan.FromSeconds(10), ErrorChuteId = 1000 },
            new UpstreamRoutingOptions { UpstreamResultTtl = TimeSpan.FromSeconds(20), ErrorChuteId = 2000 },
            new UpstreamRoutingOptions { UpstreamResultTtl = TimeSpan.FromSeconds(30), ErrorChuteId = 3000 }
        };

        // Act
        foreach (var update in updates)
        {
            await _configProvider.UpdateOptionsAsync(update);
        }

        var finalOptions = _configProvider.GetCurrentOptions();

        // Assert - 验证最后一次更新生效
        Assert.Equal(TimeSpan.FromSeconds(30), finalOptions.UpstreamResultTtl);
        Assert.Equal(3000, finalOptions.ErrorChuteId);
    }

    [Fact]
    public async Task UpdateOptionsAsync_Should_Throw_ArgumentNullException_For_Null_Options()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _configProvider.UpdateOptionsAsync(null!));
    }

    [Fact]
    public void GetCurrentOptions_Should_Return_Copy_Not_Reference()
    {
        // Arrange & Act
        var options1 = _configProvider.GetCurrentOptions();
        var options2 = _configProvider.GetCurrentOptions();

        // Assert - 验证返回的是副本
        Assert.NotSame(options1, options2);
    }

    public void Dispose()
    {
        _configStore?.Dispose();
        
        // 清理测试数据库
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
