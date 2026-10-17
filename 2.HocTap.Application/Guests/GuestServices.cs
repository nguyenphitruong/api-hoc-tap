using HocTap.Application.Commons;
using HocTap.Domain.Aggregates.Guests;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;

namespace HocTap.Application.Guests;

/// <summary>17/10/2026 - Service khách học thử: ghi hoạt động (không cần đăng nhập), gắn tài khoản, thống kê cho admin</summary>
public interface IGuestServices
{
    /// <summary>Ghi 1 hoạt động; trả false khi mã khách đã vượt giới hạn trong ngày (bỏ qua, không báo lỗi)</summary>
    bool Track(GuestEventModel i_event);
    /// <summary>Gắn tài khoản cho các hoạt động chưa gắn của mã khách (gọi sau khi đăng ký / đăng nhập thành công); trả số dòng đã gắn</summary>
    int LinkGuest(string? i_guestId, Guid i_userId, string i_via);
    AdminGuestStatsReadModel GetAdminStats(CurrentUser i_caller, int i_days);
}

/// <summary>
/// 17/10/2026 - KHÁCH HỌC THỬ (viết tường minh theo mẫu Web-MultiClinic)
/// - Track: GuestRules.ToModel kiểm tra + chuẩn hoá; quá GuestRules.MAX_PER_DAY hoạt động / mã / ngày -> bỏ qua.
/// - LinkGuest: mã khách sai / trống -> 0, không lỗi (đăng ký / đăng nhập không bao giờ hỏng vì phần này).
/// - GetAdminStats: chỉ ADMIN; đọc hoạt động của các mã có mặt trong kỳ rồi tổng hợp bằng GuestStats.Build (hàm thuần).
/// Mỗi thao tác ghi: InitTransaction -> Insert / Link -> Save (1 lần) -> CommitTransaction; lỗi -> RollbackTransaction.
/// </summary>
public class GuestServices : IGuestServices
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public GuestServices(IUnitOfWork i_UnitOfWork, IClock i_Clock)
    {
        unitOfWork = i_UnitOfWork;
        clock = i_Clock;
    }

    public bool Track(GuestEventModel i_event)
    {
        // 1. Kiểm tra + chuẩn hoá
        GuestActivityModel activity = GuestRules.ToModel(i_event);
        DateTime now = clock.UtcNow;
        if (unitOfWork.GuestRepo.CountActivitySince(activity.guestid, now.AddDays(-1)) >= GuestRules.MAX_PER_DAY) return false;

        // 2. Ghi
        activity.id = Guid.NewGuid();
        activity.timecr = now;
        try
        {
            unitOfWork.InitTransaction();
            unitOfWork.GuestRepo.InsertActivity(activity);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
        return true;
    }

    public int LinkGuest(string? i_guestId, Guid i_userId, string i_via)
    {
        string? guestId = GuestRules.NormalizeGuestId(i_guestId);
        if (guestId == null) return 0;
        DateTime now = clock.UtcNow;
        int n;
        try
        {
            unitOfWork.InitTransaction();
            n = unitOfWork.GuestRepo.LinkGuest(guestId, i_userId, i_via, now, now.AddDays(-GuestRules.LINK_DAYS));
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
        return n;
    }

    public AdminGuestStatsReadModel GetAdminStats(CurrentUser i_caller, int i_days)
    {
        ServiceGuard.EnsureAdmin(i_caller);
        DateTime now = clock.UtcNow;
        int days = Math.Clamp(i_days <= 0 ? 30 : i_days, 1, 90);
        List<GuestActivityModel> lstRow = unitOfWork.GuestRepo.GetListActivitySince(GuestStats.FromUtc(now, days), GuestRules.MAX_STAT_ROWS);
        return GuestStats.Build(lstRow, now, days, id => unitOfWork.UserRepo.GetUserById(id));
    }
}
