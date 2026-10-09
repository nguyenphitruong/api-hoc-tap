using HocTap.Application.Auth;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;
using HocTap.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HocTap.WebApi.Controllers;

/// <summary>
/// 08/10/2026 - Đăng ký / đăng nhập / làm mới token / đăng xuất / vào phiên học sinh / đổi mật khẩu (giới hạn tần suất theo IP)
/// Mẫu mỗi action (giống PayMentController Web-MultiClinic): try { retObj = await Task.Run(() => service.Xxx(...)); sr = ApiResult.Ok(retObj); }
/// catch LogicException -> HTTP ErrorCode (400/401/403/404) + thông báo nghiệp vụ; catch Exception -> 500 (ghi log).
/// </summary>
[Route("api/v1/auth")]
[EnableRateLimiting(Program.RATE_AUTH)]
public class AuthController : BaseApiController
{
    private readonly IAuthServices authServices;
    private readonly ILogger<AuthController> logger;

    public AuthController(IAuthServices i_AuthServices, ILogger<AuthController> i_Logger)
    {
        authServices = i_AuthServices;
        logger = i_Logger;
    }

    /// <summary>Đăng ký tài khoản phụ huynh (dùng thử 14 ngày), trả token</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel i_model)
    {
        ApiResult sr;
        try
        {
            AuthResultReadModel retObj = await Task.Run(() => authServices.Register(i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AuthController.Register");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Đăng nhập bằng email hoặc số điện thoại</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel i_model)
    {
        ApiResult sr;
        try
        {
            AuthResultReadModel retObj = await Task.Run(() => authServices.Login(i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AuthController.Login");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Làm mới token (xoay vòng refresh token)</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshModel i_model)
    {
        ApiResult sr;
        try
        {
            AuthResultReadModel retObj = await Task.Run(() => authServices.Refresh(i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AuthController.Refresh");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Đăng xuất: thu hồi refresh token</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshModel i_model)
    {
        ApiResult sr;
        try
        {
            await Task.Run(() => authServices.Logout(i_model));
            sr = ApiResult.Ok();
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AuthController.Logout");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Vào phiên học sinh (chọn hồ sơ con + PIN)</summary>
    [Authorize]
    [HttpPost("student-session")]
    public async Task<IActionResult> StudentSession([FromBody] StudentSessionModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AuthResultReadModel retObj = await Task.Run(() => authServices.StudentSession(caller, i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AuthController.StudentSession");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Thông tin tài khoản đang đăng nhập</summary>
    [Authorize]
    [HttpGet("me")]
    [DisableRateLimiting]
    public async Task<IActionResult> Me()
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AuthResultReadModel retObj = await Task.Run(() => authServices.Me(caller));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AuthController.Me");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Giai đoạn 5: đổi mật khẩu (thu hồi mọi phiên, client đăng nhập lại)</summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            await Task.Run(() => authServices.ChangePassword(caller, i_model));
            sr = ApiResult.Ok();
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AuthController.ChangePassword");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }
}
