using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Core.SelfCheck;

/// <summary>
/// 小车环配置管理器实现
/// 负责根据自检结果自动学习或校验小车环配置
/// </summary>
public sealed class CartRingConfigurationManager : ICartRingConfigurationManager
{
    private readonly ICartRingConfigurationProvider _configProvider;
    private readonly ICartRingHealthService? _healthService;
    private readonly ILogger<CartRingConfigurationManager> _logger;

    public CartRingConfigurationManager(
        ICartRingConfigurationProvider configProvider,
        ILogger<CartRingConfigurationManager> logger,
        ICartRingHealthService? healthService = null)
    {
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _healthService = healthService; // 可选依赖
    }

    /// <inheritdoc/>
    public CartRingConfigurationMode GetCurrentMode()
    {
        var config = _configProvider.Current;
        return config.TotalCartCount <= 0 
            ? CartRingConfigurationMode.AutoLearning 
            : CartRingConfigurationMode.Verification;
    }

    /// <inheritdoc/>
    public async Task<CartRingConfigurationProcessResult> ProcessSelfCheckResultAsync(
        CartRingSelfCheckResult selfCheckResult,
        CancellationToken cancellationToken = default)
    {
        if (selfCheckResult == null)
        {
            throw new ArgumentNullException(nameof(selfCheckResult));
        }

        var currentConfig = _configProvider.Current;
        var mode = currentConfig.TotalCartCount <= 0 
            ? CartRingConfigurationMode.AutoLearning 
            : CartRingConfigurationMode.Verification;

        if (mode == CartRingConfigurationMode.AutoLearning)
        {
            return await ProcessAutoLearningModeAsync(selfCheckResult, currentConfig, cancellationToken);
        }
        else
        {
            return ProcessVerificationMode(selfCheckResult, currentConfig);
        }
    }

    /// <summary>
    /// 处理自动学习模式
    /// </summary>
    private async Task<CartRingConfigurationProcessResult> ProcessAutoLearningModeAsync(
        CartRingSelfCheckResult selfCheckResult,
        CartRingConfiguration currentConfig,
        CancellationToken cancellationToken)
    {
        var detectedCount = selfCheckResult.MeasuredCartCount;

        // 如果检测到的小车数量 > 0，则更新配置
        if (detectedCount > 0)
        {
            var newConfig = new CartRingConfiguration
            {
                TotalCartCount = detectedCount
            };

            await _configProvider.UpdateAsync(newConfig, cancellationToken);
            
            _logger.LogInformation(
                "自动检测到小车总数：{DetectedCount}，已写入配置",
                detectedCount);

            return new CartRingConfigurationProcessResult
            {
                ConfigurationUpdated = true,
                HasError = false,
                Mode = CartRingConfigurationMode.AutoLearning,
                ExpectedCartCount = 0, // 学习模式没有期望值
                DetectedCartCount = detectedCount
            };
        }
        else
        {
            // 没有检测到小车，可能是数据不足
            _logger.LogWarning("自动学习模式下未检测到小车，需要更多数据");
            
            return new CartRingConfigurationProcessResult
            {
                ConfigurationUpdated = false,
                HasError = false,
                Mode = CartRingConfigurationMode.AutoLearning,
                ExpectedCartCount = 0,
                DetectedCartCount = 0
            };
        }
    }

    /// <summary>
    /// 处理校验模式
    /// </summary>
    private CartRingConfigurationProcessResult ProcessVerificationMode(
        CartRingSelfCheckResult selfCheckResult,
        CartRingConfiguration currentConfig)
    {
        var expectedCount = currentConfig.TotalCartCount;
        var detectedCount = selfCheckResult.MeasuredCartCount;

        if (detectedCount == expectedCount)
        {
            // 校验通过
            _logger.LogInformation(
                "小车环校验通过，期望：{Expected}，实际：{Actual}",
                expectedCount,
                detectedCount);

            // 清除健康状态异常
            _healthService?.ClearCartRingMismatch();

            return new CartRingConfigurationProcessResult
            {
                ConfigurationUpdated = false,
                HasError = false,
                Mode = CartRingConfigurationMode.Verification,
                ExpectedCartCount = expectedCount,
                DetectedCartCount = detectedCount
            };
        }
        else
        {
            // 校验失败
            var errorMessage = $"小车环校验失败，期望：{expectedCount}，实际：{detectedCount}";
            _logger.LogError(errorMessage);

            // 设置健康状态异常
            _healthService?.SetCartRingMismatch(expectedCount, detectedCount);

            return new CartRingConfigurationProcessResult
            {
                ConfigurationUpdated = false,
                HasError = true,
                ErrorMessage = errorMessage,
                Mode = CartRingConfigurationMode.Verification,
                ExpectedCartCount = expectedCount,
                DetectedCartCount = detectedCount
            };
        }
    }
}
