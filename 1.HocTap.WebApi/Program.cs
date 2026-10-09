using System.Text;
using System.Threading.RateLimiting;
using HocTap.Application;
using HocTap.Application.Auth;
using HocTap.Infrastructure;
using HocTap.WebApi.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace HocTap.WebApi;

/// <summary>
/// 08/10/2026 - Khởi động API HocTap (.NET 8)
/// Cấu hình: ConnectionStrings:DefaultConnection, Jwt:Key (≥ 32 ký tự), Cors:Origins (danh sách web được gọi API).
/// Luồng request: CORS → bắt lỗi → giới hạn tần suất (auth) → JWT → controller.
/// </summary>
public class Program
{
    public const string RATE_AUTH = "auth";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var cfg = builder.Configuration;

        var jwt = cfg.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrEmpty(jwt.Key) || jwt.Key.Length < 32) throw new InvalidOperationException("Thiếu Jwt:Key (tối thiểu 32 ký tự).");
        var conn = cfg.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection.");

        builder.Services.AddHocTapInfrastructure(conn);
        builder.Services.AddHocTapApplication(jwt);
        builder.Services.AddControllers();

        // Lỗi dữ liệu đầu vào (JSON sai kiểu) cũng trả về { code, message }
        builder.Services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = _ =>
            new BadRequestObjectResult(ApiResult.Fail(400, "Dữ liệu gửi lên không hợp lệ.")));

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
        {
            o.MapInboundClaims = false;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = jwt.Issuer,
                ValidateAudience = true, ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
                RoleClaimType = System.Security.Claims.ClaimTypes.Role, NameClaimType = "sub",
            };
            // 401 / 403 cũng trả về dạng { code, message } để client xử lý thống nhất
            o.Events = new JwtBearerEvents
            {
                OnChallenge = async ctx =>
                {
                    ctx.HandleResponse();
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.WriteAsJsonAsync(ApiResult.Fail(401, "Phiên đăng nhập đã hết hạn."));
                },
            };
        });
        builder.Services.AddAuthorization();

        // Giới hạn tần suất các API đăng nhập / PIN: 20 lần / phút / IP
        builder.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = 429;
            o.AddPolicy(RATE_AUTH, ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "x",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = cfg.GetValue("RateLimit:AuthPerMinute", 20), Window = TimeSpan.FromMinutes(1) }));
            o.OnRejected = async (ctx, ct) =>
                await ctx.HttpContext.Response.WriteAsJsonAsync(ApiResult.Fail(429, "Thao tác quá nhiều lần, vui lòng thử lại sau 1 phút."), ct);
        });

        var origins = cfg.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

        // 08/10/2026 - Giai đoạn 5: chạy sau proxy (Render) -> lấy IP thật từ X-Forwarded-For cho giới hạn tần suất
        builder.Services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.KnownNetworks.Clear();
            o.KnownProxies.Clear();
        });

        var app = builder.Build();
        app.UseForwardedHeaders();
        app.UseCors();
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        app.MapControllers();
        app.Run();
    }
}
