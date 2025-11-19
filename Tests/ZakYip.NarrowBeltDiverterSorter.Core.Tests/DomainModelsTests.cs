using ZakYip.NarrowBeltDiverterSorter.Core.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests;

/// <summary>
/// 领域模型测试
/// </summary>
public class DomainModelsTests
{
    [Fact]
    public void ParcelSnapshot_Can_Transition_From_WaitingForRouting_To_Routed()
    {
        // Arrange
        var parcelId = new ParcelId(1234567890123);
        var chuteId = new ChuteId(5);
        var createdAt = DateTimeOffset.UtcNow;

        var waitingParcel = new ParcelSnapshot
        {
            ParcelId = parcelId,
            RouteState = ParcelRouteState.WaitingForRouting,
            CreatedAt = createdAt
        };

        // Act - 模拟状态转换：分配了格口后变为已路由状态
        var routedParcel = waitingParcel with
        {
            TargetChuteId = chuteId,
            RouteState = ParcelRouteState.Routed
        };

        // Assert
        Assert.Equal(ParcelRouteState.WaitingForRouting, waitingParcel.RouteState);
        Assert.Null(waitingParcel.TargetChuteId);

        Assert.Equal(ParcelRouteState.Routed, routedParcel.RouteState);
        Assert.NotNull(routedParcel.TargetChuteId);
        Assert.Equal(chuteId.Value, routedParcel.TargetChuteId.Value.Value);
    }

    [Fact]
    public void ParcelSnapshot_Can_Transition_From_Routed_To_Sorting()
    {
        // Arrange
        var parcelId = new ParcelId(1234567890123);
        var chuteId = new ChuteId(5);
        var cartId = new CartId(10);
        var createdAt = DateTimeOffset.UtcNow;
        var loadedAt = DateTimeOffset.UtcNow.AddSeconds(5);

        var routedParcel = new ParcelSnapshot
        {
            ParcelId = parcelId,
            TargetChuteId = chuteId,
            RouteState = ParcelRouteState.Routed,
            CreatedAt = createdAt
        };

        // Act - 模拟状态转换：包裹装载到小车上后变为分拣中状态
        var sortingParcel = routedParcel with
        {
            BoundCartId = cartId,
            LoadedAt = loadedAt,
            RouteState = ParcelRouteState.Sorting
        };

        // Assert
        Assert.Equal(ParcelRouteState.Routed, routedParcel.RouteState);
        Assert.Null(routedParcel.BoundCartId);
        Assert.Null(routedParcel.LoadedAt);

        Assert.Equal(ParcelRouteState.Sorting, sortingParcel.RouteState);
        Assert.NotNull(sortingParcel.BoundCartId);
        Assert.NotNull(sortingParcel.LoadedAt);
        Assert.Equal(cartId.Value, sortingParcel.BoundCartId.Value.Value);
        Assert.Equal(loadedAt, sortingParcel.LoadedAt);
    }

    [Fact]
    public void ParcelSnapshot_Can_Transition_From_Sorting_To_Sorted()
    {
        // Arrange
        var parcelId = new ParcelId(1234567890123);
        var chuteId = new ChuteId(5);
        var cartId = new CartId(10);
        var createdAt = DateTimeOffset.UtcNow;
        var loadedAt = DateTimeOffset.UtcNow.AddSeconds(5);
        var sortedAt = DateTimeOffset.UtcNow.AddSeconds(10);

        var sortingParcel = new ParcelSnapshot
        {
            ParcelId = parcelId,
            TargetChuteId = chuteId,
            BoundCartId = cartId,
            RouteState = ParcelRouteState.Sorting,
            CreatedAt = createdAt,
            LoadedAt = loadedAt
        };

        // Act - 模拟状态转换：包裹分拣完成
        var sortedParcel = sortingParcel with
        {
            RouteState = ParcelRouteState.Sorted,
            SortedAt = sortedAt
        };

        // Assert
        Assert.Equal(ParcelRouteState.Sorting, sortingParcel.RouteState);
        Assert.Null(sortingParcel.SortedAt);

        Assert.Equal(ParcelRouteState.Sorted, sortedParcel.RouteState);
        Assert.NotNull(sortedParcel.SortedAt);
        Assert.Equal(sortedAt, sortedParcel.SortedAt);
    }

    [Fact]
    public void ParcelSnapshot_Can_Transition_To_ForceEjected()
    {
        // Arrange
        var parcelId = new ParcelId(1234567890123);
        var createdAt = DateTimeOffset.UtcNow;

        var waitingParcel = new ParcelSnapshot
        {
            ParcelId = parcelId,
            RouteState = ParcelRouteState.WaitingForRouting,
            CreatedAt = createdAt
        };

        // Act - 模拟状态转换：强制弹出
        var forceEjectedParcel = waitingParcel with
        {
            RouteState = ParcelRouteState.ForceEjected
        };

        // Assert
        Assert.Equal(ParcelRouteState.WaitingForRouting, waitingParcel.RouteState);
        Assert.Equal(ParcelRouteState.ForceEjected, forceEjectedParcel.RouteState);
    }

    [Fact]
    public void ParcelSnapshot_Can_Transition_To_Failed()
    {
        // Arrange
        var parcelId = new ParcelId(1234567890123);
        var chuteId = new ChuteId(5);
        var createdAt = DateTimeOffset.UtcNow;

        var routedParcel = new ParcelSnapshot
        {
            ParcelId = parcelId,
            TargetChuteId = chuteId,
            RouteState = ParcelRouteState.Routed,
            CreatedAt = createdAt
        };

        // Act - 模拟状态转换：分拣失败
        var failedParcel = routedParcel with
        {
            RouteState = ParcelRouteState.Failed
        };

        // Assert
        Assert.Equal(ParcelRouteState.Routed, routedParcel.RouteState);
        Assert.Equal(ParcelRouteState.Failed, failedParcel.RouteState);
    }

    [Fact]
    public void CartSnapshot_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var cartId = new CartId(10);
        var cartIndex = new CartIndex(5);
        var lastResetAt = DateTimeOffset.UtcNow;

        var cartSnapshot = new CartSnapshot
        {
            CartId = cartId,
            CartIndex = cartIndex,
            LastResetAt = lastResetAt
        };

        // Assert
        Assert.Equal(cartId, cartSnapshot.CartId);
        Assert.Equal(cartIndex, cartSnapshot.CartIndex);
        Assert.Equal(lastResetAt, cartSnapshot.LastResetAt);
        Assert.False(cartSnapshot.IsLoaded); // 默认值
        Assert.Null(cartSnapshot.CurrentParcelId);
    }

    [Fact]
    public void CartSnapshot_IsLoaded_Property_Follows_Naming_Convention()
    {
        // Arrange
        var cartSnapshot = new CartSnapshot
        {
            CartId = new CartId(1),
            CartIndex = new CartIndex(0),
            IsLoaded = true,
            LastResetAt = DateTimeOffset.UtcNow
        };

        // Assert - 验证布尔属性以Is开头
        Assert.True(cartSnapshot.IsLoaded);
    }

    [Fact]
    public void ChuteConfig_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var chuteId = new ChuteId(5);
        var maxOpenDuration = TimeSpan.FromSeconds(2);

        var chuteConfig = new ChuteConfig
        {
            ChuteId = chuteId,
            IsEnabled = true,
            IsForceEject = false,
            CartOffsetFromOrigin = 10,
            MaxOpenDuration = maxOpenDuration
        };

        // Assert
        Assert.Equal(chuteId, chuteConfig.ChuteId);
        Assert.True(chuteConfig.IsEnabled);
        Assert.False(chuteConfig.IsForceEject);
        Assert.Equal(10, chuteConfig.CartOffsetFromOrigin);
        Assert.Equal(maxOpenDuration, chuteConfig.MaxOpenDuration);
    }

    [Fact]
    public void ChuteConfig_Boolean_Properties_Follow_Naming_Convention()
    {
        // Arrange
        var chuteConfig = new ChuteConfig
        {
            ChuteId = new ChuteId(1),
            IsEnabled = true,
            IsForceEject = true,
            MaxOpenDuration = TimeSpan.FromSeconds(1)
        };

        // Assert - 验证布尔属性以Is开头
        Assert.True(chuteConfig.IsEnabled);
        Assert.True(chuteConfig.IsForceEject);
    }
}
