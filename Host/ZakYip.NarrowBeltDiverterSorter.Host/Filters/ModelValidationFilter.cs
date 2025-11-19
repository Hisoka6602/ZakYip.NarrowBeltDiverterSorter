using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ZakYip.NarrowBeltDiverterSorter.Host.DTOs;

namespace ZakYip.NarrowBeltDiverterSorter.Host.Filters;

/// <summary>
/// 模型验证过滤器
/// 自动检查 ModelState，如果验证失败则返回统一的错误响应
/// </summary>
public class ModelValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(ms => ms.Value?.Errors.Count > 0)
                .ToDictionary(
                    ms => ms.Key,
                    ms => ms.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            var result = ApiResult.Fail(
                message: "请求参数验证失败",
                errorCode: "ValidationError",
                errors: errors
            );

            context.Result = new BadRequestObjectResult(result);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No implementation needed
    }
}
