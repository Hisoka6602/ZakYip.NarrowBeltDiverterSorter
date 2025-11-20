using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

/// <summary>
/// 包裹小车绑定服务实现
/// 负责统一处理包裹创建时的小车号绑定逻辑
/// </summary>
public sealed class PackageCartBinder : IPackageCartBinder
{
    private readonly ICartAtChuteResolver _cartAtChuteResolver;
    private readonly ILogger<PackageCartBinder> _logger;

    public PackageCartBinder(
        ICartAtChuteResolver cartAtChuteResolver,
        ILogger<PackageCartBinder> logger)
    {
        _cartAtChuteResolver = cartAtChuteResolver ?? throw new ArgumentNullException(nameof(cartAtChuteResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public int BindCartForNewPackage(long packageId, long chuteId)
    {
        try
        {
            // 通过 resolver 获取当前格口的小车号
            var cartNumber = _cartAtChuteResolver.ResolveCurrentCartNumberForChute(chuteId);

            _logger.LogInformation(
                "包裹 {PackageId} 成功绑定到小车 {CartNumber}（格口 {ChuteId}）",
                packageId, cartNumber, chuteId);

            return cartNumber;
        }
        catch (InvalidOperationException ex)
        {
            // 捕获 resolver 抛出的业务异常，记录并重新抛出带有明确中文信息的异常
            var errorMsg = $"当前小车状态未准备好，暂不允许创建包裹：{ex.Message}";
            _logger.LogError(ex,
                "为包裹 {PackageId} 绑定小车号失败（格口 {ChuteId}）：{ErrorMessage}",
                packageId, chuteId, ex.Message);
            throw new InvalidOperationException(errorMsg, ex);
        }
        catch (Exception ex)
        {
            // 捕获其他意外异常
            var errorMsg = $"绑定包裹小车号时发生意外错误：{ex.Message}";
            _logger.LogError(ex,
                "为包裹 {PackageId} 绑定小车号时发生意外错误（格口 {ChuteId}）",
                packageId, chuteId);
            throw new InvalidOperationException(errorMsg, ex);
        }
    }
}
