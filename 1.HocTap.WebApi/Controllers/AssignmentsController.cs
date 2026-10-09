using HocTap.Application.Assignments;
using HocTap.Domain.Aggregates.Assignments;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;
using HocTap.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HocTap.WebApi.Controllers;

/// <summary>
/// 08/10/2026 - Giai đoạn 4: giao bài tập. Phụ huynh: danh sách, giao mới, sửa / đóng, giao lại câu sai, kết quả.
/// Phụ huynh hoặc con được giao: chi tiết bài (kèm câu hỏi nếu bài lưu sẵn câu). Danh sách bài của con: GET students/{id}/assignments.
/// Mẫu mỗi action (giống PayMentController Web-MultiClinic): try { retObj = await Task.Run(() => service.Xxx(...)); sr = ApiResult.Ok(retObj); }
/// catch LogicException -> HTTP ErrorCode (400/401/403/404) + thông báo nghiệp vụ; catch Exception -> 500 (ghi log).
/// </summary>
[Authorize]
[Route("api/v1/assignments")]
public class AssignmentsController : BaseApiController
{
    private readonly IAssignmentServices assignmentServices;
    private readonly ILogger<AssignmentsController> logger;

    public AssignmentsController(IAssignmentServices i_AssignmentServices, ILogger<AssignmentsController> i_Logger)
    {
        assignmentServices = i_AssignmentServices;
        logger = i_Logger;
    }

    /// <summary>Danh sách bài đã giao</summary>
    [HttpGet]
    public async Task<IActionResult> GetListAssignment()
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            List<AssignmentReadModel> retObj = await Task.Run(() => assignmentServices.GetListAssignment(caller));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AssignmentsController.GetListAssignment");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Giao bài mới</summary>
    [HttpPost]
    public async Task<IActionResult> InsertAssignment([FromBody] AssignmentCreateModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AssignmentReadModel retObj = await Task.Run(() => assignmentServices.InsertAssignment(caller, i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AssignmentsController.InsertAssignment");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Sửa / đóng bài</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAssignment(Guid id, [FromBody] AssignmentUpdateModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AssignmentReadModel retObj = await Task.Run(() => assignmentServices.UpdateAssignment(caller, id, i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AssignmentsController.UpdateAssignment");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Chi tiết bài</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAssignmentDetail(Guid id)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AssignmentReadModel retObj = await Task.Run(() => assignmentServices.GetAssignmentDetail(caller, id));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AssignmentsController.GetAssignmentDetail");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Kết quả: mọi lượt nộp của bài</summary>
    [HttpGet("{id:guid}/results")]
    public async Task<IActionResult> GetAssignmentResult(Guid id)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AssignmentResultReadModel retObj = await Task.Run(() => assignmentServices.GetAssignmentResult(caller, id));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AssignmentsController.GetAssignmentResult");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Giao lại câu sai cho 1 con</summary>
    [HttpPost("{id:guid}/retry-wrong")]
    public async Task<IActionResult> InsertRetryWrong(Guid id, [FromBody] AssignmentRetryModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AssignmentReadModel retObj = await Task.Run(() => assignmentServices.InsertRetryWrong(caller, id, i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AssignmentsController.InsertRetryWrong");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }
}
