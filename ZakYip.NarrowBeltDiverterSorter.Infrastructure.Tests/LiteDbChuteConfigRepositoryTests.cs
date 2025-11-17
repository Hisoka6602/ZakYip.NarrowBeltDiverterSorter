using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.LiteDb;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Tests;

/// <summary>
/// LiteDbChuteConfigRepository 单元测试
/// </summary>
public class LiteDbChuteConfigRepositoryTests : IDisposable
{
    private readonly LiteDbSorterConfigurationStore _configStore;
    private readonly LiteDbChuteConfigRepository _repository;
    private readonly string _testDbFile;

    public LiteDbChuteConfigRepositoryTests()
    {
        // Use a unique database file for each test instance
        _testDbFile = Path.Combine(Path.GetTempPath(), $"test-chute-{Guid.NewGuid()}.db");
        
        var configLogger = NullLogger<LiteDbSorterConfigurationStore>.Instance;
        _configStore = new LiteDbSorterConfigurationStore(configLogger, _testDbFile);

        var repoLogger = NullLogger<LiteDbChuteConfigRepository>.Instance;
        _repository = new LiteDbChuteConfigRepository(_configStore, repoLogger);
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Default_When_Not_Exists()
    {
        // Act
        var configSet = await _repository.LoadAsync();

        // Assert
        Assert.NotNull(configSet);
        Assert.NotNull(configSet.Configs);
        Assert.Empty(configSet.Configs);
    }

    [Fact]
    public async Task LoadAsync_Should_Save_Default_When_Not_Exists()
    {
        // Act
        await _repository.LoadAsync();

        // Assert
        var exists = await _configStore.ExistsAsync("ChuteConfigs");
        Assert.True(exists);
    }

    [Fact]
    public async Task SaveAsync_And_LoadAsync_Should_RoundTrip()
    {
        // Arrange
        var configSet = new ChuteConfigSet
        {
            Configs = new List<ChuteConfig>
            {
                new ChuteConfig
                {
                    ChuteId = new ChuteId(1),
                    IsEnabled = true,
                    IsForceEject = false,
                    CartOffsetFromOrigin = 10,
                    MaxOpenDuration = TimeSpan.FromSeconds(5)
                },
                new ChuteConfig
                {
                    ChuteId = new ChuteId(2),
                    IsEnabled = true,
                    IsForceEject = true,
                    CartOffsetFromOrigin = 20,
                    MaxOpenDuration = TimeSpan.FromSeconds(10)
                }
            }
        };

        // Act
        await _repository.SaveAsync(configSet);
        var loaded = await _repository.LoadAsync();

        // Assert
        Assert.NotNull(loaded);
        Assert.NotNull(loaded.Configs);
        Assert.Equal(2, loaded.Configs.Count);
        
        var config1 = loaded.Configs[0];
        Assert.Equal(1, config1.ChuteId.Value);
        Assert.True(config1.IsEnabled);
        Assert.False(config1.IsForceEject);
        Assert.Equal(10, config1.CartOffsetFromOrigin);
        Assert.Equal(TimeSpan.FromSeconds(5), config1.MaxOpenDuration);

        var config2 = loaded.Configs[1];
        Assert.Equal(2, config2.ChuteId.Value);
        Assert.True(config2.IsEnabled);
        Assert.True(config2.IsForceEject);
        Assert.Equal(20, config2.CartOffsetFromOrigin);
        Assert.Equal(TimeSpan.FromSeconds(10), config2.MaxOpenDuration);
    }

    [Fact]
    public async Task SaveAsync_Should_Throw_ArgumentNullException_For_Null_ConfigSet()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.SaveAsync(null!));
    }

    public void Dispose()
    {
        _configStore?.Dispose();

        // 清理测试数据库文件
        try
        {
            if (File.Exists(_testDbFile))
            {
                File.Delete(_testDbFile);
            }
        }
        catch
        {
            // 忽略清理错误
        }
    }
}
