using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ZakYip.NarrowBeltDiverterSorter.Observability.Recording;

/// <summary>
/// 基于文件的事件录制管理器
/// 使用NDJSON格式存储事件流
/// </summary>
public class FileEventRecordingManager : IEventRecordingManager, IEventRecorder, IDisposable, IAsyncDisposable
{
    private readonly string _baseDirectory;
    private readonly ILogger<FileEventRecordingManager> _logger;
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private readonly ConcurrentDictionary<Guid, SessionWriter> _activeWriters = new();
    
    private RecordingSessionInfo? _activeSession;

    public FileEventRecordingManager(ILogger<FileEventRecordingManager> logger)
        : this("recordings", logger)
    {
    }

    public FileEventRecordingManager(string baseDirectory, ILogger<FileEventRecordingManager> logger)
    {
        _baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // 确保基础目录存在
        Directory.CreateDirectory(_baseDirectory);
    }

    /// <inheritdoc />
    public async Task<RecordingSessionInfo> StartSessionAsync(string name, string? description = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Session name cannot be null or whitespace.", nameof(name));
        }

        await _sessionLock.WaitAsync(ct);
        try
        {
            // 检查是否已有活动会话
            if (_activeSession != null)
            {
                throw new InvalidOperationException($"A recording session is already active: {_activeSession.SessionId}");
            }

            var sessionId = Guid.NewGuid();
            var startedAt = DateTimeOffset.UtcNow;
            
            var session = new RecordingSessionInfo
            {
                SessionId = sessionId,
                Name = name,
                Description = description,
                StartedAt = startedAt,
                IsCompleted = false,
                EventCount = 0
            };

            // 创建会话目录
            var sessionDir = GetSessionDirectory(sessionId);
            Directory.CreateDirectory(sessionDir);

            // 创建会话元数据文件
            await SaveSessionMetadataAsync(session, ct);

            // 创建事件写入器
            var eventsFile = Path.Combine(sessionDir, "events.ndjson");
            var writer = new SessionWriter(eventsFile);
            _activeWriters[sessionId] = writer;

            _activeSession = session;

            _logger.LogInformation(
                "Started recording session {SessionId} '{Name}'",
                sessionId, name);

            return session;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        await _sessionLock.WaitAsync(ct);
        try
        {
            if (_activeSession == null || _activeSession.SessionId != sessionId)
            {
                throw new InvalidOperationException($"Session {sessionId} is not active.");
            }

            // 关闭写入器
            if (_activeWriters.TryRemove(sessionId, out var writer))
            {
                await writer.DisposeAsync();
            }

            // 更新会话元数据
            var updatedSession = _activeSession with
            {
                StoppedAt = DateTimeOffset.UtcNow,
                IsCompleted = true,
                EventCount = writer?.EventCount ?? 0
            };

            await SaveSessionMetadataAsync(updatedSession, ct);

            _activeSession = null;

            _logger.LogInformation(
                "Stopped recording session {SessionId}, recorded {EventCount} events",
                sessionId, updatedSession.EventCount);
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecordingSessionInfo>> ListSessionsAsync(CancellationToken ct = default)
    {
        var sessions = new List<RecordingSessionInfo>();

        if (!Directory.Exists(_baseDirectory))
        {
            return sessions;
        }

        var sessionDirs = Directory.GetDirectories(_baseDirectory);
        
        foreach (var dir in sessionDirs)
        {
            var sessionFile = Path.Combine(dir, "session.json");
            if (File.Exists(sessionFile))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(sessionFile, ct);
                    var session = JsonSerializer.Deserialize<RecordingSessionInfo>(json);
                    if (session != null)
                    {
                        sessions.Add(session);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read session metadata from {Path}", sessionFile);
                }
            }
        }

        return sessions.OrderByDescending(s => s.StartedAt).ToList();
    }

    /// <inheritdoc />
    public async Task<RecordingSessionInfo?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var sessionFile = Path.Combine(GetSessionDirectory(sessionId), "session.json");
        
        if (!File.Exists(sessionFile))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(sessionFile, ct);
            return JsonSerializer.Deserialize<RecordingSessionInfo>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read session {SessionId}", sessionId);
            return null;
        }
    }

    /// <inheritdoc />
    public RecordingSessionInfo? GetActiveSession()
    {
        return _activeSession;
    }

    /// <inheritdoc />
    public async ValueTask RecordAsync<TEvent>(
        string eventType,
        TEvent payload,
        DateTimeOffset timestamp,
        string? correlationId = null,
        CancellationToken ct = default) where TEvent : class
    {
        var session = _activeSession;
        if (session == null)
        {
            // 没有活动会话，静默忽略
            return;
        }

        if (!_activeWriters.TryGetValue(session.SessionId, out var writer))
        {
            _logger.LogWarning("No writer found for active session {SessionId}", session.SessionId);
            return;
        }

        try
        {
            var payloadJson = JsonSerializer.Serialize(payload);
            
            var envelope = new RecordedEventEnvelope
            {
                SessionId = session.SessionId,
                Timestamp = timestamp,
                EventType = eventType,
                PayloadJson = payloadJson,
                CorrelationId = correlationId
            };

            await writer.WriteEventAsync(envelope, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record event {EventType} for session {SessionId}",
                eventType, session.SessionId);
        }
    }

    private string GetSessionDirectory(Guid sessionId)
    {
        return Path.Combine(_baseDirectory, sessionId.ToString());
    }

    private async Task SaveSessionMetadataAsync(RecordingSessionInfo session, CancellationToken ct)
    {
        var sessionDir = GetSessionDirectory(session.SessionId);
        var sessionFile = Path.Combine(sessionDir, "session.json");
        
        var json = JsonSerializer.Serialize(session, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        await File.WriteAllTextAsync(sessionFile, json, ct);
    }

    public void Dispose()
    {
        _sessionLock.Dispose();
        
        foreach (var writer in _activeWriters.Values)
        {
            writer.Dispose();
        }
        
        _activeWriters.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        _sessionLock.Dispose();
        
        foreach (var writer in _activeWriters.Values)
        {
            await writer.DisposeAsync();
        }
        
        _activeWriters.Clear();
    }

    /// <summary>
    /// 会话事件写入器
    /// </summary>
    private class SessionWriter : IDisposable, IAsyncDisposable
    {
        private readonly string _filePath;
        private readonly StreamWriter _writer;
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private int _eventCount;

        public int EventCount => _eventCount;

        public SessionWriter(string filePath)
        {
            _filePath = filePath;
            _writer = new StreamWriter(filePath, append: true)
            {
                AutoFlush = true
            };
        }

        public async Task WriteEventAsync(RecordedEventEnvelope envelope, CancellationToken ct)
        {
            await _writeLock.WaitAsync(ct);
            try
            {
                var json = JsonSerializer.Serialize(envelope);
                await _writer.WriteLineAsync(json);
                Interlocked.Increment(ref _eventCount);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public void Dispose()
        {
            _writeLock.Dispose();
            _writer.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            _writeLock.Dispose();
            await _writer.DisposeAsync();
        }
    }
}
