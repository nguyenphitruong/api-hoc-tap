using HocTap.Domain.Commons;

namespace HocTap.WebApi.Helpers;

/// <summary>
/// 08/10/2026 - Bắt lỗi DỰ PHÒNG (controller đã try/catch ở từng action; middleware chỉ bắt lỗi ngoài action):
/// LogicException → HTTP ErrorCode (400/401/403/404) + thông báo nghiệp vụ; lỗi khác → 500 "Lỗi hệ thống" (ghi log).
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _log;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log) { _next = next; _log = log; }

    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (LogicException ex)
        {
            await Write(ctx, ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Lỗi xử lý {Path}", ctx.Request.Path);
            await Write(ctx, 500, "Lỗi hệ thống, vui lòng thử lại sau.");
        }
    }

    private static Task Write(HttpContext ctx, int code, string message)
    {
        if (ctx.Response.HasStarted) return Task.CompletedTask;
        ctx.Response.Clear();
        ctx.Response.StatusCode = code;
        return ctx.Response.WriteAsJsonAsync(ApiResult.Fail(code, message));
    }
}
