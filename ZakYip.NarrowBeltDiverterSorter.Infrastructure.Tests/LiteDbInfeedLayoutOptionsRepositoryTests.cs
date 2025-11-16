using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Tests;

/// <summary>
/// LiteDbInfeedLayoutOptionsRepository 单元测试
/// </summary>
public class LiteDbInfeedLayoutOptionsRepositoryTests : IDisposable
{
    private readonly LiteDbConfigStore _configStore;
    private readonly LiteDbInfeedLayoutOptionsRepository _repository;
    private readonly string _testDbFile;

    public LiteDbInfeedLayoutOptionsRepositoryTests()
    {
        // Use a unique database file for each test instance
        _testDbFile = Path.Combine(Path.GetTempPath(), $"test-infeed-{Guid.NewGuid()}.db");
        
        var configLogger = NullLogger<LiteDbConfigStore>.Instance;
        _configStore = new LiteDbConfigStore(configLogger, _testDbFile);

        var repoLogger = NullLogger<LiteDbInfeedLayoutOptionsRepository>.Instance;
        _repository = new LiteDbInfeedLayoutOptionsRepository(_configStore, repoLogger);
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Default_When_Not_Exists()
    {
        // Act
        var options = await _repository.LoadAsync();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(5000m, options.InfeedToMainLineDistanceMm);
        Assert.Equal(100, options.TimeToleranceMs);
        Assert.Equal(0, options.CartOffsetCalibration);
    }

    [Fact]
    public async Task LoadAsync_Should_Save_Default_When_Not_Exists()
    {
        // Act
        await _repository.LoadAsync();

        // Assert
        var exists = await _configStore.ExistsAsync("InfeedLayoutOptions");
        Assert.True(exists);
    }

    [Fact]
    public async Task SaveAsync_And_LoadAsync_Should_RoundTrip()
    {
        // Arrange
        var options = new InfeedLayoutOptions
        {
            InfeedToMainLineDistanceMm = 7000m,
            TimeToleranceMs = 200,
            CartOffsetCalibration = 2
        };

        // Act
        await _repository.SaveAsync(options);
        var loaded = await _repository.LoadAsync();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(options.InfeedToMainLineDistanceMm, loaded.InfeedToMainLineDistanceMm);
        Assert.Equal(options.TimeToleranceMs, loaded.TimeToleranceMs);
        Assert.Equal(options.CartOffsetCalibration, loaded.CartOffsetCalibration);
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
