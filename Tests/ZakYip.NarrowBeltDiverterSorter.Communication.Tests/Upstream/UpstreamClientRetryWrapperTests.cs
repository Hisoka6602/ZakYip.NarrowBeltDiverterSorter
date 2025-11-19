using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Tests.Upstream;

public class UpstreamClientRetryWrapperTests
{
    private readonly Mock<ISortingRuleEngineClient> _mockInnerClient;
    private readonly Mock<IOptionsMonitor<UpstreamOptions>> _mockOptionsMonitor;
    private readonly Mock<ILogger<UpstreamClientRetryWrapper>> _mockLogger;

    public UpstreamClientRetryWrapperTests()
    {
        _mockInnerClient = new Mock<ISortingRuleEngineClient>();
        _mockOptionsMonitor = new Mock<IOptionsMonitor<UpstreamOptions>>();
        _mockLogger = new Mock<ILogger<UpstreamClientRetryWrapper>>();
    }

    [Fact]
    public async Task ConnectAsync_ClientMode_RetriesOnFailure()
    {
        // Arrange
        var options = new UpstreamOptions
        {
            Mode = UpstreamMode.Tcp,
            Role = UpstreamRole.Client,
            Retry = new RetryOptions
            {
                InitialBackoffMs = 10,
                MaxBackoffMs = 100,
                BackoffMultiplier = 2.0
            }
        };

        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        var connectionAttempts = 0;
        _mockInnerClient.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                connectionAttempts++;
                return connectionAttempts >= 3; // 第3次成功
            });

        var wrapper = new UpstreamClientRetryWrapper(
            _mockInnerClient.Object,
            _mockOptionsMonitor.Object,
            _mockLogger.Object);

        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await wrapper.ConnectAsync(cts.Token);

        // Assert
        Assert.True(result);
        Assert.Equal(3, connectionAttempts);
    }

    [Fact]
    public async Task ConnectAsync_ServerMode_NoRetry()
    {
        // Arrange
        var options = new UpstreamOptions
        {
            Mode = UpstreamMode.Tcp,
            Role = UpstreamRole.Server
        };

        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);
        _mockInnerClient.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var wrapper = new UpstreamClientRetryWrapper(
            _mockInnerClient.Object,
            _mockOptionsMonitor.Object,
            _mockLogger.Object);

        // Act
        var result = await wrapper.ConnectAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockInnerClient.Verify(x => x.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendParcelCreatedAsync_CatchesAndLogsException()
    {
        // Arrange
        var options = new UpstreamOptions
        {
            Mode = UpstreamMode.Tcp,
            Role = UpstreamRole.Client
        };

        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);
        _mockInnerClient.Setup(x => x.SendParcelCreatedAsync(It.IsAny<ParcelCreatedMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection lost"));

        var wrapper = new UpstreamClientRetryWrapper(
            _mockInnerClient.Object,
            _mockOptionsMonitor.Object,
            _mockLogger.Object);

        var message = new ParcelCreatedMessage
        {
            ParcelId = 12345,
            CreatedTime = DateTimeOffset.UtcNow
        };

        // Act
        var result = await wrapper.SendParcelCreatedAsync(message);

        // Assert
        Assert.False(result);
    }
}
