using HocTap.Application.Assignments;
using HocTap.Application.Attempts;
using HocTap.Application.Students;
using HocTap.Domain.Aggregates.Assignments;
using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;
using HocTap.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HocTap.WebApi.Controllers;

/// <summary>
/// 08/10/2026 - Hồ sơ con (phụ huynh thêm / sửa / xoá; học sinh chỉ xem danh sách) + tiến độ + nhập lịch sử cũ + bài đang giao cho con
/// Mẫu mỗi action (giống PayMentController Web-MultiClinic): try { retObj = await Task.Run(() => service.Xxx(...)); sr = ApiResult.Ok(retObj); }
/// catch LogicException -> HTTP ErrorCode (400/401/403/404) + thông báo nghiệp vụ; catch Exception -> 500 (ghi log).
/// </summary>
[Authorize]
[Route("api/v1/students")]
public class StudentsController : BaseApiController
{
    private readonly IStudentServices studentServices;
    private readonly IAttemptServices attemptServices;
    private readonly IAssignmentServices assignmentServices;
    private readonly ILogger<StudentsController> logger;

    public StudentsController(IStudentServices i_StudentServices, IAttemptServices i_AttemptServices, IAssignmentServices i_AssignmentServices, ILogger<StudentsController> i_Logger)
    {
        studentServices = i_StudentServices;
        attemptServices = i_AttemptServices;
        assignmentServices = i_AssignmentServices;
        logger = i_Logger;
    }

    /// <summary>Danh sách hồ sơ con của tài khoản</summary>
    [HttpGet]
    public async Task<IActionResult> GetListStudent()
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            List<StudentReadModel> retObj = await Task.Run(() => studentServices.GetListStudent(caller));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "StudentsController.GetListStudent");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Thêm hồ sơ con</summary>
    [HttpPost]
    public async Task<IActionResult> InsertStudent([FromBody] StudentSaveModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            StudentReadModel retObj = await Task.Run(() => studentServices.InsertStudent(caller, i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "StudentsController.InsertStudent");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Sửa hồ sơ con</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] StudentSaveModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            StudentReadModel retObj = await Task.Run(() => studentServices.UpdateStudent(caller, id, i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "StudentsController.UpdateStudent");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Xoá (ẩn) hồ sơ con</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteStudent(Guid id)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            await Task.Run(() => studentServices.DeleteStudent(caller, id));
            sr = ApiResult.Ok();
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "StudentsController.DeleteStudent");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Tiến độ: các lượt học của 1 hồ sơ</summary>
    [HttpGet("{id:guid}/progress")]
    public async Task<IActionResult> GetListProgress(Guid id)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            List<AttemptReadModel> retObj = await Task.Run(() => attemptServices.GetListProgress(caller, id));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "StudentsController.GetListProgress");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Nhập lịch sử học cũ vào 1 hồ sơ</summary>
    [HttpPost("{id:guid}/import")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> ImportAttempt(Guid id, [FromBody] ImportModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            ImportResultReadModel retObj = await Task.Run(() => attemptServices.ImportAttempt(caller, id, i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "StudentsController.ImportAttempt");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Giai đoạn 4: bài đang giao cho con (kèm tình trạng của con)</summary>
    [HttpGet("{id:guid}/assignments")]
    public async Task<IActionResult> GetListAssignmentOfStudent(Guid id)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            List<AssignmentReadModel> retObj = await Task.Run(() => assignmentServices.GetListAssignmentOfStudent(caller, id));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "StudentsController.GetListAssignmentOfStudent");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }
}
