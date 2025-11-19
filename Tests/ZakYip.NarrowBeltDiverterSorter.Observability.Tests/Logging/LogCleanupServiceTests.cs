using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Observability.Logging;

namespace ZakYip.NarrowBeltDiverterSorter.Observability.Tests.Logging;

/// <summary>
/// 日志清理服务测试
/// </summary>
public class LogCleanupServiceTests : IDisposable
{
    private readonly string _testLogDirectory;

    public LogCleanupServiceTests()
    {
        _testLogDirectory = Path.Combine(Path.GetTempPath(), $"LogCleanupTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testLogDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testLogDirectory))
        {
            Directory.Delete(_testLogDirectory, true);
        }
    }

    [Fact]
    public async Task CleanupExpiredLogsAsync_WithExpiredLogs_ShouldDeleteOldFiles()
    {
        // Arrange
        var currentTime = DateTime.Now;
        var options = Options.Create(new LogRetentionOptions
        {
            RetentionDays = 3,
            LogDirectory = _testLogDirectory
        });

        var service = new LogCleanupService(
            options,
            NullLogger<LogCleanupService>.Instance,
            () => currentTime);

        // 创建多个日志文件，不同的时间戳
        CreateLogFile("log_day1.log", currentTime.AddDays(-5)); // 应该被删除
        CreateLogFile("log_day2.log", currentTime.AddDays(-4)); // 应该被删除
        CreateLogFile("log_day3.log", currentTime.AddDays(-2)); // 应该保留
        CreateLogFile("log_day4.log", currentTime.AddDays(-1)); // 应该保留
        CreateLogFile("log_today.log", currentTime); // 应该保留

        // Act
        var deletedCount = await service.CleanupExpiredLogsAsync();

        // Assert
        Assert.Equal(2, deletedCount);
        Assert.False(File.Exists(Path.Combine(_testLogDirectory, "log_day1.log")));
        Assert.False(File.Exists(Path.Combine(_testLogDirectory, "log_day2.log")));
        Assert.True(File.Exists(Path.Combine(_testLogDirectory, "log_day3.log")));
        Assert.True(File.Exists(Path.Combine(_testLogDirectory, "log_day4.log")));
        Assert.True(File.Exists(Path.Combine(_testLogDirectory, "log_today.log")));
    }

    [Fact]
    public async Task CleanupExpiredLogsAsync_WithNoExpiredLogs_ShouldNotDeleteFiles()
    {
        // Arrange
        var currentTime = DateTime.Now;
        var options = Options.Create(new LogRetentionOptions
        {
            RetentionDays = 3,
            LogDirectory = _testLogDirectory
        });

        var service = new LogCleanupService(
            options,
            NullLogger<LogCleanupService>.Instance,
            () => currentTime);

        // 创建都在保留期内的日志文件
        CreateLogFile("log1.log", currentTime.AddDays(-2));
        CreateLogFile("log2.log", currentTime.AddDays(-1));
        CreateLogFile("log3.log", currentTime);

        // Act
        var deletedCount = await service.CleanupExpiredLogsAsync();

        // Assert
        Assert.Equal(0, deletedCount);
        Assert.True(File.Exists(Path.Combine(_testLogDirectory, "log1.log")));
        Assert.True(File.Exists(Path.Combine(_testLogDirectory, "log2.log")));
        Assert.True(File.Exists(Path.Combine(_testLogDirectory, "log3.log")));
    }

    [Fact]
    public async Task CleanupExpiredLogsAsync_WithEmptyDirectory_ShouldReturnZero()
    {
        // Arrange
        var options = Options.Create(new LogRetentionOptions
        {
            RetentionDays = 3,
            LogDirectory = _testLogDirectory
        });

        var service = new LogCleanupService(
            options,
            NullLogger<LogCleanupService>.Instance);

        // Act
        var deletedCount = await service.CleanupExpiredLogsAsync();

        // Assert
        Assert.Equal(0, deletedCount);
    }

    [Fact]
    public async Task CleanupExpiredLogsAsync_WithNonExistentDirectory_ShouldReturnZero()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid()}");
        var options = Options.Create(new LogRetentionOptions
        {
            RetentionDays = 3,
            LogDirectory = nonExistentPath
        });

        var service = new LogCleanupService(
            options,
            NullLogger<LogCleanupService>.Instance);

        // Act
        var deletedCount = await service.CleanupExpiredLogsAsync();

        // Assert
        Assert.Equal(0, deletedCount);
    }

    [Fact]
    public async Task CleanupExpiredLogsAsync_WithNullDirectory_ShouldReturnZero()
    {
        // Arrange
        var options = Options.Create(new LogRetentionOptions
        {
            RetentionDays = 3,
            LogDirectory = null
        });

        var service = new LogCleanupService(
            options,
            NullLogger<LogCleanupService>.Instance);

        // Act
        var deletedCount = await service.CleanupExpiredLogsAsync();

        // Assert
        Assert.Equal(0, deletedCount);
    }

    [Fact]
    public async Task CleanupExpiredLogsAsync_WithSubdirectories_ShouldDeleteRecursively()
    {
        // Arrange
        var currentTime = DateTime.Now;
        var subDirectory = Path.Combine(_testLogDirectory, "subdirectory");
        Directory.CreateDirectory(subDirectory);

        var options = Options.Create(new LogRetentionOptions
        {
            RetentionDays = 3,
            LogDirectory = _testLogDirectory
        });

        var service = new LogCleanupService(
            options,
            NullLogger<LogCleanupService>.Instance,
            () => currentTime);

        // 在主目录和子目录创建日志文件
        CreateLogFile("root_old.log", currentTime.AddDays(-5));
        CreateLogFile(Path.Combine("subdirectory", "sub_old.log"), currentTime.AddDays(-4));
        CreateLogFile("root_new.log", currentTime.AddDays(-1));
        CreateLogFile(Path.Combine("subdirectory", "sub_new.log"), currentTime);

        // Act
        var deletedCount = await service.CleanupExpiredLogsAsync();

        // Assert
        Assert.Equal(2, deletedCount);
        Assert.False(File.Exists(Path.Combine(_testLogDirectory, "root_old.log")));
        Assert.False(File.Exists(Path.Combine(subDirectory, "sub_old.log")));
        Assert.True(File.Exists(Path.Combine(_testLogDirectory, "root_new.log")));
        Assert.True(File.Exists(Path.Combine(subDirectory, "sub_new.log")));
    }

    [Fact]
    public async Task CleanupExpiredLogsAsync_WithCancellation_ShouldStopGracefully()
    {
        // Arrange
        var currentTime = DateTime.Now;
        var options = Options.Create(new LogRetentionOptions
        {
            RetentionDays = 3,
            LogDirectory = _testLogDirectory
        });

        var service = new LogCleanupService(
            options,
            NullLogger<LogCleanupService>.Instance,
            () => currentTime);

        // 创建多个过期文件
        for (int i = 0; i < 10; i++)
        {
            CreateLogFile($"log_{i}.log", currentTime.AddDays(-5));
        }

        var cts = new CancellationTokenSource();
        cts.Cancel(); // 立即取消

        // Act
        var deletedCount = await service.CleanupExpiredLogsAsync(cts.Token);

        // Assert - 由于立即取消，应该删除很少或没有文件
        Assert.True(deletedCount < 10);
    }

    [Fact]
    public async Task CleanupExpiredLogsAsync_WithCustomRetentionDays_ShouldRespectConfiguration()
    {
        // Arrange
        var currentTime = DateTime.Now;
        var options = Options.Create(new LogRetentionOptions
        {
            RetentionDays = 7, // 7 天保留期
            LogDirectory = _testLogDirectory
        });

        var service = new LogCleanupService(
            options,
            NullLogger<LogCleanupService>.Instance,
            () => currentTime);

        CreateLogFile("log_8days.log", currentTime.AddDays(-8)); // 应该被删除
        CreateLogFile("log_6days.log", currentTime.AddDays(-6)); // 应该保留

        // Act
        var deletedCount = await service.CleanupExpiredLogsAsync();

        // Assert
        Assert.Equal(1, deletedCount);
        Assert.False(File.Exists(Path.Combine(_testLogDirectory, "log_8days.log")));
        Assert.True(File.Exists(Path.Combine(_testLogDirectory, "log_6days.log")));
    }

    private void CreateLogFile(string relativePath, DateTime lastWriteTime)
    {
        var fullPath = Path.Combine(_testLogDirectory, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, "Sample log content");
        File.SetLastWriteTime(fullPath, lastWriteTime);
    }
}
