using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Tests;

/// <summary>
/// 主线控制服务测试
/// </summary>
public class MainLineControlServiceTests
{
    private readonly Mock<ILogger<MainLineControlService>> _mockLogger;
    private readonly Mock<IMainLineDrivePort> _mockDrivePort;
    private readonly Mock<IMainLineFeedbackPort> _mockFeedbackPort;
    private readonly MainLineControlOptions _options;

    public MainLineControlServiceTests()
    {
        _mockLogger = new Mock<ILogger<MainLineControlService>>();
        _mockDrivePort = new Mock<IMainLineDrivePort>();
        _mockFeedbackPort = new Mock<IMainLineFeedbackPort>();
        _options = new MainLineControlOptions
        {
            TargetSpeedMmps = 1000m,
            LoopPeriod = TimeSpan.FromMilliseconds(100),
            ProportionalGain = 1.0m,
            IntegralGain = 0.1m,
            DerivativeGain = 0.01m,
            StableDeadbandMmps = 10m,
            StableHold = TimeSpan.FromSeconds(2),
            MinOutputMmps = 0m,
            MaxOutputMmps = 5000m,
            IntegralLimit = 1000m
        };
    }

    private MainLineControlService CreateService()
    {
        return new MainLineControlService(
            _mockLogger.Object,
            _mockDrivePort.Object,
            _mockFeedbackPort.Object,
            Options.Create(_options));
    }

    [Fact]
    public void SetTargetSpeed_Should_Update_Target_Speed()
    {
        // Arrange
        var service = CreateService();
        var newSpeed = 1500m;

        // Act
        service.SetTargetSpeed(newSpeed);

        // Assert
        Assert.Equal(newSpeed, service.GetTargetSpeed());
    }

    [Fact]
    public async Task StartAsync_Should_Call_DrivePort_And_Set_Running_State()
    {
        // Arrange
        var service = CreateService();
        _mockDrivePort.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await service.StartAsync();

        // Assert
        Assert.True(result);
        Assert.True(service.IsRunning);
        _mockDrivePort.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_Should_Return_False_When_Already_Running()
    {
        // Arrange
        var service = CreateService();
        _mockDrivePort.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        await service.StartAsync();

        // Act
        var result = await service.StartAsync();

        // Assert
        Assert.False(result);
        _mockDrivePort.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_Should_Call_DrivePort_And_Clear_Running_State()
    {
        // Arrange
        var service = CreateService();
        _mockDrivePort.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockDrivePort.Setup(x => x.StopAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        await service.StartAsync();

        // Act
        var result = await service.StopAsync();

        // Assert
        Assert.True(result);
        Assert.False(service.IsRunning);
        _mockDrivePort.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteControlLoopAsync_Should_Return_False_When_Not_Running()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ExecuteControlLoopAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteControlLoopAsync_Should_Read_Feedback_And_Call_DrivePort()
    {
        // Arrange
        var service = CreateService();
        _mockDrivePort.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockDrivePort.Setup(x => x.SetTargetSpeedAsync(It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFeedbackPort.Setup(x => x.GetCurrentSpeed())
            .Returns(950.0);
        _mockFeedbackPort.Setup(x => x.GetFaultCode())
            .Returns((int?)null);

        await service.StartAsync();

        // Act
        var result = await service.ExecuteControlLoopAsync();

        // Assert
        Assert.True(result);
        _mockFeedbackPort.Verify(x => x.GetCurrentSpeed(), Times.Once);
        _mockDrivePort.Verify(x => x.SetTargetSpeedAsync(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteControlLoopAsync_Should_Stop_On_Fault()
    {
        // Arrange
        var service = CreateService();
        _mockDrivePort.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFeedbackPort.Setup(x => x.GetCurrentSpeed())
            .Returns(950.0);
        _mockFeedbackPort.Setup(x => x.GetFaultCode())
            .Returns(123);

        await service.StartAsync();

        // Act
        var result = await service.ExecuteControlLoopAsync();

        // Assert
        Assert.False(result);
        Assert.False(service.IsRunning);
    }

    [Fact]
    public void ConvertMmpsToHz_Should_Calculate_Correctly()
    {
        // Arrange
        var speedMmps = 1000m;
        var mmPerRotation = 100m;

        // Act
        var frequencyHz = MainLineControlService.ConvertMmpsToHz(speedMmps, mmPerRotation);

        // Assert
        Assert.Equal(10m, frequencyHz);
    }

    [Fact]
    public void ConvertHzToMmps_Should_Calculate_Correctly()
    {
        // Arrange
        var frequencyHz = 10m;
        var mmPerRotation = 100m;

        // Act
        var speedMmps = MainLineControlService.ConvertHzToMmps(frequencyHz, mmPerRotation);

        // Assert
        Assert.Equal(1000m, speedMmps);
    }
}
