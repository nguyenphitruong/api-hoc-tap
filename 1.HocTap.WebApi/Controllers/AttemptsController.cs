using HocTap.Application.Attempts;
using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;
using HocTap.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HocTap.WebApi.Controllers;

/// <summary>
/// 08/10/2026 - Nộp lượt học (phiên học sinh) và xem lại từng câu
/// Mẫu mỗi action (giống PayMentController Web-MultiClinic): try { retObj = await Task.Run(() => service.Xxx(...)); sr = ApiResult.Ok(retObj); }
/// catch LogicException -> HTTP ErrorCode (400/401/403/404) + thông báo nghiệp vụ; catch Exception -> 500 (ghi log).
/// </summary>
[Authorize]
[Route("api/v1/attempts")]
public class AttemptsController : BaseApiController
{
    private readonly IAttemptServices attemptServices;
    private readonly ILogger<AttemptsController> logger;

    public AttemptsController(IAttemptServices i_AttemptServices, ILogger<AttemptsController> i_Logger)
    {
        attemptServices = i_AttemptServices;
        logger = i_Logger;
    }

    /// <summary>Nộp 1 lượt học</summary>
    [HttpPost]
    [RequestSizeLimit(1_000_000)]
    public async Task<IActionResult> InsertAttempt([FromBody] AttemptSaveModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AttemptReadModel retObj = await Task.Run(() => attemptServices.InsertAttempt(caller, i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AttemptsController.InsertAttempt");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Xem lại 1 lượt kèm từng câu</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAttemptDetail(Guid id)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AttemptDetailReadModel retObj = await Task.Run(() => attemptServices.GetAttemptDetail(caller, id));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AttemptsController.GetAttemptDetail");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }
}
