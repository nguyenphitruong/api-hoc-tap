using HocTap.Domain.Aggregates.Guests;
using HocTap.Infrastructure.Entities;
using HocTap.Infrastructure.MappingRepos;
using HocTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HocTap.Infrastructure.Repositories;

/// <summary>
/// 17/10/2026 - Repository hoạt động khách học thử (guest_activity).
/// Đọc AsNoTracking -> Model; ghi qua DbContext (Service gọi unitOfWork.Save()).
/// </summary>
public class GuestRepository : IGuestRepository
{
    private readonly HocTapDbContext dbContext;

    public GuestRepository(HocTapDbContext i_dbContext)
    {
        dbContext = i_dbContext;
    }

    public void InsertActivity(GuestActivityModel i_activity)
    {
        dbContext.guest_activity.Add(HocTapEntityMapper.ToEntity(i_activity));
    }

    public int CountActivitySince(string i_guestId, DateTime i_since)
    {
        return dbContext.guest_activity.AsNoTracking().Count(x => x.guestid == i_guestId && x.timecr >= i_since);
    }

    /// <summary>Đọc các dòng chưa gắn (có theo dõi thay đổi) rồi gán tài khoản — số dòng 1 mã khách nhỏ (≤ 300 / ngày)</summary>
    public int LinkGuest(string i_guestId, Guid i_userId, string i_via, DateTime i_now, DateTime i_since)
    {
        List<guest_activity> lstRow = dbContext.guest_activity
            .Where(x => x.guestid == i_guestId && x.userid == null && x.timecr >= i_since).ToList();
        foreach (guest_activity row in lstRow)
        {
            row.userid = i_userId;
            row.linkvia = i_via;
            row.linkedat = i_now;
        }
        return lstRow.Count;
    }

    public List<GuestActivityModel> GetListActivitySince(DateTime i_since, int i_max)
    {
        IQueryable<string> ids = dbContext.guest_activity.Where(x => x.timecr >= i_since).Select(x => x.guestid).Distinct();
        return dbContext.guest_activity.AsNoTracking()
            .Where(x => ids.Contains(x.guestid))
            .OrderByDescending(x => x.timecr).Take(i_max).ToList()
            .Select(HocTapEntityMapper.ToModel).ToList();
    }
}
