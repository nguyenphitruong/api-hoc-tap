using HocTap.Application.Guests;
using HocTap.Domain.Aggregates.Guests;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;
using HocTap.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HocTap.WebApi.Controllers;

/// <summary>
/// 17/10/2026 - KHÁCH HỌC THỬ
///   POST api/v1/guest/events       (không cần đăng nhập, 60 lần / phút / IP) web gửi 1 hoạt động { guestId, kind, grade, subject, topicId, total, correct, score10, durationSec, note }
///   GET  api/v1/admin/guests?days= (ADMIN) thống kê khách học thử N ngày (1–90, mặc định 30) cho tab "Học thử" trang quản trị
/// Gắn hoạt động vào tài khoản: AuthController register / login nhận thêm guestId (AuthServices.LinkGuestSafe).
/// </summary>
[Route("api/v1")]
public class GuestController : BaseApiController
{
    private readonly IGuestServices guestServices;
    private readonly ILogger<GuestController> logger;

    public GuestController(IGuestServices i_GuestServices, ILogger<GuestController> i_Logger)
    {
        guestServices = i_GuestServices;
        logger = i_Logger;
    }

    /// <summary>Ghi 1 hoạt động học thử. Vượt giới hạn trong ngày vẫn trả 200 (bỏ qua lặng lẽ)</summary>
    [AllowAnonymous]
    [EnableRateLimiting(Program.RATE_GUEST)]
    [HttpPost("guest/events")]
    public async Task<IActionResult> Track([FromBody] GuestEventModel i_model)
    {
        ApiResult sr;
        try
        {
            bool retObj = await Task.Run(() => guestServices.Track(i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GuestController.Track");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>Thống kê khách học thử (chỉ ADMIN)</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("admin/guests")]
    public async Task<IActionResult> GetAdminStats([FromQuery] int days = 30)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AdminGuestStatsReadModel retObj = await Task.Run(() => guestServices.GetAdminStats(caller, days));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GuestController.GetAdminStats");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }
}
