using HocTap.Application.Feedbacks;
using HocTap.Domain.Aggregates.Feedbacks;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;
using HocTap.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HocTap.WebApi.Controllers;

/// <summary>
/// 18/10/2026 - GÓP Ý / BÁO LỖI CÂU HỎI
///   POST api/v1/feedback                (không bắt buộc đăng nhập; 5 lần / 10 phút / IP) gửi góp ý
///   GET  api/v1/admin/feedback          (ADMIN) ?status=&kind=&q=&page=&pageSize= danh sách + số tổng hợp
///   PUT  api/v1/admin/feedback/{id}     (ADMIN) { status, adminNote } đổi trạng thái / ghi chú nội bộ
/// </summary>
[Route("api/v1")]
public class FeedbackController : BaseApiController
{
    private readonly IFeedbackServices feedbackServices;
    private readonly ILogger<FeedbackController> logger;

    public FeedbackController(IFeedbackServices i_FeedbackServices, ILogger<FeedbackController> i_Logger)
    {
        feedbackServices = i_FeedbackServices;
        logger = i_Logger;
    }

    /// <summary>Gửi góp ý: có token thì ghi theo tài khoản, không có thì ghi là khách</summary>
    [AllowAnonymous]
    [EnableRateLimiting(Program.RATE_FEEDBACK)]
    [HttpPost("feedback")]
    public async Task<IActionResult> Submit([FromBody] SubmitFeedbackModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser? caller = CallerOrNull;
            Guid retObj = await Task.Run(() => feedbackServices.Submit(caller, i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FeedbackController.Submit");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet("admin/feedback")]
    public async Task<IActionResult> GetListFeedback([FromQuery] AdminFeedbackQuery i_query)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AdminFeedbackPageReadModel retObj = await Task.Run(() => feedbackServices.GetListFeedback(caller, i_query));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FeedbackController.GetListFeedback");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("admin/feedback/{id:guid}")]
    public async Task<IActionResult> UpdateFeedback(Guid id, [FromBody] UpdateFeedbackModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AdminFeedbackReadModel retObj = await Task.Run(() => feedbackServices.UpdateFeedback(caller, id, i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FeedbackController.UpdateFeedback");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }
}
