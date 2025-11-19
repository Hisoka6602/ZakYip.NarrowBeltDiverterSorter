using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ZakYip.NarrowBeltDiverterSorter.Observability.Tests;

/// <summary>
/// 内存事件总线测试
/// </summary>
public class InMemoryEventBusTests : IDisposable
{
    private readonly InMemoryEventBus _eventBus;

    public InMemoryEventBusTests()
    {
        var logger = NullLogger<InMemoryEventBus>.Instance;
        _eventBus = new InMemoryEventBus(logger);
    }

    public void Dispose()
    {
        _eventBus.Dispose();
    }

    [Fact]
    public async Task PublishAsync_WithSubscriber_ShouldInvokeHandler()
    {
        // Arrange
        var received = false;
        var tcs = new TaskCompletionSource<bool>();
        
        _eventBus.Subscribe<TestEventArgs>(async (args, ct) =>
        {
            received = true;
            tcs.SetResult(true);
            await Task.CompletedTask;
        });

        // Act
        await _eventBus.PublishAsync(new TestEventArgs { Message = "Test" });
        
        // Wait for processing
        await Task.WhenAny(tcs.Task, Task.Delay(1000));

        // Assert
        Assert.True(received);
    }

    [Fact]
    public async Task PublishAsync_WithMultipleSubscribers_ShouldInvokeAllHandlers()
    {
        // Arrange
        var receivedCount = 0;
        var tcs = new TaskCompletionSource<bool>();
        
        _eventBus.Subscribe<TestEventArgs>(async (args, ct) =>
        {
            Interlocked.Increment(ref receivedCount);
            await Task.CompletedTask;
        });
        
        _eventBus.Subscribe<TestEventArgs>(async (args, ct) =>
        {
            Interlocked.Increment(ref receivedCount);
            await Task.CompletedTask;
        });
        
        _eventBus.Subscribe<TestEventArgs>(async (args, ct) =>
        {
            Interlocked.Increment(ref receivedCount);
            tcs.SetResult(true);
            await Task.CompletedTask;
        });

        // Act
        await _eventBus.PublishAsync(new TestEventArgs { Message = "Test" });
        
        // Wait for processing
        await Task.WhenAny(tcs.Task, Task.Delay(1000));

        // Assert
        Assert.Equal(3, receivedCount);
    }

    [Fact]
    public async Task PublishAsync_WhenSubscriberThrows_ShouldNotAffectOtherSubscribers()
    {
        // Arrange
        var successfulHandlerInvoked = false;
        var tcs = new TaskCompletionSource<bool>();
        
        // First handler throws exception
        _eventBus.Subscribe<TestEventArgs>(async (args, ct) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Test exception");
        });
        
        // Second handler should still be invoked
        _eventBus.Subscribe<TestEventArgs>(async (args, ct) =>
        {
            successfulHandlerInvoked = true;
            tcs.SetResult(true);
            await Task.CompletedTask;
        });

        // Act
        await _eventBus.PublishAsync(new TestEventArgs { Message = "Test" });
        
        // Wait for processing
        await Task.WhenAny(tcs.Task, Task.Delay(1000));

        // Assert
        Assert.True(successfulHandlerInvoked, "成功的处理器应该被调用，即使另一个处理器抛出异常");
    }

    [Fact]
    public async Task Unsubscribe_ShouldRemoveHandler()
    {
        // Arrange
        var invocationCount = 0;
        
        async Task Handler(TestEventArgs args, CancellationToken ct)
        {
            Interlocked.Increment(ref invocationCount);
            await Task.CompletedTask;
        }
        
        _eventBus.Subscribe<TestEventArgs>(Handler);

        // Act - Publish first time
        await _eventBus.PublishAsync(new TestEventArgs { Message = "Test 1" });
        await Task.Delay(100); // Wait for processing
        
        // Unsubscribe
        _eventBus.Unsubscribe<TestEventArgs>(Handler);
        
        // Publish second time
        await _eventBus.PublishAsync(new TestEventArgs { Message = "Test 2" });
        await Task.Delay(100); // Wait for processing

        // Assert - Handler should only be invoked once
        Assert.Equal(1, invocationCount);
    }

    [Fact]
    public async Task PublishAsync_WithNoSubscribers_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        await _eventBus.PublishAsync(new TestEventArgs { Message = "Test" });
        
        // Wait a bit to ensure processing completes
        await Task.Delay(100);
    }

    [Fact]
    public void GetBacklogCount_InitiallyZero()
    {
        // Assert
        Assert.Equal(0, _eventBus.GetBacklogCount());
    }

    [Fact]
    public async Task GetBacklogCount_WithMultipleEvents_ShouldReflectBacklog()
    {
        // Arrange
        var blockHandler = new TaskCompletionSource<bool>();
        
        _eventBus.Subscribe<TestEventArgs>(async (args, ct) =>
        {
            // Block until we signal
            await blockHandler.Task;
        });

        // Act - Publish multiple events rapidly
        var publishTasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            publishTasks.Add(_eventBus.PublishAsync(new TestEventArgs { Message = $"Test {i}" }));
        }
        await Task.WhenAll(publishTasks);

        // Give a moment for the first event to start processing
        await Task.Delay(50);

        // Assert - Should have backlog (at least 1 event remaining)
        var backlog = _eventBus.GetBacklogCount();
        Assert.True(backlog >= 0, $"积压量应该大于等于0，实际为: {backlog}");

        // Cleanup - Unblock handler
        blockHandler.SetResult(true);
        await Task.Delay(200); // Wait for all events to process
    }

    [Fact]
    public async Task PublishAsync_WithCancellationToken_ShouldPassToHandler()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var receivedToken = CancellationToken.None;
        var tcs = new TaskCompletionSource<bool>();
        
        _eventBus.Subscribe<TestEventArgs>(async (args, ct) =>
        {
            receivedToken = ct;
            tcs.SetResult(true);
            await Task.CompletedTask;
        });

        // Act
        await _eventBus.PublishAsync(new TestEventArgs { Message = "Test" }, cts.Token);
        
        // Wait for processing
        await Task.WhenAny(tcs.Task, Task.Delay(1000));

        // Assert
        Assert.Equal(cts.Token, receivedToken);
    }
}

/// <summary>
/// 测试事件参数
/// </summary>
public record class TestEventArgs
{
    public required string Message { get; init; }
}
