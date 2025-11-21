using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.SelfCheck;

/// <summary>
/// CartRingConfigurationManager 单元测试
/// </summary>
public class CartRingConfigurationManagerTests
{
    [Fact]
    public async Task ProcessSelfCheckResult_Should_Update_Config_In_AutoLearning_Mode()
    {
        // Arrange
        var configProvider = new TestCartRingConfigurationProvider(new CartRingConfiguration { TotalCartCount = 0 });
        var manager = new CartRingConfigurationManager(configProvider, NullLogger<CartRingConfigurationManager>.Instance);

        var selfCheckResult = new CartRingSelfCheckResult
        {
            ExpectedCartCount = 0,
            MeasuredCartCount = 10,
            ExpectedPitchMm = 500m,
            MeasuredPitchMm = 500m,
            IsCartCountMatched = false,
            IsPitchWithinTolerance = true
        };

        // Act
        var result = await manager.ProcessSelfCheckResultAsync(selfCheckResult, CancellationToken.None);

        // Assert
        Assert.True(result.ConfigurationUpdated);
        Assert.False(result.HasError);
        Assert.Equal(CartRingConfigurationMode.AutoLearning, result.Mode);
        Assert.Equal(10, result.DetectedCartCount);
        Assert.Equal(10, configProvider.Current.TotalCartCount);
    }

    [Fact]
    public async Task ProcessSelfCheckResult_Should_Not_Update_In_AutoLearning_Mode_When_No_Carts_Detected()
    {
        // Arrange
        var configProvider = new TestCartRingConfigurationProvider(new CartRingConfiguration { TotalCartCount = 0 });
        var manager = new CartRingConfigurationManager(configProvider, NullLogger<CartRingConfigurationManager>.Instance);

        var selfCheckResult = new CartRingSelfCheckResult
        {
            ExpectedCartCount = 0,
            MeasuredCartCount = 0,
            ExpectedPitchMm = 500m,
            MeasuredPitchMm = 0m,
            IsCartCountMatched = false,
            IsPitchWithinTolerance = false
        };

        // Act
        var result = await manager.ProcessSelfCheckResultAsync(selfCheckResult, CancellationToken.None);

        // Assert
        Assert.False(result.ConfigurationUpdated);
        Assert.False(result.HasError);
        Assert.Equal(CartRingConfigurationMode.AutoLearning, result.Mode);
        Assert.Equal(0, result.DetectedCartCount);
        Assert.Equal(0, configProvider.Current.TotalCartCount);
    }

    [Fact]
    public async Task ProcessSelfCheckResult_Should_Pass_Verification_When_Counts_Match()
    {
        // Arrange
        var configProvider = new TestCartRingConfigurationProvider(new CartRingConfiguration { TotalCartCount = 10 });
        var manager = new CartRingConfigurationManager(configProvider, NullLogger<CartRingConfigurationManager>.Instance);

        var selfCheckResult = new CartRingSelfCheckResult
        {
            ExpectedCartCount = 10,
            MeasuredCartCount = 10,
            ExpectedPitchMm = 500m,
            MeasuredPitchMm = 500m,
            IsCartCountMatched = true,
            IsPitchWithinTolerance = true
        };

        // Act
        var result = await manager.ProcessSelfCheckResultAsync(selfCheckResult, CancellationToken.None);

        // Assert
        Assert.False(result.ConfigurationUpdated);
        Assert.False(result.HasError);
        Assert.Equal(CartRingConfigurationMode.Verification, result.Mode);
        Assert.Equal(10, result.ExpectedCartCount);
        Assert.Equal(10, result.DetectedCartCount);
    }

    [Fact]
    public async Task ProcessSelfCheckResult_Should_Fail_Verification_When_Counts_Mismatch()
    {
        // Arrange
        var configProvider = new TestCartRingConfigurationProvider(new CartRingConfiguration { TotalCartCount = 10 });
        var manager = new CartRingConfigurationManager(configProvider, NullLogger<CartRingConfigurationManager>.Instance);

        var selfCheckResult = new CartRingSelfCheckResult
        {
            ExpectedCartCount = 10,
            MeasuredCartCount = 8,
            ExpectedPitchMm = 500m,
            MeasuredPitchMm = 500m,
            IsCartCountMatched = false,
            IsPitchWithinTolerance = true
        };

        // Act
        var result = await manager.ProcessSelfCheckResultAsync(selfCheckResult, CancellationToken.None);

        // Assert
        Assert.False(result.ConfigurationUpdated);
        Assert.True(result.HasError);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("期望：10", result.ErrorMessage);
        Assert.Contains("实际：8", result.ErrorMessage);
        Assert.Equal(CartRingConfigurationMode.Verification, result.Mode);
        Assert.Equal(10, result.ExpectedCartCount);
        Assert.Equal(8, result.DetectedCartCount);
    }

    [Fact]
    public void GetCurrentMode_Should_Return_AutoLearning_When_TotalCartCount_Is_Zero()
    {
        // Arrange
        var configProvider = new TestCartRingConfigurationProvider(new CartRingConfiguration { TotalCartCount = 0 });
        var manager = new CartRingConfigurationManager(configProvider, NullLogger<CartRingConfigurationManager>.Instance);

        // Act
        var mode = manager.GetCurrentMode();

        // Assert
        Assert.Equal(CartRingConfigurationMode.AutoLearning, mode);
    }

    [Fact]
    public void GetCurrentMode_Should_Return_AutoLearning_When_TotalCartCount_Is_Negative()
    {
        // Arrange
        var configProvider = new TestCartRingConfigurationProvider(new CartRingConfiguration { TotalCartCount = -1 });
        var manager = new CartRingConfigurationManager(configProvider, NullLogger<CartRingConfigurationManager>.Instance);

        // Act
        var mode = manager.GetCurrentMode();

        // Assert
        Assert.Equal(CartRingConfigurationMode.AutoLearning, mode);
    }

    [Fact]
    public void GetCurrentMode_Should_Return_Verification_When_TotalCartCount_Is_Positive()
    {
        // Arrange
        var configProvider = new TestCartRingConfigurationProvider(new CartRingConfiguration { TotalCartCount = 10 });
        var manager = new CartRingConfigurationManager(configProvider, NullLogger<CartRingConfigurationManager>.Instance);

        // Act
        var mode = manager.GetCurrentMode();

        // Assert
        Assert.Equal(CartRingConfigurationMode.Verification, mode);
    }

    /// <summary>
    /// 测试用的配置提供器
    /// </summary>
    private class TestCartRingConfigurationProvider : ICartRingConfigurationProvider
    {
        private CartRingConfiguration _config;

        public TestCartRingConfigurationProvider(CartRingConfiguration initialConfig)
        {
            _config = initialConfig;
        }

        public CartRingConfiguration Current => _config;

        public Task<CartRingConfiguration> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_config);
        }

        public Task UpdateAsync(CartRingConfiguration configuration, CancellationToken cancellationToken = default)
        {
            _config = configuration;
            return Task.CompletedTask;
        }
    }
}
