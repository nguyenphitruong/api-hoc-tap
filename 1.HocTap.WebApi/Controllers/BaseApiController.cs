using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;
using HocTap.WebApi.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace HocTap.WebApi.Controllers;

/// <summary>
/// 08/10/2026 - Controller gốc: đọc người gọi từ JWT (sub, role, sid).
/// Các controller con tự bắt lỗi ở từng action (LogicException -> ErrorCode, Exception -> 500 + SYSTEM_ERROR).
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected CurrentUser Caller
    {
        get
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
            var sid = User.FindFirstValue(HocTapConst.CLAIM_STUDENT);
            if (!Guid.TryParse(sub, out var userId)) throw new LogicException("Chưa đăng nhập.", 401);
            return new CurrentUser(userId, role, Guid.TryParse(sid, out var s) ? s : null);
        }
    }

    /// <summary>Thông báo trả về khi lỗi hệ thống (chi tiết ghi log)</summary>
    protected const string SYSTEM_ERROR = "Lỗi hệ thống, vui lòng thử lại sau.";
}
