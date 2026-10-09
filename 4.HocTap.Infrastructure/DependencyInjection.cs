using HocTap.Domain.Bases;
using HocTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HocTap.Infrastructure;

/// <summary>
/// 08/10/2026 - Đăng ký DbContext + UnitOfWork (scoped theo request). Repository không đăng ký riêng:
/// Service lấy qua unitOfWork.UserRepo / StudentRepo / AttemptRepo / AssignmentRepo / AdminRepo (giống Web-MultiClinic).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddHocTapInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Neon (pooler) có thể cắt kết nối rảnh / khi compute ngủ -> EndOfStreamException.
        // Bật thử lại tự động cho lỗi tạm thời (3 lần, chờ tối đa 5 giây) — xem ghi chú transaction ở UnitOfWork.
        services.AddDbContext<HocTapDbContext>(o => o.UseNpgsql(connectionString, npg =>
            npg.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
