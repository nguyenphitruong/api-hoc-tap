using HocTap.Domain.Aggregates.Admin;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Commons;
using HocTap.Infrastructure.Entities;
using HocTap.Infrastructure.MappingRepos;
using HocTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HocTap.Infrastructure.Repositories;

/// <summary>
/// 08/10/2026 - Repository quản trị (giai đoạn 5). Đếm hồ sơ / lượt học bằng truy vấn con (1 lần gọi CSDL);
/// thống kê theo ngày chỉ lấy cột thời gian, Service gom theo ngày giờ Việt Nam.
/// </summary>
public class AdminRepository : IAdminRepository
{
    private readonly HocTapDbContext dbContext;

    public AdminRepository(HocTapDbContext i_context)
    {
        dbContext = i_context;
    }

    public List<AdminUserRow> GetListParentUser(string? i_query)
    {
        IQueryable<app_user> qUser = dbContext.app_user.AsNoTracking().Where(x => x.role == HocTapConst.ROLE_PARENT);
        if (!string.IsNullOrWhiteSpace(i_query))
        {
            string keyword = "%" + i_query.Trim().ToLower() + "%";
            qUser = qUser.Where(x => EF.Functions.Like(x.fullname.ToLower(), keyword)
                                  || EF.Functions.Like(x.email ?? "", keyword)
                                  || EF.Functions.Like(x.phone ?? "", keyword));
        }
        var lstRow = qUser
            .OrderByDescending(x => x.timecr)
            .Select(u => new
            {
                user = u,
                students = dbContext.student_profile.Count(s => s.userid == u.id && s.active),
                attempts = (from a in dbContext.attempt join s in dbContext.student_profile on a.studentid equals s.id where s.userid == u.id select a.id).Count(),
                lastAttempt = (from a in dbContext.attempt join s in dbContext.student_profile on a.studentid equals s.id where s.userid == u.id select (DateTime?)a.finishedat).Max(),
            })
            .ToList();
        List<AdminUserRow> lstResult = new List<AdminUserRow>();
        foreach (var row in lstRow)
        {
            AdminUserRow item = new AdminUserRow();
            item.User = HocTapEntityMapper.ToModel(row.user);
            item.Students = row.students;
            item.Attempts = row.attempts;
            item.LastAttempt = row.lastAttempt;
            lstResult.Add(item);
        }
        return lstResult;
    }

    public List<AppUserModel> GetAllParentUser()
    {
        List<app_user> lstEntity = dbContext.app_user.AsNoTracking().Where(x => x.role == HocTapConst.ROLE_PARENT).ToList();
        List<AppUserModel> lstResult = new List<AppUserModel>();
        foreach (app_user entity in lstEntity)
        {
            lstResult.Add(HocTapEntityMapper.ToModel(entity));
        }
        return lstResult;
    }

    public void InsertPlanLog(PlanLogModel i_log)
    {
        dbContext.plan_log.Add(HocTapEntityMapper.ToEntity(i_log));
    }

    public List<PlanLogRow> GetListPlanLog(Guid i_userId)
    {
        var lstRow = (from l in dbContext.plan_log.AsNoTracking()
                      join a in dbContext.app_user.AsNoTracking() on l.adminid equals a.id into ga
                      from a in ga.DefaultIfEmpty()
                      where l.userid == i_userId
                      orderby l.timecr descending
                      select new { l, name = a == null ? "" : a.fullname })
                     .ToList();
        List<PlanLogRow> lstResult = new List<PlanLogRow>();
        foreach (var row in lstRow)
        {
            PlanLogRow item = new PlanLogRow();
            item.Log = HocTapEntityMapper.ToModel(row.l);
            item.AdminName = row.name;
            lstResult.Add(item);
        }
        return lstResult;
    }

    public int CountActiveStudent()
    {
        return dbContext.student_profile.Count(x => x.active);
    }

    public int CountStudentHasAttemptSince(DateTime i_sinceUtc)
    {
        return dbContext.attempt.Where(x => x.finishedat >= i_sinceUtc).Select(x => x.studentid).Distinct().Count();
    }

    public int CountAttempt()
    {
        return dbContext.attempt.Count();
    }

    public int CountAssignment()
    {
        return dbContext.assignment.Count();
    }

    public List<DateTime> GetListAttemptTime(DateTime i_sinceUtc)
    {
        return dbContext.attempt.AsNoTracking().Where(x => x.finishedat >= i_sinceUtc).Select(x => x.finishedat).ToList();
    }
}
