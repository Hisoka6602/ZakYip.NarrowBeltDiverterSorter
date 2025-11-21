using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.Sorting;

/// <summary>
/// ChuteConfigValidator 单元测试
/// </summary>
public class ChuteConfigValidatorTests
{
    private readonly ChuteConfigValidator _validator;

    public ChuteConfigValidatorTests()
    {
        _validator = new ChuteConfigValidator();
    }

    [Fact]
    public void Validate_Should_Pass_When_TotalCartCount_Is_Zero()
    {
        // Arrange
        var config = new ChuteConfig
        {
            ChuteId = new ChuteId(1),
            IsEnabled = true,
            IsForceEject = false,
            CartOffsetFromOrigin = 5,
            MaxOpenDuration = TimeSpan.FromMilliseconds(300),
            CartNumberWhenHeadAtOrigin = 0 // 可以为 0，因为总数为 0
        };

        // Act
        var result = _validator.Validate(config, totalCartCount: 0);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Validate_Should_Pass_When_CartNumberWhenHeadAtOrigin_Is_Valid()
    {
        // Arrange
        var config = new ChuteConfig
        {
            ChuteId = new ChuteId(1),
            IsEnabled = true,
            IsForceEject = false,
            CartOffsetFromOrigin = 5,
            MaxOpenDuration = TimeSpan.FromMilliseconds(300),
            CartNumberWhenHeadAtOrigin = 5
        };

        // Act
        var result = _validator.Validate(config, totalCartCount: 10);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Validate_Should_Fail_When_CartNumberWhenHeadAtOrigin_Is_Zero_And_TotalCartCount_Is_Positive()
    {
        // Arrange
        var config = new ChuteConfig
        {
            ChuteId = new ChuteId(1),
            IsEnabled = true,
            IsForceEject = false,
            CartOffsetFromOrigin = 5,
            MaxOpenDuration = TimeSpan.FromMilliseconds(300),
            CartNumberWhenHeadAtOrigin = 0
        };

        // Act
        var result = _validator.Validate(config, totalCartCount: 10);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("格口 1 的窗口小车号必须大于 0", result.ErrorMessage);
    }

    [Fact]
    public void Validate_Should_Fail_When_CartNumberWhenHeadAtOrigin_Is_Negative()
    {
        // Arrange
        var config = new ChuteConfig
        {
            ChuteId = new ChuteId(1),
            IsEnabled = true,
            IsForceEject = false,
            CartOffsetFromOrigin = 5,
            MaxOpenDuration = TimeSpan.FromMilliseconds(300),
            CartNumberWhenHeadAtOrigin = -1
        };

        // Act
        var result = _validator.Validate(config, totalCartCount: 10);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("格口 1 的窗口小车号必须大于 0", result.ErrorMessage);
    }

    [Fact]
    public void Validate_Should_Fail_When_CartNumberWhenHeadAtOrigin_Exceeds_TotalCartCount()
    {
        // Arrange
        var config = new ChuteConfig
        {
            ChuteId = new ChuteId(1),
            IsEnabled = true,
            IsForceEject = false,
            CartOffsetFromOrigin = 5,
            MaxOpenDuration = TimeSpan.FromMilliseconds(300),
            CartNumberWhenHeadAtOrigin = 11
        };

        // Act
        var result = _validator.Validate(config, totalCartCount: 10);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("格口 1 的窗口小车号必须在 1 和总小车数量 10 之间", result.ErrorMessage);
        Assert.Contains("当前值：11", result.ErrorMessage);
    }

    [Fact]
    public void Validate_Should_Pass_When_CartNumberWhenHeadAtOrigin_Equals_TotalCartCount()
    {
        // Arrange
        var config = new ChuteConfig
        {
            ChuteId = new ChuteId(1),
            IsEnabled = true,
            IsForceEject = false,
            CartOffsetFromOrigin = 5,
            MaxOpenDuration = TimeSpan.FromMilliseconds(300),
            CartNumberWhenHeadAtOrigin = 10
        };

        // Act
        var result = _validator.Validate(config, totalCartCount: 10);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Validate_Should_Fail_For_Null_Config()
    {
        // Act
        var result = _validator.Validate(null!, totalCartCount: 10);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("格口配置不能为空", result.ErrorMessage);
    }
}
