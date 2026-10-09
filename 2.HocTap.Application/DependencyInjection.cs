using HocTap.Application.Admin;
using HocTap.Application.Assignments;
using HocTap.Application.Attempts;
using HocTap.Application.Auth;
using HocTap.Application.Commons;
using HocTap.Application.Students;
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
        return services;
    }
}
