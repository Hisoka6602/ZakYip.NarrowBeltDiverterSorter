using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Tests;

/// <summary>
/// LiteDbConfigStore 单元测试
/// </summary>
public class LiteDbConfigStoreTests : IDisposable
{
    private readonly LiteDbConfigStore _configStore;
    private readonly string _testDbPath;

    public LiteDbConfigStoreTests()
    {
        // 为每个测试使用一个唯一的数据库文件
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_narrowbelt.config.{Guid.NewGuid()}.db");
        
        var logger = NullLogger<LiteDbConfigStore>.Instance;
        
        // 删除测试数据库（如果存在）
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
        
        _configStore = new LiteDbConfigStore(logger);
    }

    [Fact]
    public async Task SaveAsync_And_LoadAsync_Should_RoundTrip_Configuration()
    {
        // Arrange
        const string key = "test-config-1";
        var testConfig = new TestConfiguration
        {
            Name = "测试配置",
            Value = 42,
            IsEnabled = true
        };

        // Act
        await _configStore.SaveAsync(key, testConfig);
        var loaded = await _configStore.LoadAsync<TestConfiguration>(key);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(testConfig.Name, loaded.Name);
        Assert.Equal(testConfig.Value, loaded.Value);
        Assert.Equal(testConfig.IsEnabled, loaded.IsEnabled);
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Null_For_NonExistent_Key()
    {
        // Arrange
        const string key = "non-existent-key";

        // Act
        var result = await _configStore.LoadAsync<TestConfiguration>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_False_For_NonExistent_Key()
    {
        // Arrange
        const string key = "non-existent-key";

        // Act
        var exists = await _configStore.ExistsAsync(key);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_True_For_Existing_Key()
    {
        // Arrange
        const string key = "test-config-2";
        var testConfig = new TestConfiguration { Name = "测试", Value = 100 };

        // Act
        await _configStore.SaveAsync(key, testConfig);
        var exists = await _configStore.ExistsAsync(key);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task SaveAsync_Should_Update_Existing_Configuration()
    {
        // Arrange
        const string key = "test-config-3";
        var originalConfig = new TestConfiguration { Name = "原始", Value = 1 };
        var updatedConfig = new TestConfiguration { Name = "更新", Value = 2 };

        // Act
        await _configStore.SaveAsync(key, originalConfig);
        await _configStore.SaveAsync(key, updatedConfig);
        var loaded = await _configStore.LoadAsync<TestConfiguration>(key);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(updatedConfig.Name, loaded.Name);
        Assert.Equal(updatedConfig.Value, loaded.Value);
    }

    [Fact]
    public async Task SaveAsync_Should_Throw_ArgumentException_For_Empty_Key()
    {
        // Arrange
        var testConfig = new TestConfiguration { Name = "测试" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _configStore.SaveAsync(string.Empty, testConfig));
    }

    [Fact]
    public async Task LoadAsync_Should_Throw_ArgumentException_For_Empty_Key()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _configStore.LoadAsync<TestConfiguration>(string.Empty));
    }

    public void Dispose()
    {
        _configStore?.Dispose();
        
        // 清理测试数据库文件
        try
        {
            if (File.Exists("narrowbelt.config.db"))
            {
                File.Delete("narrowbelt.config.db");
            }
        }
        catch
        {
            // 忽略清理错误
        }
    }

    /// <summary>
    /// 测试配置类
    /// </summary>
    private class TestConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public bool IsEnabled { get; set; }
    }
}
