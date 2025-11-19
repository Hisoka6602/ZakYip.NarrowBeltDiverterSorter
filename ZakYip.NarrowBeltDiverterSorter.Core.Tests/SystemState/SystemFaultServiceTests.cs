using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.SystemState;

/// <summary>
/// SystemFaultService 单元测试
/// </summary>
public class SystemFaultServiceTests
{
    [Fact]
    public void RegisterFault_AddsFaultToCollection()
    {
        // Arrange
        var service = new SystemFaultService();

        // Act
        service.RegisterFault(SystemFaultCode.EmergencyStopActive, "测试急停", isBlocking: true);

        // Assert
        var faults = service.GetActiveFaults();
        Assert.Single(faults);
        Assert.Equal(SystemFaultCode.EmergencyStopActive, faults[0].FaultCode);
        Assert.Equal("测试急停", faults[0].Message);
        Assert.True(faults[0].IsBlocking);
    }

    [Fact]
    public void RegisterFault_DuplicateFault_DoesNotAddTwice()
    {
        // Arrange
        var service = new SystemFaultService();

        // Act
        service.RegisterFault(SystemFaultCode.EmergencyStopActive, "测试急停", isBlocking: true);
        service.RegisterFault(SystemFaultCode.EmergencyStopActive, "重复的急停", isBlocking: true);

        // Assert
        var faults = service.GetActiveFaults();
        Assert.Single(faults);
    }

    [Fact]
    public void HasBlockingFault_ReturnsTrueWhenBlockingFaultExists()
    {
        // Arrange
        var service = new SystemFaultService();

        // Act
        service.RegisterFault(SystemFaultCode.EmergencyStopActive, "测试急停", isBlocking: true);

        // Assert
        Assert.True(service.HasBlockingFault());
    }

    [Fact]
    public void HasBlockingFault_ReturnsFalseWhenNoBlockingFaultExists()
    {
        // Arrange
        var service = new SystemFaultService();

        // Act
        service.RegisterFault(SystemFaultCode.RuleEngineUnavailable, "测试规则引擎", isBlocking: false);

        // Assert
        Assert.False(service.HasBlockingFault());
    }

    [Fact]
    public void ClearFault_RemovesFaultFromCollection()
    {
        // Arrange
        var service = new SystemFaultService();
        service.RegisterFault(SystemFaultCode.EmergencyStopActive, "测试急停", isBlocking: true);

        // Act
        var removed = service.ClearFault(SystemFaultCode.EmergencyStopActive);

        // Assert
        Assert.True(removed);
        Assert.Empty(service.GetActiveFaults());
    }

    [Fact]
    public void ClearAllFaults_RemovesAllFaults()
    {
        // Arrange
        var service = new SystemFaultService();
        service.RegisterFault(SystemFaultCode.EmergencyStopActive, "测试急停", isBlocking: true);
        service.RegisterFault(SystemFaultCode.FieldBusDisconnected, "测试现场总线", isBlocking: true);

        // Act
        service.ClearAllFaults();

        // Assert
        Assert.Empty(service.GetActiveFaults());
    }

    [Fact]
    public void FaultAdded_EventFired_WhenFaultRegistered()
    {
        // Arrange
        var service = new SystemFaultService();
        SystemFaultEventArgs? receivedArgs = null;
        service.FaultAdded += (sender, args) => receivedArgs = args;

        // Act
        service.RegisterFault(SystemFaultCode.EmergencyStopActive, "测试急停", isBlocking: true);

        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(SystemFaultCode.EmergencyStopActive, receivedArgs.FaultCode);
        Assert.Equal("测试急停", receivedArgs.Message);
    }

    [Fact]
    public void FaultCleared_EventFired_WhenFaultCleared()
    {
        // Arrange
        var service = new SystemFaultService();
        service.RegisterFault(SystemFaultCode.EmergencyStopActive, "测试急停", isBlocking: true);
        
        SystemFaultCode? clearedCode = null;
        service.FaultCleared += (sender, code) => clearedCode = code;

        // Act
        service.ClearFault(SystemFaultCode.EmergencyStopActive);

        // Assert
        Assert.NotNull(clearedCode);
        Assert.Equal(SystemFaultCode.EmergencyStopActive, clearedCode);
    }
}
