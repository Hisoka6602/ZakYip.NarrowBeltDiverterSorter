using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Tests;

/// <summary>
/// LiteDbMainLineOptionsRepository 单元测试
/// </summary>
public class LiteDbMainLineOptionsRepositoryTests : IDisposable
{
    private readonly LiteDbConfigStore _configStore;
    private readonly LiteDbMainLineOptionsRepository _repository;
    private readonly string _testDbFile;

    public LiteDbMainLineOptionsRepositoryTests()
    {
        // Use a unique database file for each test instance
        _testDbFile = Path.Combine(Path.GetTempPath(), $"test-mainline-{Guid.NewGuid()}.db");
        
        var configLogger = NullLogger<LiteDbConfigStore>.Instance;
        _configStore = new LiteDbConfigStore(configLogger, _testDbFile);

        var repoLogger = NullLogger<LiteDbMainLineOptionsRepository>.Instance;
        _repository = new LiteDbMainLineOptionsRepository(_configStore, repoLogger);
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Default_When_Not_Exists()
    {
        // Act
        var options = await _repository.LoadAsync();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(1000m, options.TargetSpeedMmps);
        Assert.Equal(TimeSpan.FromMilliseconds(100), options.LoopPeriod);
    }

    [Fact]
    public async Task LoadAsync_Should_Save_Default_When_Not_Exists()
    {
        // Act
        await _repository.LoadAsync();

        // Assert
        var exists = await _configStore.ExistsAsync("MainLineControlOptions");
        Assert.True(exists);
    }

    [Fact]
    public async Task SaveAsync_And_LoadAsync_Should_RoundTrip()
    {
        // Arrange
        var options = new MainLineControlOptions
        {
            TargetSpeedMmps = 1500m,
            LoopPeriod = TimeSpan.FromMilliseconds(200),
            ProportionalGain = 2.0m,
            IntegralGain = 0.2m,
            DerivativeGain = 0.02m,
            StableDeadbandMmps = 20m,
            StableHold = TimeSpan.FromSeconds(3),
            MinOutputMmps = 100m,
            MaxOutputMmps = 6000m,
            IntegralLimit = 2000m
        };

        // Act
        await _repository.SaveAsync(options);
        var loaded = await _repository.LoadAsync();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(options.TargetSpeedMmps, loaded.TargetSpeedMmps);
        Assert.Equal(options.LoopPeriod, loaded.LoopPeriod);
        Assert.Equal(options.ProportionalGain, loaded.ProportionalGain);
        Assert.Equal(options.IntegralGain, loaded.IntegralGain);
        Assert.Equal(options.DerivativeGain, loaded.DerivativeGain);
        Assert.Equal(options.StableDeadbandMmps, loaded.StableDeadbandMmps);
        Assert.Equal(options.StableHold, loaded.StableHold);
        Assert.Equal(options.MinOutputMmps, loaded.MinOutputMmps);
        Assert.Equal(options.MaxOutputMmps, loaded.MaxOutputMmps);
        Assert.Equal(options.IntegralLimit, loaded.IntegralLimit);
    }

    [Fact]
    public async Task SaveAsync_Should_Throw_ArgumentNullException_For_Null_Options()
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
