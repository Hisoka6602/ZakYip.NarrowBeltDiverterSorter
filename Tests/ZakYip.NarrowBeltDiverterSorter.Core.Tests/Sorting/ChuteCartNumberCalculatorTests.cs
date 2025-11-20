using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.Sorting;

/// <summary>
/// ChuteCartNumberCalculator 单元测试
/// </summary>
public class ChuteCartNumberCalculatorTests
{
    private readonly ChuteCartNumberCalculator _calculator;

    public ChuteCartNumberCalculatorTests()
    {
        _calculator = new ChuteCartNumberCalculator(NullLogger<ChuteCartNumberCalculator>.Instance);
    }

    [Fact]
    public void GetCartNumberAtChute_Should_Calculate_Correctly_For_Simple_Case()
    {
        // Arrange
        int totalCartCount = 10;
        int headCartNumber = 1; // 首车在原点
        int cartNumberWhenHeadAtOrigin = 5; // 格口窗口应该显示 5 号车

        // Act
        var result = _calculator.GetCartNumberAtChute(totalCartCount, headCartNumber, cartNumberWhenHeadAtOrigin);

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void GetCartNumberAtChute_Should_Handle_Wraparound()
    {
        // Arrange
        int totalCartCount = 10;
        int headCartNumber = 8; // 8号车在原点
        int cartNumberWhenHeadAtOrigin = 5; // 首车在原点时格口显示5号车

        // Act
        var result = _calculator.GetCartNumberAtChute(totalCartCount, headCartNumber, cartNumberWhenHeadAtOrigin);

        // Assert
        // 计算：(5-1 + 8-1) % 10 + 1 = (4 + 7) % 10 + 1 = 11 % 10 + 1 = 1 + 1 = 2
        Assert.Equal(2, result);
    }

    [Theory]
    [InlineData(1, 8)]  // 首车在原点时格口显示8号车，当前原点是1号车，结果应该是8
    [InlineData(2, 9)]  // 当前原点是2号车，结果应该是9
    [InlineData(3, 10)] // 当前原点是3号车，结果应该是10
    [InlineData(4, 1)]  // 当前原点是4号车，结果应该是1（环绕）
    [InlineData(5, 2)]  // 当前原点是5号车，结果应该是2
    [InlineData(6, 3)]  // 当前原点是6号车，结果应该是3
    [InlineData(7, 4)]  // 当前原点是7号车，结果应该是4
    [InlineData(8, 5)]  // 当前原点是8号车，结果应该是5
    [InlineData(9, 6)]  // 当前原点是9号车，结果应该是6
    [InlineData(10, 7)] // 当前原点是10号车，结果应该是7
    public void GetCartNumberAtChute_Should_Calculate_For_All_Head_Positions(int headCartNumber, int expectedResult)
    {
        // Arrange
        int totalCartCount = 10;
        int cartNumberWhenHeadAtOrigin = 8;

        // Act
        var result = _calculator.GetCartNumberAtChute(totalCartCount, headCartNumber, cartNumberWhenHeadAtOrigin);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(90, 1, 90)]   // 首车在原点，格口显示90
    [InlineData(90, 11, 100)] // 原点在11号车，格口显示100
    [InlineData(90, 12, 1)]   // 原点在12号车，格口显示1（环绕）
    [InlineData(90, 50, 39)]  // 原点在50号车
    public void GetCartNumberAtChute_Should_Handle_Large_Cart_Count_With_Wraparound(
        int cartNumberWhenHeadAtOrigin, 
        int headCartNumber, 
        int expectedResult)
    {
        // Arrange
        int totalCartCount = 100;

        // Act
        var result = _calculator.GetCartNumberAtChute(totalCartCount, headCartNumber, cartNumberWhenHeadAtOrigin);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(80, 1, 80)]
    [InlineData(80, 21, 100)]
    [InlineData(80, 22, 1)]
    [InlineData(80, 30, 9)]
    public void GetCartNumberAtChute_Should_Calculate_For_100_Carts(
        int cartNumberWhenHeadAtOrigin,
        int headCartNumber,
        int expectedResult)
    {
        // Arrange
        int totalCartCount = 100;

        // Act
        var result = _calculator.GetCartNumberAtChute(totalCartCount, headCartNumber, cartNumberWhenHeadAtOrigin);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void GetCartNumberAtChute_Should_Throw_For_Zero_TotalCartCount()
    {
        // Arrange
        int totalCartCount = 0;
        int headCartNumber = 1;
        int cartNumberWhenHeadAtOrigin = 1;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _calculator.GetCartNumberAtChute(totalCartCount, headCartNumber, cartNumberWhenHeadAtOrigin));
        
        Assert.Contains("总小车数量必须大于 0", exception.Message);
    }

    [Fact]
    public void GetCartNumberAtChute_Should_Throw_For_Negative_TotalCartCount()
    {
        // Arrange
        int totalCartCount = -5;
        int headCartNumber = 1;
        int cartNumberWhenHeadAtOrigin = 1;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _calculator.GetCartNumberAtChute(totalCartCount, headCartNumber, cartNumberWhenHeadAtOrigin));
        
        Assert.Contains("总小车数量必须大于 0", exception.Message);
    }

    [Fact]
    public void GetCartNumberAtChute_Should_Throw_For_Invalid_HeadCartNumber_TooLow()
    {
        // Arrange
        int totalCartCount = 10;
        int headCartNumber = 0; // Invalid
        int cartNumberWhenHeadAtOrigin = 5;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _calculator.GetCartNumberAtChute(totalCartCount, headCartNumber, cartNumberWhenHeadAtOrigin));
        
        Assert.Contains("原点处小车号必须在 1 和总小车数量", exception.Message);
    }

    [Fact]
    public void GetCartNumberAtChute_Should_Throw_For_Invalid_HeadCartNumber_TooHigh()
    {
        // Arrange
        int totalCartCount = 10;
        int headCartNumber = 11; // Invalid
        int cartNumberWhenHeadAtOrigin = 5;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _calculator.GetCartNumberAtChute(totalCartCount, headCartNumber, cartNumberWhenHeadAtOrigin));
        
        Assert.Contains("原点处小车号必须在 1 和总小车数量", exception.Message);
    }

    [Fact]
    public void GetCartNumberAtChute_Should_Throw_For_Invalid_CartNumberWhenHeadAtOrigin_TooLow()
    {
        // Arrange
        int totalCartCount = 10;
        int headCartNumber = 1;
        int cartNumberWhenHeadAtOrigin = 0; // Invalid

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _calculator.GetCartNumberAtChute(totalCartCount, headCartNumber, cartNumberWhenHeadAtOrigin));
        
        Assert.Contains("格口窗口小车号必须在 1 和总小车数量", exception.Message);
    }

    [Fact]
    public void GetCartNumberAtChute_Should_Throw_For_Invalid_CartNumberWhenHeadAtOrigin_TooHigh()
    {
        // Arrange
        int totalCartCount = 10;
        int headCartNumber = 1;
        int cartNumberWhenHeadAtOrigin = 11; // Invalid

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _calculator.GetCartNumberAtChute(totalCartCount, headCartNumber, cartNumberWhenHeadAtOrigin));
        
        Assert.Contains("格口窗口小车号必须在 1 和总小车数量", exception.Message);
    }

    [Fact]
    public void GetCartNumberAtChute_Should_Handle_Edge_Case_Single_Cart()
    {
        // Arrange
        int totalCartCount = 1;
        int headCartNumber = 1;
        int cartNumberWhenHeadAtOrigin = 1;

        // Act
        var result = _calculator.GetCartNumberAtChute(totalCartCount, headCartNumber, cartNumberWhenHeadAtOrigin);

        // Assert
        Assert.Equal(1, result); // Only one cart, always returns 1
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    [InlineData(5, 5)]
    public void GetCartNumberAtChute_Should_Work_With_Small_Cart_Counts(int totalCartCount, int expected)
    {
        // Arrange
        int headCartNumber = 1;
        int cartNumberWhenHeadAtOrigin = totalCartCount;

        // Act
        var result = _calculator.GetCartNumberAtChute(totalCartCount, headCartNumber, cartNumberWhenHeadAtOrigin);

        // Assert
        Assert.Equal(expected, result);
    }
}
