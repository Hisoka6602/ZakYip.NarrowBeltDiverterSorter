using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Tests.Sorting;

/// <summary>
/// CartAtChuteResolver 单元测试
/// 验证基于首车原点的格口小车号解析逻辑
/// </summary>
public class CartAtChuteResolverTests
{
    private readonly Mock<ICartPositionTracker> _mockCartPositionTracker;
    private readonly Mock<ICartRingConfigurationProvider> _mockCartRingConfigProvider;
    private readonly Mock<IChuteConfigProvider> _mockChuteConfigProvider;
    private readonly Mock<IChuteCartNumberCalculator> _mockCalculator;
    private readonly CartAtChuteResolver _resolver;

    public CartAtChuteResolverTests()
    {
        _mockCartPositionTracker = new Mock<ICartPositionTracker>();
        _mockCartRingConfigProvider = new Mock<ICartRingConfigurationProvider>();
        _mockChuteConfigProvider = new Mock<IChuteConfigProvider>();
        _mockCalculator = new Mock<IChuteCartNumberCalculator>();

        _resolver = new CartAtChuteResolver(
            _mockCartPositionTracker.Object,
            _mockCartRingConfigProvider.Object,
            _mockChuteConfigProvider.Object,
            _mockCalculator.Object,
            NullLogger<CartAtChuteResolver>.Instance);
    }

    [Fact]
    public void ResolveCurrentCartNumberForChute_Should_ThrowException_When_TotalCartCount_Is_Zero()
    {
        // Arrange
        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 0 });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.ResolveCurrentCartNumberForChute(1));
        
        Assert.Contains("小车总数量未完成学习或配置", exception.Message);
    }

    [Fact]
    public void ResolveCurrentCartNumberForChute_Should_ThrowException_When_TotalCartCount_Is_Negative()
    {
        // Arrange
        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = -5 });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.ResolveCurrentCartNumberForChute(1));
        
        Assert.Contains("小车总数量未完成学习或配置", exception.Message);
    }

    [Fact]
    public void ResolveCurrentCartNumberForChute_Should_ThrowException_When_CartPositionTracker_Not_Initialized()
    {
        // Arrange
        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 100 });
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(false);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns((CartIndex?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.ResolveCurrentCartNumberForChute(1));
        
        Assert.Contains("当前首车状态未就绪", exception.Message);
    }

    [Fact]
    public void ResolveCurrentCartNumberForChute_Should_ThrowException_When_CurrentOriginCartIndex_Is_Null()
    {
        // Arrange
        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 100 });
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(true);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns((CartIndex?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.ResolveCurrentCartNumberForChute(1));
        
        Assert.Contains("当前首车状态未就绪", exception.Message);
    }

    [Fact]
    public void ResolveCurrentCartNumberForChute_Should_ThrowException_When_ChuteConfig_Not_Found()
    {
        // Arrange
        var chuteId = 1L;
        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 100 });
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(true);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns(new CartIndex(0));
        _mockChuteConfigProvider.Setup(x => x.GetConfig(new ChuteId(chuteId)))
            .Returns((ChuteConfig?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.ResolveCurrentCartNumberForChute(chuteId));
        
        Assert.Contains($"格口 {chuteId} 配置不存在", exception.Message);
    }

    [Fact]
    public void ResolveCurrentCartNumberForChute_Should_ThrowException_When_CartNumberWhenHeadAtOrigin_Is_Zero()
    {
        // Arrange
        var chuteId = 1L;
        var chuteConfig = new ChuteConfig
        {
            ChuteId = new ChuteId(chuteId),
            IsEnabled = true,
            CartNumberWhenHeadAtOrigin = 0,
            MaxOpenDuration = TimeSpan.FromSeconds(5)
        };

        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 100 });
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(true);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns(new CartIndex(0));
        _mockChuteConfigProvider.Setup(x => x.GetConfig(new ChuteId(chuteId)))
            .Returns(chuteConfig);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.ResolveCurrentCartNumberForChute(chuteId));
        
        Assert.Contains("CartNumberWhenHeadAtOrigin", exception.Message);
        Assert.Contains("超出", exception.Message);
    }

    [Fact]
    public void ResolveCurrentCartNumberForChute_Should_ThrowException_When_CartNumberWhenHeadAtOrigin_Exceeds_TotalCartCount()
    {
        // Arrange
        var chuteId = 1L;
        var chuteConfig = new ChuteConfig
        {
            ChuteId = new ChuteId(chuteId),
            IsEnabled = true,
            CartNumberWhenHeadAtOrigin = 101, // Exceeds TotalCartCount
            MaxOpenDuration = TimeSpan.FromSeconds(5)
        };

        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 100 });
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(true);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns(new CartIndex(0));
        _mockChuteConfigProvider.Setup(x => x.GetConfig(new ChuteId(chuteId)))
            .Returns(chuteConfig);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.ResolveCurrentCartNumberForChute(chuteId));
        
        Assert.Contains("CartNumberWhenHeadAtOrigin", exception.Message);
        Assert.Contains("超出", exception.Message);
    }

    [Theory]
    [InlineData(1, 90, 90)]  // 首车=1时，格口1应显示90号车
    [InlineData(1, 80, 80)]  // 首车=1时，格口3应显示80号车
    public void ResolveCurrentCartNumberForChute_Should_Return_Correct_CartNumber_When_HeadCart_Is_1(
        long chuteId, int cartNumberWhenHeadAtOrigin, int expectedCartNumber)
    {
        // Arrange
        var chuteConfig = new ChuteConfig
        {
            ChuteId = new ChuteId(chuteId),
            IsEnabled = true,
            CartNumberWhenHeadAtOrigin = cartNumberWhenHeadAtOrigin,
            MaxOpenDuration = TimeSpan.FromSeconds(5)
        };

        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 100 });
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(true);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns(new CartIndex(0)); // 0-based = 1
        _mockChuteConfigProvider.Setup(x => x.GetConfig(new ChuteId(chuteId)))
            .Returns(chuteConfig);
        _mockCalculator.Setup(x => x.GetCartNumberAtChute(100, 1, cartNumberWhenHeadAtOrigin))
            .Returns(expectedCartNumber);

        // Act
        var result = _resolver.ResolveCurrentCartNumberForChute(chuteId);

        // Assert
        Assert.Equal(expectedCartNumber, result);
        _mockCalculator.Verify(x => x.GetCartNumberAtChute(100, 1, cartNumberWhenHeadAtOrigin), Times.Once);
    }

    [Theory]
    [InlineData(1, 90, 95)]  // 首车=5时，格口1应显示95号车
    [InlineData(3, 80, 85)]  // 首车=5时，格口3应显示85号车
    public void ResolveCurrentCartNumberForChute_Should_Return_Correct_CartNumber_When_HeadCart_Is_5(
        long chuteId, int cartNumberWhenHeadAtOrigin, int expectedCartNumber)
    {
        // Arrange
        var chuteConfig = new ChuteConfig
        {
            ChuteId = new ChuteId(chuteId),
            IsEnabled = true,
            CartNumberWhenHeadAtOrigin = cartNumberWhenHeadAtOrigin,
            MaxOpenDuration = TimeSpan.FromSeconds(5)
        };

        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 100 });
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(true);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns(new CartIndex(4)); // 0-based = 5
        _mockChuteConfigProvider.Setup(x => x.GetConfig(new ChuteId(chuteId)))
            .Returns(chuteConfig);
        _mockCalculator.Setup(x => x.GetCartNumberAtChute(100, 5, cartNumberWhenHeadAtOrigin))
            .Returns(expectedCartNumber);

        // Act
        var result = _resolver.ResolveCurrentCartNumberForChute(chuteId);

        // Assert
        Assert.Equal(expectedCartNumber, result);
        _mockCalculator.Verify(x => x.GetCartNumberAtChute(100, 5, cartNumberWhenHeadAtOrigin), Times.Once);
    }

    [Fact]
    public void GetCurrentHeadCartNumber_Should_ThrowException_When_Not_Initialized()
    {
        // Arrange
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(false);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns((CartIndex?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.GetCurrentHeadCartNumber());
        
        Assert.Contains("当前首车状态未就绪", exception.Message);
    }

    [Fact]
    public void GetCurrentHeadCartNumber_Should_ThrowException_When_CurrentOriginCartIndex_Is_Null()
    {
        // Arrange
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(true);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns((CartIndex?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.GetCurrentHeadCartNumber());
        
        Assert.Contains("当前首车状态未就绪", exception.Message);
    }

    [Theory]
    [InlineData(0, 1)]   // 0-based index 0 = cart number 1
    [InlineData(4, 5)]   // 0-based index 4 = cart number 5
    [InlineData(99, 100)] // 0-based index 99 = cart number 100
    public void GetCurrentHeadCartNumber_Should_Return_Correct_CartNumber(int zeroBasedIndex, int expectedCartNumber)
    {
        // Arrange
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(true);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns(new CartIndex(zeroBasedIndex));

        // Act
        var result = _resolver.GetCurrentHeadCartNumber();

        // Assert
        Assert.Equal(expectedCartNumber, result);
    }

    #region CaptureCartBindingSnapshot Tests

    [Fact]
    public void CaptureCartBindingSnapshot_Should_Return_Valid_Snapshot()
    {
        // Arrange
        var chuteId = 1L;
        var chuteConfig = new ChuteConfig
        {
            ChuteId = new ChuteId(chuteId),
            IsEnabled = true,
            CartNumberWhenHeadAtOrigin = 90,
            MaxOpenDuration = TimeSpan.FromSeconds(5)
        };

        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 100 });
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(true);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns(new CartIndex(4)); // 0-based = 5
        _mockChuteConfigProvider.Setup(x => x.GetConfig(new ChuteId(chuteId)))
            .Returns(chuteConfig);

        var beforeCapture = DateTimeOffset.UtcNow;

        // Act
        var snapshot = _resolver.CaptureCartBindingSnapshot(chuteId);

        var afterCapture = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal(100, snapshot.TotalCartCount);
        Assert.Equal(5, snapshot.HeadCartNumber);
        Assert.Equal(chuteId, snapshot.ChuteId);
        Assert.Equal(90, snapshot.CartNumberWhenHeadAtOrigin);
        Assert.InRange(snapshot.CapturedAt, beforeCapture, afterCapture);
    }

    [Fact]
    public void CaptureCartBindingSnapshot_Should_ThrowException_When_TotalCartCount_Is_Zero()
    {
        // Arrange
        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 0 });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.CaptureCartBindingSnapshot(1));

        Assert.Contains("小车总数量未完成学习或配置", exception.Message);
    }

    [Fact]
    public void CaptureCartBindingSnapshot_Should_ThrowException_When_CartPositionTracker_Not_Initialized()
    {
        // Arrange
        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 100 });
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(false);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.CaptureCartBindingSnapshot(1));

        Assert.Contains("当前首车状态未就绪", exception.Message);
    }

    [Fact]
    public void CaptureCartBindingSnapshot_Should_ThrowException_When_HeadCartNumber_Is_Out_Of_Range()
    {
        // Arrange
        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 100 });
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(true);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns(new CartIndex(100)); // 0-based = 101, out of range

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.CaptureCartBindingSnapshot(1));

        Assert.Contains("首车编号", exception.Message);
        Assert.Contains("超出有效范围", exception.Message);
    }

    [Fact]
    public void CaptureCartBindingSnapshot_Should_ThrowException_When_ChuteConfig_Not_Found()
    {
        // Arrange
        var chuteId = 1L;
        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 100 });
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(true);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns(new CartIndex(0));
        _mockChuteConfigProvider.Setup(x => x.GetConfig(new ChuteId(chuteId)))
            .Returns((ChuteConfig?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.CaptureCartBindingSnapshot(chuteId));

        Assert.Contains($"格口 {chuteId} 配置不存在", exception.Message);
    }

    [Fact]
    public void CaptureCartBindingSnapshot_Should_ThrowException_When_CartNumberWhenHeadAtOrigin_Is_Invalid()
    {
        // Arrange
        var chuteId = 1L;
        var chuteConfig = new ChuteConfig
        {
            ChuteId = new ChuteId(chuteId),
            IsEnabled = true,
            CartNumberWhenHeadAtOrigin = 0, // Invalid
            MaxOpenDuration = TimeSpan.FromSeconds(5)
        };

        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 100 });
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(true);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns(new CartIndex(0));
        _mockChuteConfigProvider.Setup(x => x.GetConfig(new ChuteId(chuteId)))
            .Returns(chuteConfig);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.CaptureCartBindingSnapshot(chuteId));

        Assert.Contains("CartNumberWhenHeadAtOrigin", exception.Message);
        Assert.Contains("超出", exception.Message);
    }

    [Theory]
    [InlineData(1L, 90, 0, 1)]  // Chute 1, cart 90, head at position 0 (cart 1)
    [InlineData(3L, 80, 4, 5)]  // Chute 3, cart 80, head at position 4 (cart 5)
    [InlineData(5L, 50, 10, 11)] // Chute 5, cart 50, head at position 10 (cart 11)
    public void CaptureCartBindingSnapshot_Should_Capture_Consistent_State(
        long chuteId, int cartNumberWhenHeadAtOrigin, int headPosition, int expectedHeadCartNumber)
    {
        // Arrange
        var chuteConfig = new ChuteConfig
        {
            ChuteId = new ChuteId(chuteId),
            IsEnabled = true,
            CartNumberWhenHeadAtOrigin = cartNumberWhenHeadAtOrigin,
            MaxOpenDuration = TimeSpan.FromSeconds(5)
        };

        _mockCartRingConfigProvider.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = 100 });
        _mockCartPositionTracker.Setup(x => x.IsInitialized).Returns(true);
        _mockCartPositionTracker.Setup(x => x.CurrentOriginCartIndex).Returns(new CartIndex(headPosition));
        _mockChuteConfigProvider.Setup(x => x.GetConfig(new ChuteId(chuteId)))
            .Returns(chuteConfig);

        // Act
        var snapshot = _resolver.CaptureCartBindingSnapshot(chuteId);

        // Assert - 验证快照的一致性
        Assert.Equal(100, snapshot.TotalCartCount);
        Assert.Equal(expectedHeadCartNumber, snapshot.HeadCartNumber);
        Assert.Equal(chuteId, snapshot.ChuteId);
        Assert.Equal(cartNumberWhenHeadAtOrigin, snapshot.CartNumberWhenHeadAtOrigin);
    }

    #endregion
}
