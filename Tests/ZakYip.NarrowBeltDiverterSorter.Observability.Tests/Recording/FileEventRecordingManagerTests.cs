using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Observability.Recording;

namespace ZakYip.NarrowBeltDiverterSorter.Observability.Tests.Recording;

/// <summary>
/// 文件事件录制管理器测试
/// </summary>
public class FileEventRecordingManagerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileEventRecordingManager _manager;

    public FileEventRecordingManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"recording-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        var logger = NullLogger<FileEventRecordingManager>.Instance;
        _manager = new FileEventRecordingManager(_testDirectory, logger);
    }

    public void Dispose()
    {
        _manager.Dispose();
        
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task StartSessionAsync_ShouldCreateSession()
    {
        // Act
        var session = await _manager.StartSessionAsync("Test Session", "Test Description");

        // Assert
        Assert.NotEqual(Guid.Empty, session.SessionId);
        Assert.Equal("Test Session", session.Name);
        Assert.Equal("Test Description", session.Description);
        Assert.False(session.IsCompleted);
        Assert.Null(session.StoppedAt);
        Assert.Equal(0, session.EventCount);
    }

    [Fact]
    public async Task StartSessionAsync_WithActiveSession_ShouldThrowException()
    {
        // Arrange
        await _manager.StartSessionAsync("Session 1");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _manager.StartSessionAsync("Session 2"));
    }

    [Fact]
    public async Task StopSessionAsync_ShouldUpdateSession()
    {
        // Arrange
        var session = await _manager.StartSessionAsync("Test Session");
        
        // Act
        await _manager.StopSessionAsync(session.SessionId);

        // Assert
        var updatedSession = await _manager.GetSessionAsync(session.SessionId);
        Assert.NotNull(updatedSession);
        Assert.True(updatedSession.IsCompleted);
        Assert.NotNull(updatedSession.StoppedAt);
    }

    [Fact]
    public async Task StopSessionAsync_WithInactiveSession_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _manager.StopSessionAsync(Guid.NewGuid()));
    }

    [Fact]
    public void GetActiveSession_WithNoActiveSession_ShouldReturnNull()
    {
        // Act
        var activeSession = _manager.GetActiveSession();

        // Assert
        Assert.Null(activeSession);
    }

    [Fact]
    public async Task GetActiveSession_WithActiveSession_ShouldReturnSession()
    {
        // Arrange
        var session = await _manager.StartSessionAsync("Test Session");

        // Act
        var activeSession = _manager.GetActiveSession();

        // Assert
        Assert.NotNull(activeSession);
        Assert.Equal(session.SessionId, activeSession.SessionId);
    }

    [Fact]
    public async Task ListSessionsAsync_ShouldReturnAllSessions()
    {
        // Arrange
        var session1 = await _manager.StartSessionAsync("Session 1");
        await _manager.StopSessionAsync(session1.SessionId);
        
        var session2 = await _manager.StartSessionAsync("Session 2");
        await _manager.StopSessionAsync(session2.SessionId);

        // Act
        var sessions = await _manager.ListSessionsAsync();

        // Assert
        Assert.Equal(2, sessions.Count);
        Assert.Contains(sessions, s => s.SessionId == session1.SessionId);
        Assert.Contains(sessions, s => s.SessionId == session2.SessionId);
    }

    [Fact]
    public async Task RecordAsync_WithActiveSession_ShouldWriteEvent()
    {
        // Arrange
        var session = await _manager.StartSessionAsync("Test Session");
        var testEvent = new TestEvent { Message = "Test message", Value = 42 };

        // Act
        await _manager.RecordAsync("TestEvent", testEvent, DateTimeOffset.Now);
        await _manager.StopSessionAsync(session.SessionId);

        // Assert
        var sessionDir = Path.Combine(_testDirectory, session.SessionId.ToString());
        var eventsFile = Path.Combine(sessionDir, "events.ndjson");
        
        Assert.True(File.Exists(eventsFile));
        var lines = await File.ReadAllLinesAsync(eventsFile);
        Assert.NotEmpty(lines);
    }

    [Fact]
    public async Task RecordAsync_WithoutActiveSession_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Message = "Test message", Value = 42 };

        // Act & Assert - should not throw
        await _manager.RecordAsync("TestEvent", testEvent, DateTimeOffset.Now);
    }

    [Fact]
    public async Task RecordAsync_MultipleEvents_ShouldWriteAllEvents()
    {
        // Arrange
        var session = await _manager.StartSessionAsync("Test Session");
        
        // Act
        for (int i = 0; i < 10; i++)
        {
            var testEvent = new TestEvent { Message = $"Event {i}", Value = i };
            await _manager.RecordAsync("TestEvent", testEvent, DateTimeOffset.Now);
        }
        
        await _manager.StopSessionAsync(session.SessionId);

        // Assert
        var sessionDir = Path.Combine(_testDirectory, session.SessionId.ToString());
        var eventsFile = Path.Combine(sessionDir, "events.ndjson");
        
        var lines = await File.ReadAllLinesAsync(eventsFile);
        Assert.Equal(10, lines.Length);
    }

    [Fact]
    public async Task GetSessionAsync_WithNonExistentSession_ShouldReturnNull()
    {
        // Act
        var session = await _manager.GetSessionAsync(Guid.NewGuid());

        // Assert
        Assert.Null(session);
    }

    private class TestEvent
    {
        public string Message { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
