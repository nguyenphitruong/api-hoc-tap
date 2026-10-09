using HocTap.Domain.Aggregates.Admin;
using HocTap.Domain.Bases;

namespace HocTap.Application.Admin;

/// <summary>08/10/2026 - Service quản trị (chỉ tài khoản ADMIN)</summary>
public interface IAdminServices
{
    AdminUserPageReadModel GetListUser(CurrentUser i_caller, AdminUserQuery i_query);
    /// <summary>Kích hoạt / gia hạn gói (KICHHOAT) hoặc gia hạn dùng thử (GIAHANTHU)</summary>
    AdminUserReadModel UpdatePlan(CurrentUser i_caller, Guid i_userId, AdminPlanModel i_model);
    AdminUserReadModel UpdateLock(CurrentUser i_caller, Guid i_userId, AdminLockModel i_model);
    List<PlanLogReadModel> GetListPlanLog(CurrentUser i_caller, Guid i_userId);
    AdminStatsReadModel GetStats(CurrentUser i_caller);
}
