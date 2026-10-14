using HocTap.Application.Admin;
using HocTap.Application.Assignments;
using HocTap.Application.Attempts;
using HocTap.Application.Auth;
using HocTap.Application.Commons;
using HocTap.Application.Students;
using HocTap.Application.Payments;   // 12/10/2026
using HocTap.Application.Settings;   // 14/10/2026
using Microsoft.Extensions.DependencyInjection;

namespace HocTap.Application;

/// <summary>08/10/2026 - Đăng ký service tầng ứng dụng</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddHocTapApplication(this IServiceCollection services, JwtOptions jwt)
    {
        services.AddSingleton(jwt);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BCryptHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        // 08/10/2026 - Service viết tường minh theo mẫu Web-MultiClinic (XxxServices : IXxxServices, dùng IUnitOfWork)
        services.AddScoped<IAuthServices, AuthServices>();
        services.AddScoped<IStudentServices, StudentServices>();
        services.AddScoped<IAttemptServices, AttemptServices>();
        services.AddScoped<IAssignmentServices, AssignmentServices>();
        services.AddScoped<IAdminServices, AdminServices>();
        services.AddScoped<IPaymentServices, PaymentServices>();   // 12/10/2026 - gia hạn chuyển khoản QR (PaymentOptions đăng ký ở Program)
        services.AddSingleton<SettingCache>();                      // 14/10/2026 - bộ nhớ đệm bảng giá + liên hệ (5 phút)
        services.AddScoped<ISettingServices, SettingServices>();    // 14/10/2026 - cài đặt hệ thống (admin)
        return services;
    }
}
