using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Chutes;
using ZakYip.NarrowBeltDiverterSorter.Execution.Chute;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Tests.Chute;

/// <summary>
/// ChuteTransmitterDriver 单元测试
/// </summary>
public class ChuteTransmitterDriverTests
{
    [Fact]
    public void GetRegisteredBindings_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var mockFieldBusClient = new Mock<IFieldBusClient>();
        var mappingConfig = new ChuteMappingConfiguration();
        var logger = NullLogger<ChuteTransmitterDriver>.Instance;

        var driver = new ChuteTransmitterDriver(mockFieldBusClient.Object, mappingConfig, logger);

        // Act
        var bindings = driver.GetRegisteredBindings();

        // Assert
        Assert.NotNull(bindings);
        Assert.Empty(bindings);
    }

    [Fact]
    public void RegisterBindings_ShouldStore_Bindings()
    {
        // Arrange
        var mockFieldBusClient = new Mock<IFieldBusClient>();
        var mappingConfig = new ChuteMappingConfiguration();
        var logger = NullLogger<ChuteTransmitterDriver>.Instance;

        var driver = new ChuteTransmitterDriver(mockFieldBusClient.Object, mappingConfig, logger);

        var bindings = new List<ChuteTransmitterBinding>
        {
            new ChuteTransmitterBinding { ChuteId = 1, BusKey = "Bus1", OutputBitIndex = 0, IsNormallyOn = false },
            new ChuteTransmitterBinding { ChuteId = 2, BusKey = "Bus1", OutputBitIndex = 1, IsNormallyOn = false },
            new ChuteTransmitterBinding { ChuteId = 3, BusKey = "Bus1", OutputBitIndex = 2, IsNormallyOn = true }
        };

        // Act
        driver.RegisterBindings(bindings);
        var registered = driver.GetRegisteredBindings();

        // Assert
        Assert.Equal(3, registered.Count);
        Assert.Contains(registered, b => b.ChuteId == 1 && b.BusKey == "Bus1" && b.OutputBitIndex == 0);
        Assert.Contains(registered, b => b.ChuteId == 2 && b.BusKey == "Bus1" && b.OutputBitIndex == 1);
        Assert.Contains(registered, b => b.ChuteId == 3 && b.BusKey == "Bus1" && b.OutputBitIndex == 2 && b.IsNormallyOn);
    }

    [Fact]
    public void RegisterBindings_CalledTwice_ShouldReplace_PreviousBindings()
    {
        // Arrange
        var mockFieldBusClient = new Mock<IFieldBusClient>();
        var mappingConfig = new ChuteMappingConfiguration();
        var logger = NullLogger<ChuteTransmitterDriver>.Instance;

        var driver = new ChuteTransmitterDriver(mockFieldBusClient.Object, mappingConfig, logger);

        var firstBindings = new List<ChuteTransmitterBinding>
        {
            new ChuteTransmitterBinding { ChuteId = 1, BusKey = "Bus1", OutputBitIndex = 0, IsNormallyOn = false }
        };

        var secondBindings = new List<ChuteTransmitterBinding>
        {
            new ChuteTransmitterBinding { ChuteId = 2, BusKey = "Bus2", OutputBitIndex = 5, IsNormallyOn = true },
            new ChuteTransmitterBinding { ChuteId = 3, BusKey = "Bus2", OutputBitIndex = 6, IsNormallyOn = false }
        };

        // Act
        driver.RegisterBindings(firstBindings);
        driver.RegisterBindings(secondBindings);
        var registered = driver.GetRegisteredBindings();

        // Assert
        Assert.Equal(2, registered.Count);
        Assert.DoesNotContain(registered, b => b.ChuteId == 1);
        Assert.Contains(registered, b => b.ChuteId == 2);
        Assert.Contains(registered, b => b.ChuteId == 3);
    }
}
