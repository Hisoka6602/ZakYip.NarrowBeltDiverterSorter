using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

/// <summary>
/// 格口小车号解析器实现
/// 基于当前首车编号和配置，解析指定格口当前窗口的小车编号
/// </summary>
public sealed class CartAtChuteResolver : ICartAtChuteResolver
{
    private readonly ICartPositionTracker _cartPositionTracker;
    private readonly ICartRingConfigurationProvider _cartRingConfigProvider;
    private readonly IChuteConfigProvider _chuteConfigProvider;
    private readonly IChuteCartNumberCalculator _calculator;
    private readonly ILogger<CartAtChuteResolver> _logger;

    public CartAtChuteResolver(
        ICartPositionTracker cartPositionTracker,
        ICartRingConfigurationProvider cartRingConfigProvider,
        IChuteConfigProvider chuteConfigProvider,
        IChuteCartNumberCalculator calculator,
        ILogger<CartAtChuteResolver> logger)
    {
        _cartPositionTracker = cartPositionTracker ?? throw new ArgumentNullException(nameof(cartPositionTracker));
        _cartRingConfigProvider = cartRingConfigProvider ?? throw new ArgumentNullException(nameof(cartRingConfigProvider));
        _chuteConfigProvider = chuteConfigProvider ?? throw new ArgumentNullException(nameof(chuteConfigProvider));
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public int ResolveCurrentCartNumberForChute(long chuteId)
    {
        // 1. 检查小车总数是否已配置
        var cartRingConfig = _cartRingConfigProvider.Current;
        if (cartRingConfig.TotalCartCount <= 0)
        {
            var errorMsg = "小车总数量未完成学习或配置，无法解析格口小车号";
            _logger.LogError("{ErrorMessage}。TotalCartCount={TotalCartCount}", errorMsg, cartRingConfig.TotalCartCount);
            throw new InvalidOperationException(errorMsg);
        }

        // 2. 检查当前首车编号是否已知
        if (!_cartPositionTracker.IsInitialized || _cartPositionTracker.CurrentOriginCartIndex == null)
        {
            var errorMsg = "当前首车状态未就绪，无法解析格口小车号";
            _logger.LogError("{ErrorMessage}。IsInitialized={IsInitialized}, CurrentOriginCartIndex={CurrentOriginCartIndex}",
                errorMsg, _cartPositionTracker.IsInitialized, _cartPositionTracker.CurrentOriginCartIndex);
            throw new InvalidOperationException(errorMsg);
        }

        // 3. 获取格口配置
        var chuteConfig = _chuteConfigProvider.GetConfig(new ChuteId(chuteId));
        if (chuteConfig == null)
        {
            var errorMsg = $"格口 {chuteId} 配置不存在";
            _logger.LogError("{ErrorMessage}", errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        // 4. 验证 CartNumberWhenHeadAtOrigin 配置
        if (chuteConfig.CartNumberWhenHeadAtOrigin <= 0 || 
            chuteConfig.CartNumberWhenHeadAtOrigin > cartRingConfig.TotalCartCount)
        {
            var errorMsg = $"格口 {chuteId} 缺少 CartNumberWhenHeadAtOrigin 配置或超出范围。" +
                          $"当前值：{chuteConfig.CartNumberWhenHeadAtOrigin}，允许范围：1-{cartRingConfig.TotalCartCount}";
            _logger.LogError("{ErrorMessage}", errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        // 5. 计算当前首车编号（CartIndex 是 0 基索引，需要转换为 1 基索引）
        var headCartNumber = _cartPositionTracker.CurrentOriginCartIndex.Value.Value + 1;

        // 6. 使用统一的环形算法计算格口当前小车号
        try
        {
            var cartNumber = _calculator.GetCartNumberAtChute(
                cartRingConfig.TotalCartCount,
                headCartNumber,
                chuteConfig.CartNumberWhenHeadAtOrigin);

            _logger.LogDebug(
                "解析格口小车号成功。格口ID={ChuteId}, 总小车数={TotalCartCount}, 首车号={HeadCartNumber}, " +
                "格口基准小车号={CartNumberWhenHeadAtOrigin}, 计算结果={CartNumber}",
                chuteId, cartRingConfig.TotalCartCount, headCartNumber,
                chuteConfig.CartNumberWhenHeadAtOrigin, cartNumber);

            return cartNumber;
        }
        catch (ArgumentException ex)
        {
            // 计算器内部验证失败，记录详细错误信息
            _logger.LogError(ex, "计算格口小车号时发生参数错误");
            throw new InvalidOperationException($"计算格口 {chuteId} 小车号失败：{ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public int GetCurrentHeadCartNumber()
    {
        if (!_cartPositionTracker.IsInitialized || _cartPositionTracker.CurrentOriginCartIndex == null)
        {
            var errorMsg = "当前首车状态未就绪";
            _logger.LogError("{ErrorMessage}。IsInitialized={IsInitialized}, CurrentOriginCartIndex={CurrentOriginCartIndex}",
                errorMsg, _cartPositionTracker.IsInitialized, _cartPositionTracker.CurrentOriginCartIndex);
            throw new InvalidOperationException(errorMsg);
        }

        // CartIndex 是 0 基索引，转换为 1 基索引
        var headCartNumber = _cartPositionTracker.CurrentOriginCartIndex.Value.Value + 1;
        
        _logger.LogDebug("获取当前首车编号：{HeadCartNumber}", headCartNumber);
        
        return headCartNumber;
    }
}
