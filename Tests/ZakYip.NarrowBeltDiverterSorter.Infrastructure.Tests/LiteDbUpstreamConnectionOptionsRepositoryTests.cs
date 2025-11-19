using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.LiteDb;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Tests;

/// <summary>
/// LiteDbUpstreamConnectionOptionsRepository 单元测试
/// </summary>
public class LiteDbUpstreamConnectionOptionsRepositoryTests : IDisposable
{
    private readonly LiteDbSorterConfigurationStore _configStore;
    private readonly LiteDbUpstreamConnectionOptionsRepository _repository;
    private readonly string _testDbFile;

    public LiteDbUpstreamConnectionOptionsRepositoryTests()
    {
        // Use a unique database file for each test instance
        _testDbFile = Path.Combine(Path.GetTempPath(), $"test-upstream-{Guid.NewGuid()}.db");
        
        var configLogger = NullLogger<LiteDbSorterConfigurationStore>.Instance;
        _configStore = new LiteDbSorterConfigurationStore(configLogger, _testDbFile);

        var repoLogger = NullLogger<LiteDbUpstreamConnectionOptionsRepository>.Instance;
        _repository = new LiteDbUpstreamConnectionOptionsRepository(_configStore, repoLogger);
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Default_When_Not_Exists()
    {
        // Act
        var options = await _repository.LoadAsync();

        // Assert
        Assert.NotNull(options);
        Assert.Equal("http://localhost:5000", options.BaseUrl);
        Assert.Equal(30, options.RequestTimeoutSeconds);
        Assert.Null(options.AuthToken);
    }

    [Fact]
    public async Task LoadAsync_Should_Save_Default_When_Not_Exists()
    {
        // Act
        await _repository.LoadAsync();

        // Assert
        var exists = await _configStore.ExistsAsync("UpstreamConnectionOptions");
        Assert.True(exists);
    }

    [Fact]
    public async Task SaveAsync_And_LoadAsync_Should_RoundTrip()
    {
        // Arrange
        var options = new UpstreamConnectionOptions
        {
            BaseUrl = "http://example.com:8080",
            RequestTimeoutSeconds = 60,
            AuthToken = "test-token-12345"
        };

        // Act
        await _repository.SaveAsync(options);
        var loaded = await _repository.LoadAsync();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(options.BaseUrl, loaded.BaseUrl);
        Assert.Equal(options.RequestTimeoutSeconds, loaded.RequestTimeoutSeconds);
        Assert.Equal(options.AuthToken, loaded.AuthToken);
    }

    [Fact]
    public async Task SaveAsync_And_LoadAsync_Should_Handle_Null_AuthToken()
    {
        // Arrange
        var options = new UpstreamConnectionOptions
        {
            BaseUrl = "http://example.com:8080",
            RequestTimeoutSeconds = 60,
            AuthToken = null
        };

        // Act
        await _repository.SaveAsync(options);
        var loaded = await _repository.LoadAsync();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(options.BaseUrl, loaded.BaseUrl);
        Assert.Equal(options.RequestTimeoutSeconds, loaded.RequestTimeoutSeconds);
        Assert.Null(loaded.AuthToken);
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
