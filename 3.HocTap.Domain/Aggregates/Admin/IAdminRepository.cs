using HocTap.Domain.Aggregates.Auth;

namespace HocTap.Domain.Aggregates.Admin;

/// <summary>08/10/2026 - Dòng lịch sử gói kèm họ tên admin thực hiện</summary>
public class PlanLogRow
{
    public PlanLogModel Log { get; set; } = new();
    public string AdminName { get; set; } = "";
}

/// <summary>08/10/2026 - Repository quản trị: danh sách tài khoản kèm số liệu, plan_log, thống kê</summary>
public interface IAdminRepository
{
    /// <summary>Tài khoản phụ huynh (lọc chữ ở CSDL) kèm số hồ sơ / lượt học / lần học gần nhất</summary>
    List<AdminUserRow> GetListParentUser(string? i_query);
    List<AppUserModel> GetAllParentUser();
    void InsertPlanLog(PlanLogModel i_log);
    List<PlanLogRow> GetListPlanLog(Guid i_userId);
    int CountActiveStudent();
    int CountStudentHasAttemptSince(DateTime i_sinceUtc);
    int CountAttempt();
    int CountAssignment();
    List<DateTime> GetListAttemptTime(DateTime i_sinceUtc);
}
