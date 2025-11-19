using ZakYip.NarrowBeltDiverterSorter.Observability.Logging;

namespace ZakYip.NarrowBeltDiverterSorter.Observability.Tests.Logging;

/// <summary>
/// 日志节流器测试
/// </summary>
public class LogThrottlerTests
{
    [Fact]
    public void ShouldLog_FirstCall_ShouldReturnTrue()
    {
        // Arrange
        var throttler = new LogThrottler();

        // Act
        var result = throttler.ShouldLog("Test message");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldLog_RepeatedCallsWithinInterval_ShouldReturnFalseAfterFirst()
    {
        // Arrange
        var currentTime = DateTime.Now;
        var throttler = new LogThrottler(TimeSpan.FromSeconds(1), () => currentTime);

        // Act & Assert
        Assert.True(throttler.ShouldLog("Test message"));
        Assert.False(throttler.ShouldLog("Test message"));
        Assert.False(throttler.ShouldLog("Test message"));
    }

    [Fact]
    public void ShouldLog_CallsAfterInterval_ShouldReturnTrue()
    {
        // Arrange
        var currentTime = DateTime.Now;
        var throttler = new LogThrottler(TimeSpan.FromSeconds(1), () => currentTime);

        // Act & Assert
        Assert.True(throttler.ShouldLog("Test message"));
        
        // 推进时间到间隔之后
        currentTime = currentTime.AddSeconds(1.1);
        Assert.True(throttler.ShouldLog("Test message"));
    }

    [Fact]
    public void ShouldLog_DifferentKeyFields_ShouldNotThrottle()
    {
        // Arrange
        var throttler = new LogThrottler();

        // Act & Assert - 不同的关键字段应该被视为不同的日志
        Assert.True(throttler.ShouldLog("Connection failed", "host1"));
        Assert.True(throttler.ShouldLog("Connection failed", "host2"));
        Assert.True(throttler.ShouldLog("Connection failed", "host3"));
    }

    [Fact]
    public void ShouldLog_SameKeyFields_ShouldThrottle()
    {
        // Arrange
        var currentTime = DateTime.Now;
        var throttler = new LogThrottler(TimeSpan.FromSeconds(1), () => currentTime);

        // Act & Assert - 相同的关键字段应该被节流
        Assert.True(throttler.ShouldLog("Connection failed", "host1", 8080));
        Assert.False(throttler.ShouldLog("Connection failed", "host1", 8080));
        Assert.False(throttler.ShouldLog("Connection failed", "host1", 8080));
    }

    [Fact]
    public void ShouldLog_HighFrequencyLogs_ShouldSignificantlyReduceOutput()
    {
        // Arrange
        var currentTime = DateTime.Now;
        var callCount = 0;
        var throttler = new LogThrottler(
            TimeSpan.FromSeconds(1), 
            () =>
            {
                // 每次调用时间推进 10ms，模拟高频调用
                var time = currentTime.AddMilliseconds(callCount * 10);
                callCount++;
                return time;
            });

        // Act - 在 1 秒内连续记录 100 次相同日志
        var loggedCount = 0;
        for (int i = 0; i < 100; i++)
        {
            if (throttler.ShouldLog("High frequency error"))
            {
                loggedCount++;
            }
        }

        // Assert - 实际输出次数应该显著少于 100（<= 2）
        Assert.True(loggedCount <= 2, $"Expected logged count <= 2, but got {loggedCount}");
    }

    [Fact]
    public void ShouldLog_DifferentMessages_ShouldNotThrottle()
    {
        // Arrange
        var throttler = new LogThrottler();

        // Act & Assert - 不同的消息模板应该被独立处理
        Assert.True(throttler.ShouldLog("Message 1"));
        Assert.True(throttler.ShouldLog("Message 2"));
        Assert.True(throttler.ShouldLog("Message 3"));
    }

    [Fact]
    public void ShouldLog_EmptyMessage_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var throttler = new LogThrottler();

        // Act & Assert - 空消息不应该被节流
        Assert.True(throttler.ShouldLog(""));
        Assert.True(throttler.ShouldLog(""));
        Assert.True(throttler.ShouldLog(null!));
    }

    [Fact]
    public void Clear_ShouldResetThrottlingState()
    {
        // Arrange
        var currentTime = DateTime.Now;
        var throttler = new LogThrottler(TimeSpan.FromSeconds(1), () => currentTime);

        // Act
        Assert.True(throttler.ShouldLog("Test message"));
        Assert.False(throttler.ShouldLog("Test message"));
        
        throttler.Clear();
        
        // Assert - 清空后应该可以再次记录
        Assert.True(throttler.ShouldLog("Test message"));
    }

    [Fact]
    public async Task ShouldLog_ConcurrentCalls_ShouldBeThreadSafe()
    {
        // Arrange
        var throttler = new LogThrottler(TimeSpan.FromSeconds(1));
        var loggedCount = 0;
        var tasks = new List<Task>();

        // Act - 并发调用
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    if (throttler.ShouldLog("Concurrent message"))
                    {
                        Interlocked.Increment(ref loggedCount);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks.ToArray());

        // Assert - 即使在并发情况下，第一个日志应该被记录，后续的在 1 秒内应该被节流
        // 由于时间可能推进，允许少量日志通过，但不应该是全部
        Assert.True(loggedCount >= 1 && loggedCount < 100, 
            $"Expected 1 <= logged count < 100, but got {loggedCount}");
    }

    [Fact]
    public void ShouldLog_WithNullKeyField_ShouldHandleGracefully()
    {
        // Arrange
        var throttler = new LogThrottler();

        // Act & Assert - 包含 null 的关键字段应该被正确处理
        Assert.True(throttler.ShouldLog("Message with null", null!, "value"));
        Assert.False(throttler.ShouldLog("Message with null", null!, "value"));
    }

    [Fact]
    public void ShouldLog_CustomInterval_ShouldRespectConfiguration()
    {
        // Arrange
        var currentTime = DateTime.Now;
        var throttler = new LogThrottler(TimeSpan.FromSeconds(5), () => currentTime);

        // Act & Assert
        Assert.True(throttler.ShouldLog("Test message"));
        
        // 3 秒后仍在节流期内
        currentTime = currentTime.AddSeconds(3);
        Assert.False(throttler.ShouldLog("Test message"));
        
        // 5 秒后应该可以再次记录
        currentTime = currentTime.AddSeconds(2.1);
        Assert.True(throttler.ShouldLog("Test message"));
    }
}
