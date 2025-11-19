using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.SystemState;

/// <summary>
/// 系统运行状态服务测试
/// </summary>
public class SystemRunStateServiceTests
{
    [Fact]
    public void Constructor_Should_Initialize_With_Stopped_State()
    {
        // Act
        var service = new SystemRunStateService();

        // Assert
        Assert.Equal(SystemRunState.Stopped, service.Current);
    }

    [Fact]
    public void TryHandleStart_From_Stopped_Initial_Should_Transition_To_Running()
    {
        // Arrange
        var service = new SystemRunStateService();
        Assert.Equal(SystemRunState.Stopped, service.Current);

        // Act
        var result = service.TryHandleStart();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SystemRunState.Running, service.Current);
    }

    [Fact]
    public void TryHandleStart_From_Stopped_After_Stop_Should_Transition_To_Running()
    {
        // Arrange
        var service = new SystemRunStateService();
        service.TryHandleStart(); // Start first
        service.TryHandleStop(); // Then stop
        Assert.Equal(SystemRunState.Stopped, service.Current);

        // Act
        var result = service.TryHandleStart();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SystemRunState.Running, service.Current);
    }

    [Fact]
    public void TryHandleStart_When_Already_Running_Should_Fail()
    {
        // Arrange
        var service = new SystemRunStateService();
        service.TryHandleStart();
        Assert.Equal(SystemRunState.Running, service.Current);

        // Act
        var result = service.TryHandleStart();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("已处于运行状态", result.ErrorMessage);
        Assert.Equal(SystemRunState.Running, service.Current);
    }

    [Fact]
    public void TryHandleStart_When_Fault_Should_Fail()
    {
        // Arrange
        var service = new SystemRunStateService();
        service.TryHandleEmergencyStop();
        Assert.Equal(SystemRunState.Fault, service.Current);

        // Act
        var result = service.TryHandleStart();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("故障状态", result.ErrorMessage);
        Assert.Equal(SystemRunState.Fault, service.Current);
    }

    [Fact]
    public void TryHandleStop_From_Running_Initial_Should_Transition_To_Stopped()
    {
        // Arrange
        var service = new SystemRunStateService();
        service.TryHandleStart(); // Start first since we begin at Stopped
        Assert.Equal(SystemRunState.Running, service.Current);

        // Act
        var result = service.TryHandleStop();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SystemRunState.Stopped, service.Current);
    }

    [Fact]
    public void TryHandleStop_From_Running_Should_Transition_To_Stopped()
    {
        // Arrange
        var service = new SystemRunStateService();
        service.TryHandleStart();
        Assert.Equal(SystemRunState.Running, service.Current);

        // Act
        var result = service.TryHandleStop();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SystemRunState.Stopped, service.Current);
    }

    [Fact]
    public void TryHandleStop_When_Already_Stopped_Should_Fail()
    {
        // Arrange
        var service = new SystemRunStateService();
        // Service starts at Stopped state
        Assert.Equal(SystemRunState.Stopped, service.Current);

        // Act
        var result = service.TryHandleStop();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("已处于停止状态", result.ErrorMessage);
        Assert.Equal(SystemRunState.Stopped, service.Current);
    }

    [Fact]
    public void TryHandleStop_When_Fault_Should_Fail()
    {
        // Arrange
        var service = new SystemRunStateService();
        service.TryHandleEmergencyStop();
        Assert.Equal(SystemRunState.Fault, service.Current);

        // Act
        var result = service.TryHandleStop();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("故障状态", result.ErrorMessage);
        Assert.Equal(SystemRunState.Fault, service.Current);
    }

    [Fact]
    public void TryHandleEmergencyStop_From_Stopped_Should_Transition_To_Fault()
    {
        // Arrange
        var service = new SystemRunStateService();
        Assert.Equal(SystemRunState.Stopped, service.Current);

        // Act
        var result = service.TryHandleEmergencyStop();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SystemRunState.Fault, service.Current);
    }

    [Fact]
    public void TryHandleEmergencyStop_From_Running_Should_Transition_To_Fault()
    {
        // Arrange
        var service = new SystemRunStateService();
        service.TryHandleStart();
        Assert.Equal(SystemRunState.Running, service.Current);

        // Act
        var result = service.TryHandleEmergencyStop();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SystemRunState.Fault, service.Current);
    }

    [Fact]
    public void TryHandleEmergencyStop_From_Stopped_After_Stop_Should_Transition_To_Fault()
    {
        // Arrange
        var service = new SystemRunStateService();
        service.TryHandleStart(); // Start first
        service.TryHandleStop(); // Then stop
        Assert.Equal(SystemRunState.Stopped, service.Current);

        // Act
        var result = service.TryHandleEmergencyStop();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SystemRunState.Fault, service.Current);
    }

    [Fact]
    public void TryHandleEmergencyStop_When_Already_Fault_Should_Fail()
    {
        // Arrange
        var service = new SystemRunStateService();
        service.TryHandleEmergencyStop();
        Assert.Equal(SystemRunState.Fault, service.Current);

        // Act
        var result = service.TryHandleEmergencyStop();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("已处于故障状态", result.ErrorMessage);
        Assert.Equal(SystemRunState.Fault, service.Current);
    }

    [Fact]
    public void TryHandleEmergencyReset_From_Fault_Should_Transition_To_Stopped()
    {
        // Arrange
        var service = new SystemRunStateService();
        service.TryHandleEmergencyStop();
        Assert.Equal(SystemRunState.Fault, service.Current);

        // Act
        var result = service.TryHandleEmergencyReset();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SystemRunState.Stopped, service.Current);
    }

    [Fact]
    public void TryHandleEmergencyReset_When_Not_Fault_Should_Fail()
    {
        // Arrange
        var service = new SystemRunStateService();
        Assert.Equal(SystemRunState.Stopped, service.Current);

        // Act
        var result = service.TryHandleEmergencyReset();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("不需要解除急停", result.ErrorMessage);
        Assert.Equal(SystemRunState.Stopped, service.Current);
    }

    [Fact]
    public void ValidateCanCreateParcel_When_Running_Should_Succeed()
    {
        // Arrange
        var service = new SystemRunStateService();
        service.TryHandleStart();
        Assert.Equal(SystemRunState.Running, service.Current);

        // Act
        var result = service.ValidateCanCreateParcel();

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateCanCreateParcel_When_Stopped_Initial_Should_Fail()
    {
        // Arrange
        var service = new SystemRunStateService();
        Assert.Equal(SystemRunState.Stopped, service.Current);

        // Act
        var result = service.ValidateCanCreateParcel();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("停止", result.ErrorMessage);
        Assert.Contains("禁止创建包裹", result.ErrorMessage);
    }

    [Fact]
    public void ValidateCanCreateParcel_When_Stopped_After_Stop_Should_Fail()
    {
        // Arrange
        var service = new SystemRunStateService();
        service.TryHandleStart(); // Start first
        service.TryHandleStop(); // Then stop
        Assert.Equal(SystemRunState.Stopped, service.Current);

        // Act
        var result = service.ValidateCanCreateParcel();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("停止", result.ErrorMessage);
        Assert.Contains("禁止创建包裹", result.ErrorMessage);
    }

    [Fact]
    public void ValidateCanCreateParcel_When_Fault_Should_Fail()
    {
        // Arrange
        var service = new SystemRunStateService();
        service.TryHandleEmergencyStop();
        Assert.Equal(SystemRunState.Fault, service.Current);

        // Act
        var result = service.ValidateCanCreateParcel();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("故障状态", result.ErrorMessage);
        Assert.Contains("禁止创建包裹", result.ErrorMessage);
    }

    [Fact]
    public void State_Transitions_Full_Lifecycle_Should_Work()
    {
        // Test full lifecycle: Stopped -> Running -> Stopped -> Running -> Fault -> Stopped
        var service = new SystemRunStateService();
        
        // Initial state
        Assert.Equal(SystemRunState.Stopped, service.Current);
        
        // Stopped -> Running
        Assert.True(service.TryHandleStart().IsSuccess);
        Assert.Equal(SystemRunState.Running, service.Current);
        
        // Running -> Stopped
        Assert.True(service.TryHandleStop().IsSuccess);
        Assert.Equal(SystemRunState.Stopped, service.Current);
        
        // Stopped -> Running
        Assert.True(service.TryHandleStart().IsSuccess);
        Assert.Equal(SystemRunState.Running, service.Current);
        
        // Running -> Fault
        Assert.True(service.TryHandleEmergencyStop().IsSuccess);
        Assert.Equal(SystemRunState.Fault, service.Current);
        
        // Fault -> Stopped
        Assert.True(service.TryHandleEmergencyReset().IsSuccess);
        Assert.Equal(SystemRunState.Stopped, service.Current);
    }
}
