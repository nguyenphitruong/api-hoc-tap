using HocTap.Application.Commons;
using HocTap.Domain.Aggregates.Admin;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;

namespace HocTap.Application.Admin;

/// <summary>
/// 08/10/2026 - QUẢN TRỊ (viết tường minh theo mẫu Web-MultiClinic) — chỉ tài khoản ADMIN
/// - Danh sách tài khoản phụ huynh: tìm theo tên / email / SĐT, lọc trạng thái hiệu lực, kèm số hồ sơ con / lượt học / lần học gần nhất.
/// - Kích hoạt / gia hạn gói N tháng (cộng nối tiếp nếu gói còn hạn), gia hạn dùng thử N ngày; khoá / mở khoá (khoá = thu hồi mọi phiên).
///   Mỗi thao tác ghi plan_log (ai làm, từ ngày – đến ngày, ghi chú) trong cùng 1 lần Save.
/// - Thống kê: tài khoản theo trạng thái, hồ sơ con, học sinh hoạt động 7 ngày, lượt học + tài khoản mới theo ngày (30 ngày, giờ VN).
/// </summary>
public class AdminServices : IAdminServices
{
    private static readonly TimeSpan VN = TimeSpan.FromHours(7);

    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public AdminServices(IUnitOfWork i_UnitOfWork, IClock i_Clock)
    {
        unitOfWork = i_UnitOfWork;
        clock = i_Clock;
    }

    public AdminUserPageReadModel GetListUser(CurrentUser i_caller, AdminUserQuery i_query)
    {
        AdminUserPageReadModel _Result = new AdminUserPageReadModel();
        ServiceGuard.EnsureAdmin(i_caller);

        // 1. Đọc + lọc trạng thái hiệu lực (tính theo thời điểm hiện tại nên lọc sau khi đọc)
        List<AdminUserRow> lstRow = unitOfWork.AdminRepo.GetListParentUser(i_query.Q);
        List<AdminUserReadModel> lstUser = new List<AdminUserReadModel>();
        foreach (AdminUserRow row in lstRow)
        {
            AdminUserReadModel read = ToReadModel(row);
            if (string.IsNullOrEmpty(i_query.Status) || read.Status == i_query.Status) lstUser.Add(read);
        }

        // 2. Phân trang (10–200 dòng / trang)
        int pageSize = Math.Clamp(i_query.PageSize, 10, 200);
        int page = Math.Max(1, i_query.Page);
        _Result.Total = lstUser.Count;
        _Result.Items = lstUser.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return _Result;
    }

    /// <summary>
    /// KICHHOAT: gói đến = (planend còn hạn ? planend : bây giờ) + Months tháng, trạng thái HOATDONG.
    /// GIAHANTHU: trialend = (trialend còn hạn ? trialend : bây giờ) + Days ngày, trạng thái DUNGTHU (nếu đang không có gói còn hạn).
    /// Tài khoản đang khoá phải mở khoá trước.
    /// </summary>
    public AdminUserReadModel UpdatePlan(CurrentUser i_caller, Guid i_userId, AdminPlanModel i_model)
    {
        // 1. Kiểm tra
        ServiceGuard.EnsureAdmin(i_caller);
        AppUserModel user = GetParentUser(i_userId);
        if (user.status == HocTapConst.STATUS_LOCKED) throw new LogicException("Tài khoản đang bị khoá, hãy mở khoá trước.");

        // 2. Tính hạn mới + dòng lịch sử
        DateTime now = clock.UtcNow;
        PlanLogModel log = new PlanLogModel();
        log.id = Guid.NewGuid();
        log.userid = user.id;
        log.adminid = i_caller.UserId;
        log.note = CleanNote(i_model.Note);
        log.timecr = now;

        if (i_model.Action == HocTapConst.PLAN_ACTIVATE)
        {
            if (i_model.Months < 1 || i_model.Months > HocTapConst.PLAN_MAX_MONTHS) throw new LogicException($"Số tháng từ 1 đến {HocTapConst.PLAN_MAX_MONTHS}.");
            DateTime from = user.planend.HasValue && user.planend > now && user.status == HocTapConst.STATUS_ACTIVE ? user.planend.Value : now;
            user.planend = from.AddMonths(i_model.Months);
            user.status = HocTapConst.STATUS_ACTIVE;
            log.action = HocTapConst.PLAN_ACTIVATE;
            log.fromdate = from;
            log.todate = user.planend;
        }
        else if (i_model.Action == HocTapConst.PLAN_TRIAL)
        {
            if (i_model.Days < 1 || i_model.Days > HocTapConst.PLAN_MAX_TRIAL_DAYS) throw new LogicException($"Số ngày dùng thử từ 1 đến {HocTapConst.PLAN_MAX_TRIAL_DAYS}.");
            DateTime from = user.trialend > now ? user.trialend : now;
            user.trialend = from.AddDays(i_model.Days);
            if (!(user.status == HocTapConst.STATUS_ACTIVE && user.planend > now)) user.status = HocTapConst.STATUS_TRIAL;
            log.action = HocTapConst.PLAN_TRIAL;
            log.fromdate = from;
            log.todate = user.trialend;
        }
        else
        {
            throw new LogicException("Thao tác không hợp lệ.");
        }
        user.timeup = now;

        // 3. Lưu tài khoản + plan_log
        try
        {
            unitOfWork.InitTransaction();
            unitOfWork.UserRepo.UpdateUser(user);
            unitOfWork.AdminRepo.InsertPlanLog(log);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
        return ToReadModel(new AdminUserRow { User = user });
    }

    /// <summary>Khoá: status KHOA + thu hồi mọi phiên. Mở khoá: về HOATDONG nếu có gói, ngược lại DUNGTHU (hết hạn tự tính HETHAN)</summary>
    public AdminUserReadModel UpdateLock(CurrentUser i_caller, Guid i_userId, AdminLockModel i_model)
    {
        // 1. Kiểm tra
        ServiceGuard.EnsureAdmin(i_caller);
        AppUserModel user = GetParentUser(i_userId);
        bool isLocked = user.status == HocTapConst.STATUS_LOCKED;
        if (i_model.Lock == isLocked) throw new LogicException(i_model.Lock ? "Tài khoản đã bị khoá." : "Tài khoản không bị khoá.");

        // 2. Gán trạng thái + dòng lịch sử
        DateTime now = clock.UtcNow;
        if (i_model.Lock) user.status = HocTapConst.STATUS_LOCKED;
        else user.status = user.planend.HasValue ? HocTapConst.STATUS_ACTIVE : HocTapConst.STATUS_TRIAL;
        user.timeup = now;

        PlanLogModel log = new PlanLogModel();
        log.id = Guid.NewGuid();
        log.userid = user.id;
        log.adminid = i_caller.UserId;
        log.action = i_model.Lock ? HocTapConst.PLAN_LOCK : HocTapConst.PLAN_UNLOCK;
        log.note = CleanNote(i_model.Note);
        log.timecr = now;

        // 3. Lưu (khoá thì thu hồi mọi refresh token)
        try
        {
            unitOfWork.InitTransaction();
            if (i_model.Lock)
            {
                unitOfWork.UserRepo.RevokeAllRefreshToken(user.id);
            }
            unitOfWork.UserRepo.UpdateUser(user);
            unitOfWork.AdminRepo.InsertPlanLog(log);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
        return ToReadModel(new AdminUserRow { User = user });
    }

    public List<PlanLogReadModel> GetListPlanLog(CurrentUser i_caller, Guid i_userId)
    {
        List<PlanLogReadModel> lstResult = new List<PlanLogReadModel>();
        ServiceGuard.EnsureAdmin(i_caller);

        List<PlanLogRow> lstRow = unitOfWork.AdminRepo.GetListPlanLog(i_userId);
        foreach (PlanLogRow row in lstRow)
        {
            PlanLogReadModel read = new PlanLogReadModel();
            read.Id = row.Log.id;
            read.Action = row.Log.action;
            read.FromDate = row.Log.fromdate;
            read.ToDate = row.Log.todate;
            read.Note = row.Log.note;
            read.AdminName = row.AdminName;
            read.TimeCr = row.Log.timecr;
            lstResult.Add(read);
        }
        return lstResult;
    }

    public AdminStatsReadModel GetStats(CurrentUser i_caller)
    {
        AdminStatsReadModel _Result = new AdminStatsReadModel();
        ServiceGuard.EnsureAdmin(i_caller);

        DateTime now = clock.UtcNow;
        DateTime since = VnDate(now).AddDays(-(HocTapConst.STATS_DAYS - 1)) - VN;      // 00:00 giờ VN của ngày đầu, đổi về UTC

        // 1. Tài khoản theo trạng thái hiệu lực (đủ 4 khoá)
        List<AppUserModel> lstParent = unitOfWork.AdminRepo.GetAllParentUser();
        Dictionary<string, int> byStatus = new Dictionary<string, int>();
        byStatus[HocTapConst.STATUS_TRIAL] = 0;
        byStatus[HocTapConst.STATUS_ACTIVE] = 0;
        byStatus[HocTapConst.STATUS_EXPIRED] = 0;
        byStatus[HocTapConst.STATUS_LOCKED] = 0;
        foreach (AppUserModel parent in lstParent)
        {
            string status = AccountRules.EffectiveStatus(parent.status, parent.trialend, parent.planend, now);
            byStatus[status] = byStatus.TryGetValue(status, out int count) ? count + 1 : 1;
        }

        // 2. Số liệu tổng + theo ngày
        _Result.Users = lstParent.Count;
        _Result.ByStatus = byStatus;
        _Result.Students = unitOfWork.AdminRepo.CountActiveStudent();
        _Result.ActiveStudents7d = unitOfWork.AdminRepo.CountStudentHasAttemptSince(now.AddDays(-7));
        _Result.Attempts = unitOfWork.AdminRepo.CountAttempt();
        _Result.Assignments = unitOfWork.AdminRepo.CountAssignment();
        _Result.AttemptsByDay = ByDay(unitOfWork.AdminRepo.GetListAttemptTime(since), since, now);
        _Result.NewUsersByDay = ByDay(lstParent.Select(x => x.timecr).Where(x => x >= since), since, now);
        return _Result;
    }

    // ===== Nội bộ =====

    /// <summary>Gom theo ngày giờ VN, đủ các ngày từ sinceUtc đến nowUtc (ngày không có = 0)</summary>
    public static List<DayCountReadModel> ByDay(IEnumerable<DateTime> i_lstUtcTime, DateTime i_sinceUtc, DateTime i_nowUtc)
    {
        List<DayCountReadModel> lstResult = new List<DayCountReadModel>();
        Dictionary<DateTime, int> counts = i_lstUtcTime.GroupBy(VnDate).ToDictionary(g => g.Key, g => g.Count());
        DateTime first = VnDate(i_sinceUtc);
        DateTime last = VnDate(i_nowUtc);
        for (DateTime day = first; day <= last; day = day.AddDays(1))
        {
            DayCountReadModel item = new DayCountReadModel();
            item.Day = day.ToString("yyyy-MM-dd");
            item.Count = counts.TryGetValue(day, out int count) ? count : 0;
            lstResult.Add(item);
        }
        return lstResult;
    }

    private static DateTime VnDate(DateTime i_utc) => DateTime.SpecifyKind((i_utc + VN).Date, DateTimeKind.Utc);

    private AdminUserReadModel ToReadModel(AdminUserRow i_row)
    {
        AdminUserReadModel read = new AdminUserReadModel();
        read.Id = i_row.User.id;
        read.FullName = i_row.User.fullname;
        read.Email = i_row.User.email;
        read.Phone = i_row.User.phone;
        read.Role = i_row.User.role;
        read.Status = AccountRules.EffectiveStatus(i_row.User.status, i_row.User.trialend, i_row.User.planend, clock.UtcNow);
        read.TrialEnd = i_row.User.trialend;
        read.PlanEnd = i_row.User.planend;
        read.LastLogin = i_row.User.lastlogin;
        read.TimeCr = i_row.User.timecr;
        read.Students = i_row.Students;
        read.Attempts = i_row.Attempts;
        read.LastAttempt = i_row.LastAttempt;
        return read;
    }

    private AppUserModel GetParentUser(Guid i_id)
    {
        AppUserModel? user = unitOfWork.UserRepo.GetUserById(i_id);
        if (user == null || user.role != HocTapConst.ROLE_PARENT) throw new LogicException("Không tìm thấy tài khoản phụ huynh.", 404);
        return user;
    }

    private static string? CleanNote(string? i_note)
    {
        string note = (i_note ?? "").Trim();
        if (note.Length == 0) return null;
        return note.Length > 300 ? note[..300] : note;
    }
}
