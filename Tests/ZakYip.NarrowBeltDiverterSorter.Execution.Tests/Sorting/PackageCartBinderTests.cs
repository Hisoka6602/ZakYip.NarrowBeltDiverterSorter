using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Tests.Sorting;

/// <summary>
/// PackageCartBinder 单元测试
/// 验证包裹绑定小车号的逻辑
/// </summary>
public class PackageCartBinderTests
{
    private readonly Mock<ICartAtChuteResolver> _mockResolver;
    private readonly PackageCartBinder _binder;

    public PackageCartBinderTests()
    {
        _mockResolver = new Mock<ICartAtChuteResolver>();
        _binder = new PackageCartBinder(
            _mockResolver.Object,
            NullLogger<PackageCartBinder>.Instance);
    }

    [Fact]
    public void BindCartForNewPackage_Should_Return_CartNumber_From_Resolver()
    {
        // Arrange
        const long packageId = 12345L;
        const long chuteId = 1L;
        const int expectedCartNumber = 95;

        _mockResolver.Setup(x => x.ResolveCurrentCartNumberForChute(chuteId))
            .Returns(expectedCartNumber);

        // Act
        var result = _binder.BindCartForNewPackage(packageId, chuteId);

        // Assert
        Assert.Equal(expectedCartNumber, result);
        _mockResolver.Verify(x => x.ResolveCurrentCartNumberForChute(chuteId), Times.Once);
    }

    [Fact]
    public void BindCartForNewPackage_Should_ThrowException_When_Resolver_Throws_InvalidOperationException()
    {
        // Arrange
        const long packageId = 12345L;
        const long chuteId = 1L;
        var resolverException = new InvalidOperationException("小车总数量未完成学习或配置");

        _mockResolver.Setup(x => x.ResolveCurrentCartNumberForChute(chuteId))
            .Throws(resolverException);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _binder.BindCartForNewPackage(packageId, chuteId));

        Assert.Contains("当前小车状态未准备好，暂不允许创建包裹", exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.Equal(resolverException, exception.InnerException);
    }

    [Fact]
    public void BindCartForNewPackage_Should_ThrowException_When_Resolver_Throws_UnexpectedException()
    {
        // Arrange
        const long packageId = 12345L;
        const long chuteId = 1L;
        var unexpectedException = new Exception("Unexpected error");

        _mockResolver.Setup(x => x.ResolveCurrentCartNumberForChute(chuteId))
            .Throws(unexpectedException);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _binder.BindCartForNewPackage(packageId, chuteId));

        Assert.Contains("绑定包裹小车号时发生意外错误", exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.Equal(unexpectedException, exception.InnerException);
    }

    [Theory]
    [InlineData(1L, 1L, 90)]
    [InlineData(2L, 1L, 90)]
    [InlineData(3L, 3L, 85)]
    [InlineData(100L, 5L, 50)]
    public void BindCartForNewPackage_Should_Work_With_Various_PackageIds_And_Chutes(
        long packageId, long chuteId, int expectedCartNumber)
    {
        // Arrange
        _mockResolver.Setup(x => x.ResolveCurrentCartNumberForChute(chuteId))
            .Returns(expectedCartNumber);

        // Act
        var result = _binder.BindCartForNewPackage(packageId, chuteId);

        // Assert
        Assert.Equal(expectedCartNumber, result);
    }

    [Fact]
    public void BindCartForNewPackage_Should_Call_Resolver_For_Each_Invocation()
    {
        // Arrange
        const long packageId = 12345L;
        const long chuteId = 1L;
        
        _mockResolver.SetupSequence(x => x.ResolveCurrentCartNumberForChute(chuteId))
            .Returns(90)
            .Returns(91)
            .Returns(92);

        // Act
        var result1 = _binder.BindCartForNewPackage(packageId, chuteId);
        var result2 = _binder.BindCartForNewPackage(packageId + 1, chuteId);
        var result3 = _binder.BindCartForNewPackage(packageId + 2, chuteId);

        // Assert - 每次调用都应该获取当前的小车号
        Assert.Equal(90, result1);
        Assert.Equal(91, result2);
        Assert.Equal(92, result3);
        _mockResolver.Verify(x => x.ResolveCurrentCartNumberForChute(chuteId), Times.Exactly(3));
    }

    [Fact]
    public void BindCartForNewPackage_Should_Not_Modify_State()
    {
        // Arrange
        const long packageId = 12345L;
        const long chuteId = 1L;
        const int cartNumber = 95;

        _mockResolver.Setup(x => x.ResolveCurrentCartNumberForChute(chuteId))
            .Returns(cartNumber);

        // Act - 多次调用相同参数
        var result1 = _binder.BindCartForNewPackage(packageId, chuteId);
        var result2 = _binder.BindCartForNewPackage(packageId, chuteId);

        // Assert - 结果应该一致（假设 resolver 返回相同值）
        Assert.Equal(cartNumber, result1);
        Assert.Equal(cartNumber, result2);
    }
}
