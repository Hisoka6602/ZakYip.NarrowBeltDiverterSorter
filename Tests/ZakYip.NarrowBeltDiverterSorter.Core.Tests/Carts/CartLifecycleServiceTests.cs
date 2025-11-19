using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.Carts;

/// <summary>
/// 小车生命周期服务测试
/// </summary>
public class CartLifecycleServiceTests
{
    [Fact]
    public void InitializeCart_ShouldCreateCartWithCorrectState()
    {
        // Arrange
        var service = new CartLifecycleService();
        var cartId = new CartId(1);
        var cartIndex = new CartIndex(0);
        var initialTime = DateTimeOffset.UtcNow;

        // Act
        service.InitializeCart(cartId, cartIndex, initialTime);
        var cart = service.Get(cartId);

        // Assert
        Assert.NotNull(cart);
        Assert.Equal(cartId, cart.CartId);
        Assert.Equal(cartIndex, cart.CartIndex);
        Assert.False(cart.IsLoaded);
        Assert.Null(cart.CurrentParcelId);
        Assert.Equal(initialTime, cart.LastResetAt);
    }

    [Fact]
    public void LoadParcel_ShouldUpdateCartState()
    {
        // Arrange
        var service = new CartLifecycleService();
        var cartId = new CartId(1);
        var cartIndex = new CartIndex(0);
        var parcelId = new ParcelId(100);
        service.InitializeCart(cartId, cartIndex, DateTimeOffset.UtcNow);

        // Act
        service.LoadParcel(cartId, parcelId);
        var cart = service.Get(cartId);

        // Assert
        Assert.NotNull(cart);
        Assert.True(cart.IsLoaded);
        Assert.Equal(parcelId, cart.CurrentParcelId);
    }

    [Fact]
    public void UnloadCart_ShouldClearCartState()
    {
        // Arrange
        var service = new CartLifecycleService();
        var cartId = new CartId(1);
        var cartIndex = new CartIndex(0);
        var parcelId = new ParcelId(100);
        var resetTime = DateTimeOffset.UtcNow.AddSeconds(10);
        
        service.InitializeCart(cartId, cartIndex, DateTimeOffset.UtcNow);
        service.LoadParcel(cartId, parcelId);

        // Act
        service.UnloadCart(cartId, resetTime);
        var cart = service.Get(cartId);

        // Assert
        Assert.NotNull(cart);
        Assert.False(cart.IsLoaded);
        Assert.Null(cart.CurrentParcelId);
        Assert.Equal(resetTime, cart.LastResetAt);
    }

    [Fact]
    public void GetAll_ShouldReturnAllCarts()
    {
        // Arrange
        var service = new CartLifecycleService();
        var initialTime = DateTimeOffset.UtcNow;
        
        service.InitializeCart(new CartId(1), new CartIndex(0), initialTime);
        service.InitializeCart(new CartId(2), new CartIndex(1), initialTime);
        service.InitializeCart(new CartId(3), new CartIndex(2), initialTime);

        // Act
        var allCarts = service.GetAll();

        // Assert
        Assert.Equal(3, allCarts.Count);
    }

    [Fact]
    public void LoadParcel_OnNonExistentCart_ShouldThrowException()
    {
        // Arrange
        var service = new CartLifecycleService();
        var cartId = new CartId(1);
        var parcelId = new ParcelId(100);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.LoadParcel(cartId, parcelId));
    }
}
