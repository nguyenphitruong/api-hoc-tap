using HocTap.Application.Admin;
using HocTap.Domain.Aggregates.Admin;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;
using HocTap.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HocTap.WebApi.Controllers;

/// <summary>
/// 08/10/2026 - Giai đoạn 5: quản trị (chỉ ADMIN) — tài khoản, gói, khoá, lịch sử gói, thống kê
/// Mẫu mỗi action (giống PayMentController Web-MultiClinic): try { retObj = await Task.Run(() => service.Xxx(...)); sr = ApiResult.Ok(retObj); }
/// catch LogicException -> HTTP ErrorCode (400/401/403/404) + thông báo nghiệp vụ; catch Exception -> 500 (ghi log).
/// </summary>
[Authorize(Roles = "ADMIN")]
[Route("api/v1/admin")]
public class AdminController : BaseApiController
{
    private readonly IAdminServices adminServices;
    private readonly ILogger<AdminController> logger;

    public AdminController(IAdminServices i_AdminServices, ILogger<AdminController> i_Logger)
    {
        adminServices = i_AdminServices;
        logger = i_Logger;
    }

    /// <summary>Danh sách tài khoản phụ huynh (tìm, lọc trạng thái, phân trang)</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetListUser([FromQuery] AdminUserQuery i_query)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AdminUserPageReadModel retObj = await Task.Run(() => adminServices.GetListUser(caller, i_query));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AdminController.GetListUser");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Kích hoạt gói / gia hạn dùng thử</summary>
    [HttpPost("users/{id:guid}/plan")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] AdminPlanModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AdminUserReadModel retObj = await Task.Run(() => adminServices.UpdatePlan(caller, id, i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AdminController.UpdatePlan");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Khoá / mở khoá tài khoản</summary>
    [HttpPost("users/{id:guid}/lock")]
    public async Task<IActionResult> UpdateLock(Guid id, [FromBody] AdminLockModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AdminUserReadModel retObj = await Task.Run(() => adminServices.UpdateLock(caller, id, i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AdminController.UpdateLock");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Lịch sử gói của 1 tài khoản</summary>
    [HttpGet("users/{id:guid}/plan-logs")]
    public async Task<IActionResult> GetListPlanLog(Guid id)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            List<PlanLogReadModel> retObj = await Task.Run(() => adminServices.GetListPlanLog(caller, id));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AdminController.GetListPlanLog");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Thống kê tổng quan</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AdminStatsReadModel retObj = await Task.Run(() => adminServices.GetStats(caller));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AdminController.GetStats");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }
}
