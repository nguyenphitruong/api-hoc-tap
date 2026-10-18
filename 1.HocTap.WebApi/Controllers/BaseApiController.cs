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

    /// <summary>18/10/2026 - Người gọi nếu có token hợp lệ, ngược lại null (dùng cho API không bắt buộc đăng nhập như gửi góp ý)</summary>
    protected CurrentUser? CallerOrNull
    {
        get
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out var userId)) return null;
            var sid = User.FindFirstValue(HocTapConst.CLAIM_STUDENT);
            return new CurrentUser(userId, User.FindFirstValue(ClaimTypes.Role) ?? "", Guid.TryParse(sid, out var s) ? s : null);
        }
    }
}
