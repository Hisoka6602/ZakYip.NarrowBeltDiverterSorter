using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Shared.Kernel;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Tests.Sorting;

/// <summary>
/// 集成测试：验证首车原点 → 格口小车号 → 包裹绑定的完整链路
/// 涵盖问题陈述中的所有场景
/// </summary>
public class CartNumberBindingIntegrationTests
{
    /// <summary>
    /// 场景：TotalCartCount=100, Chute1=90, Chute3=80, 首车=1
    /// 预期：Chute1当前小车号=90, Chute3当前小车号=80
    /// </summary>
    [Fact]
    public void Should_Calculate_Correct_CartNumber_When_HeadCart_Is_1()
    {
        // Arrange
        var (resolver, _) = SetupTestEnvironment(
            totalCartCount: 100,
            headCartIndex: 0, // 0-based = cart number 1
            chuteConfigs: new[]
            {
                (chuteId: 1L, cartNumberWhenHeadAtOrigin: 90),
                (chuteId: 3L, cartNumberWhenHeadAtOrigin: 80)
            });

        // Act
        var chute1CartNumber = resolver.ResolveCurrentCartNumberForChute(1);
        var chute3CartNumber = resolver.ResolveCurrentCartNumberForChute(3);

        // Assert
        Assert.Equal(90, chute1CartNumber);
        Assert.Equal(80, chute3CartNumber);
    }

    /// <summary>
    /// 场景：TotalCartCount=100, Chute1=90, Chute3=80, 首车=5
    /// 预期：Chute1当前小车号=94, Chute3当前小车号=84
    /// </summary>
    [Fact]
    public void Should_Calculate_Correct_CartNumber_When_HeadCart_Is_5()
    {
        // Arrange
        var (resolver, _) = SetupTestEnvironment(
            totalCartCount: 100,
            headCartIndex: 4, // 0-based = cart number 5
            chuteConfigs: new[]
            {
                (chuteId: 1L, cartNumberWhenHeadAtOrigin: 90),
                (chuteId: 3L, cartNumberWhenHeadAtOrigin: 80)
            });

        // Act
        var chute1CartNumber = resolver.ResolveCurrentCartNumberForChute(1);
        var chute3CartNumber = resolver.ResolveCurrentCartNumberForChute(3);

        // Assert
        Assert.Equal(94, chute1CartNumber);
        Assert.Equal(84, chute3CartNumber);
    }

    /// <summary>
    /// 场景：验证包裹创建时绑定小车号的完整流程
    /// 首车=5, Chute1配置=90 → 包裹应绑定到94号车
    /// </summary>
    [Fact]
    public void Should_Bind_Correct_CartNumber_When_Creating_Package_At_Chute1()
    {
        // Arrange
        var (resolver, parcelService) = SetupTestEnvironment(
            totalCartCount: 100,
            headCartIndex: 4, // 0-based = cart number 5
            chuteConfigs: new[]
            {
                (chuteId: 1L, cartNumberWhenHeadAtOrigin: 90)
            });

        var parcelId = new ParcelId(12345);
        var barcode = "TEST-BARCODE-001";
        var createdAt = DateTimeOffset.UtcNow;

        // Act - 解析格口当前小车号
        var cartNumber = resolver.ResolveCurrentCartNumberForChute(1);

        // Act - 创建包裹并绑定小车号
        var parcel = parcelService.CreateParcelWithCartNumber(parcelId, barcode, createdAt, cartNumber);

        // Assert
        Assert.Equal(94, cartNumber);
        Assert.Equal(94, parcel.BoundCartNumber);
        Assert.Equal(parcelId, parcel.ParcelId);
    }

    /// <summary>
    /// 场景：验证包裹创建时绑定小车号的完整流程
    /// 首车=5, Chute3配置=80 → 包裹应绑定到84号车
    /// </summary>
    [Fact]
    public void Should_Bind_Correct_CartNumber_When_Creating_Package_At_Chute3()
    {
        // Arrange
        var (resolver, parcelService) = SetupTestEnvironment(
            totalCartCount: 100,
            headCartIndex: 4, // 0-based = cart number 5
            chuteConfigs: new[]
            {
                (chuteId: 3L, cartNumberWhenHeadAtOrigin: 80)
            });

        var parcelId = new ParcelId(67890);
        var barcode = "TEST-BARCODE-002";
        var createdAt = DateTimeOffset.UtcNow;

        // Act - 解析格口当前小车号
        var cartNumber = resolver.ResolveCurrentCartNumberForChute(3);

        // Act - 创建包裹并绑定小车号
        var parcel = parcelService.CreateParcelWithCartNumber(parcelId, barcode, createdAt, cartNumber);

        // Assert
        Assert.Equal(84, cartNumber);
        Assert.Equal(84, parcel.BoundCartNumber);
        Assert.Equal(parcelId, parcel.ParcelId);
    }

    /// <summary>
    /// 场景：验证配置热更新后，格口小车号和包裹绑定同步变化
    /// Chute1配置从90改为70，首车=1 → 期望小车号变为70
    /// </summary>
    [Fact]
    public void Should_Reflect_Config_Update_In_Cart_Number_Resolution()
    {
        // Arrange
        var cartRingConfig = new CartRingConfiguration { TotalCartCount = 100 };
        var cartRingConfigProvider = new TestCartRingConfigurationProvider(cartRingConfig);
        var chuteConfigProvider = new ChuteConfigProvider();
        var cartPositionTracker = new TestCartPositionTracker(new CartIndex(0)); // cart 1
        var calculator = new ChuteCartNumberCalculator(NullLogger<ChuteCartNumberCalculator>.Instance);

        var resolver = new CartAtChuteResolver(
            cartPositionTracker,
            cartRingConfigProvider,
            chuteConfigProvider,
            calculator,
            NullLogger<CartAtChuteResolver>.Instance);

        // 初始配置：Chute1=90
        chuteConfigProvider.AddOrUpdate(new ChuteConfig
        {
            ChuteId = new ChuteId(1),
            IsEnabled = true,
            CartNumberWhenHeadAtOrigin = 90,
            MaxOpenDuration = TimeSpan.FromSeconds(5)
        });

        // Act - 初始解析
        var initialCartNumber = resolver.ResolveCurrentCartNumberForChute(1);

        // 更新配置：Chute1=70
        chuteConfigProvider.AddOrUpdate(new ChuteConfig
        {
            ChuteId = new ChuteId(1),
            IsEnabled = true,
            CartNumberWhenHeadAtOrigin = 70,
            MaxOpenDuration = TimeSpan.FromSeconds(5)
        });

        // Act - 更新后再次解析
        var updatedCartNumber = resolver.ResolveCurrentCartNumberForChute(1);

        // Assert
        Assert.Equal(90, initialCartNumber);
        Assert.Equal(70, updatedCartNumber);
    }

    /// <summary>
    /// 场景：验证多个格口在同一时刻的小车号解析一致性
    /// </summary>
    [Theory]
    [InlineData(0, 90, 80)] // 首车=1
    [InlineData(4, 94, 84)] // 首车=5
    [InlineData(10, 100, 90)] // 首车=11
    [InlineData(20, 10, 100)] // 首车=21 (环绕)
    public void Should_Maintain_Consistency_Across_Multiple_Chutes(
        int headCartIndex, int expectedChute1, int expectedChute3)
    {
        // Arrange
        var (resolver, _) = SetupTestEnvironment(
            totalCartCount: 100,
            headCartIndex: headCartIndex,
            chuteConfigs: new[]
            {
                (chuteId: 1L, cartNumberWhenHeadAtOrigin: 90),
                (chuteId: 3L, cartNumberWhenHeadAtOrigin: 80)
            });

        // Act
        var chute1CartNumber = resolver.ResolveCurrentCartNumberForChute(1);
        var chute3CartNumber = resolver.ResolveCurrentCartNumberForChute(3);

        // Assert
        Assert.Equal(expectedChute1, chute1CartNumber);
        Assert.Equal(expectedChute3, chute3CartNumber);
    }

    #region Helper Methods

    private (ICartAtChuteResolver resolver, IParcelLifecycleService parcelService) SetupTestEnvironment(
        int totalCartCount,
        int headCartIndex,
        (long chuteId, int cartNumberWhenHeadAtOrigin)[] chuteConfigs)
    {
        var cartRingConfig = new CartRingConfiguration { TotalCartCount = totalCartCount };
        var cartRingConfigProvider = new TestCartRingConfigurationProvider(cartRingConfig);
        var chuteConfigProvider = new ChuteConfigProvider();
        var cartPositionTracker = new TestCartPositionTracker(new CartIndex(headCartIndex));
        var calculator = new ChuteCartNumberCalculator(NullLogger<ChuteCartNumberCalculator>.Instance);

        // Setup chute configurations
        foreach (var (chuteId, cartNumberWhenHeadAtOrigin) in chuteConfigs)
        {
            chuteConfigProvider.AddOrUpdate(new ChuteConfig
            {
                ChuteId = new ChuteId(chuteId),
                IsEnabled = true,
                CartNumberWhenHeadAtOrigin = cartNumberWhenHeadAtOrigin,
                MaxOpenDuration = TimeSpan.FromSeconds(5)
            });
        }

        var resolver = new CartAtChuteResolver(
            cartPositionTracker,
            cartRingConfigProvider,
            chuteConfigProvider,
            calculator,
            NullLogger<CartAtChuteResolver>.Instance);

        // Create a simple system run state service for parcel creation
        var systemRunStateService = new TestSystemRunStateService();
        var parcelService = new ParcelLifecycleService(systemRunStateService);

        return (resolver, parcelService);
    }

    #endregion

    #region Test Doubles

    private class TestCartRingConfigurationProvider : ICartRingConfigurationProvider
    {
        private CartRingConfiguration _config;

        public TestCartRingConfigurationProvider(CartRingConfiguration config)
        {
            _config = config;
        }

        public CartRingConfiguration Current => _config;

        public Task UpdateAsync(CartRingConfiguration configuration, CancellationToken cancellationToken = default)
        {
            _config = configuration;
            return Task.CompletedTask;
        }

        public Task<CartRingConfiguration> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_config);
        }
    }

    private class TestCartPositionTracker : ICartPositionTracker
    {
        private readonly CartIndex? _currentOriginCartIndex;

        public TestCartPositionTracker(CartIndex? currentOriginCartIndex)
        {
            _currentOriginCartIndex = currentOriginCartIndex;
        }

        public bool IsInitialized => _currentOriginCartIndex.HasValue;
        public bool IsRingReady => _currentOriginCartIndex.HasValue;
        public CartIndex? CurrentOriginCartIndex => _currentOriginCartIndex;

        public void OnCartPassedOrigin(DateTimeOffset timestamp)
        {
            throw new NotImplementedException();
        }

        public CartIndex? CalculateCartIndexAtOffset(int offset, RingLength ringLength)
        {
            throw new NotImplementedException();
        }
    }

    private class TestSystemRunStateService : ISystemRunStateService
    {
        public SystemRunState Current => SystemRunState.Running;

        public OperationResult TryHandleStart()
        {
            return OperationResult.Success();
        }

        public OperationResult TryHandleStop()
        {
            return OperationResult.Success();
        }

        public OperationResult TryHandleEmergencyStop()
        {
            return OperationResult.Success();
        }

        public OperationResult TryHandleEmergencyReset()
        {
            return OperationResult.Success();
        }

        public OperationResult ValidateCanCreateParcel()
        {
            return OperationResult.Success();
        }

        public void ForceToFaultState(string reason)
        {
            // No-op for test
        }
    }

    #endregion
}
