using Microsoft.Extensions.Logging;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

/// <summary>
/// 格口小车号计算服务实现
/// 提供基于环形数组的格口窗口小车号计算逻辑
/// </summary>
public sealed class ChuteCartNumberCalculator : IChuteCartNumberCalculator
{
    private readonly ILogger<ChuteCartNumberCalculator> _logger;

    public ChuteCartNumberCalculator(ILogger<ChuteCartNumberCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public int GetCartNumberAtChute(
        int totalCartCount,
        int headCartNumber,
        int cartNumberWhenHeadAtOrigin)
    {
        // 参数验证
        if (totalCartCount <= 0)
        {
            var errorMsg = $"总小车数量必须大于 0，当前值：{totalCartCount}";
            _logger.LogError(errorMsg);
            throw new ArgumentException(errorMsg, nameof(totalCartCount));
        }

        if (headCartNumber < 1 || headCartNumber > totalCartCount)
        {
            var errorMsg = $"原点处小车号必须在 1 和总小车数量 {totalCartCount} 之间，当前值：{headCartNumber}";
            _logger.LogError(errorMsg);
            throw new ArgumentException(errorMsg, nameof(headCartNumber));
        }

        if (cartNumberWhenHeadAtOrigin < 1 || cartNumberWhenHeadAtOrigin > totalCartCount)
        {
            var errorMsg = $"格口窗口小车号必须在 1 和总小车数量 {totalCartCount} 之间，当前值：{cartNumberWhenHeadAtOrigin}";
            _logger.LogError(errorMsg);
            throw new ArgumentException(errorMsg, nameof(cartNumberWhenHeadAtOrigin));
        }

        // 环形数组计算
        // 转换为 0 基索引
        var zeroBasedHead = headCartNumber - 1;
        var zeroBasedChuteBase = cartNumberWhenHeadAtOrigin - 1;

        // 计算偏移并处理环绕
        var result = (zeroBasedChuteBase + zeroBasedHead) % totalCartCount;

        // 转换回 1 基索引
        return result + 1;
    }
}
