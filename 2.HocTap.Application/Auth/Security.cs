using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HocTap.Domain.Commons;
using HocTap.Domain.Aggregates.Auth;
using Microsoft.IdentityModel.Tokens;

namespace HocTap.Application.Auth;

/// <summary>08/10/2026 - Cấu hình JWT (appsettings "Jwt" hoặc biến môi trường Jwt__Key ...)</summary>
public class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "hoctap";
    public string Audience { get; set; } = "hoctap-web";
    public int AccessMinutes { get; set; } = 30;
    public int RefreshDays { get; set; } = 30;
}

/// <summary>08/10/2026 - Băm mật khẩu / PIN bằng BCrypt</summary>
public interface IPasswordHasher
{
    string Hash(string raw);
    bool Verify(string raw, string? hash);
}

public class BCryptHasher : IPasswordHasher
{
    public string Hash(string raw) => BCrypt.Net.BCrypt.HashPassword(raw, 11);
    public bool Verify(string raw, string? hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        try { return BCrypt.Net.BCrypt.Verify(raw, hash); } catch { return false; }
    }
}

/// <summary>08/10/2026 - Cấp JWT + refresh token (refresh token ngẫu nhiên 48 byte, CSDL chỉ giữ SHA-256)</summary>
public interface ITokenService
{
    (string Token, DateTime Expires) CreateAccessToken(AppUserModel user, Guid? studentId, DateTime nowUtc);
    string NewRefreshToken();
    string HashRefreshToken(string raw);
    JwtOptions Options { get; }
}

public class JwtTokenService : ITokenService
{
    public JwtOptions Options { get; }
    public JwtTokenService(JwtOptions options) { Options = options; }

    /// <summary>Token phiên học sinh: role HOCSINH + claim sid = hồ sơ con; phụ huynh / admin: role theo tài khoản</summary>
    public (string Token, DateTime Expires) CreateAccessToken(AppUserModel user, Guid? studentId, DateTime nowUtc)
    {
        var expires = nowUtc.AddMinutes(Options.AccessMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.id.ToString()),
            new(ClaimTypes.Role, studentId.HasValue ? HocTapConst.ROLE_STUDENT : user.role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        if (studentId.HasValue) claims.Add(new Claim(HocTapConst.CLAIM_STUDENT, studentId.Value.ToString()));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Key));
        var jwt = new JwtSecurityToken(Options.Issuer, Options.Audience, claims, nowUtc, expires,
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }

    public string NewRefreshToken() => Base64Url(RandomNumberGenerator.GetBytes(48));

    public string HashRefreshToken(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();

    private static string Base64Url(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
