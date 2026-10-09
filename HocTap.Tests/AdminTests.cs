using HocTap.Domain.Aggregates.Admin;
using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Commons;

namespace HocTap.Tests;

/// <summary>08/10/2026 - Giai đoạn 5: quản trị (gói, dùng thử, khoá / mở khoá, thống kê) và đổi mật khẩu</summary>
public class AdminTests
{
    private static (Env E, Guid Uid, Guid AdminId) Setup()
    {
        var e = new Env();
        var uid = e.Auth.Register(new RegisterModel { FullName = "Phụ huynh A", Email = "a@x.vn", Password = "123456" }).User.Id;
        return (e, uid, Guid.NewGuid());
    }

    [Fact]
    public void OnlyAdmin()
    {
        var (e, uid, _) = Setup();
        Assert.Equal(403, Assert.Throws<LogicException>(() => e.AdminSvc.GetListUser(e.Parent(uid), new AdminUserQuery())).ErrorCode);
    }

    [Fact]
    public void Activate_ExtendsFromPlanEnd_AndLogs()
    {
        var (e, uid, aid) = Setup();
        var adm = e.AdminUser(aid);
        var now = e.Clock.UtcNow;
        var r = e.AdminSvc.UpdatePlan(adm, uid, new AdminPlanModel { Action = HocTapConst.PLAN_ACTIVATE, Months = 1, Note = "CK 50k" });
        Assert.Equal(HocTapConst.STATUS_ACTIVE, r.Status);
        Assert.Equal(now.AddMonths(1), r.PlanEnd);
        r = e.AdminSvc.UpdatePlan(adm, uid, new AdminPlanModel { Action = HocTapConst.PLAN_ACTIVATE, Months = 3 });
        Assert.Equal(now.AddMonths(1).AddMonths(3), r.PlanEnd);           // cộng nối tiếp
        Assert.Equal(2, e.AdminSvc.GetListPlanLog(adm, uid).Count);
        Assert.Throws<LogicException>(() => e.AdminSvc.UpdatePlan(adm, uid, new AdminPlanModel { Action = HocTapConst.PLAN_ACTIVATE, Months = 0 }));
    }

    [Fact]
    public void ExtendTrial_AfterExpired()
    {
        var (e, uid, aid) = Setup();
        e.Clock.UtcNow = e.Clock.UtcNow.AddDays(20);
        Assert.Equal(HocTapConst.STATUS_EXPIRED, e.AdminSvc.GetListUser(e.AdminUser(aid), new AdminUserQuery()).Items.Single().Status);
        var r = e.AdminSvc.UpdatePlan(e.AdminUser(aid), uid, new AdminPlanModel { Action = HocTapConst.PLAN_TRIAL, Days = 7 });
        Assert.Equal(HocTapConst.STATUS_TRIAL, r.Status);
        Assert.Equal(e.Clock.UtcNow.AddDays(7), r.TrialEnd);
    }

    [Fact]
    public void Lock_RevokesSessions_BlocksLogin_Unlock()
    {
        var (e, uid, aid) = Setup();
        var adm = e.AdminUser(aid);
        var r = e.AdminSvc.UpdateLock(adm, uid, new AdminLockModel { Lock = true, Note = "spam" });
        Assert.Equal(HocTapConst.STATUS_LOCKED, r.Status);
        Assert.All(e.Users.Tokens, t => Assert.True(t.revoked));
        Assert.Throws<LogicException>(() => e.Auth.Login(new LoginModel { Login = "a@x.vn", Password = "123456" }));
        Assert.Throws<LogicException>(() => e.AdminSvc.UpdatePlan(adm, uid, new AdminPlanModel { Action = HocTapConst.PLAN_TRIAL, Days = 3 }));
        r = e.AdminSvc.UpdateLock(adm, uid, new AdminLockModel { Lock = false });
        Assert.Equal(HocTapConst.STATUS_TRIAL, r.Status);
        Assert.NotNull(e.Auth.Login(new LoginModel { Login = "a@x.vn", Password = "123456" }).AccessToken);
    }

    [Fact]
    public void Stats_CountsByStatusAndDay()
    {
        var (e, uid, aid) = Setup();
        var st = e.StudentSvc.InsertStudent(e.Parent(uid), new StudentSaveModel { NickName = "Minh", Grade = 6 });
        e.AttemptSvc.InsertAttempt(e.Student(uid, st.Id), new AttemptSaveModel
        {
            TopicId = "g6.toan.taphop", Mode = HocTapConst.MODE_PRACTICE, Total = 10, Correct = 5,
            FinishedAt = AttemptRules.ToUnixMs(e.Clock.UtcNow.AddMinutes(-5)),
        });
        var s = e.AdminSvc.GetStats(e.AdminUser(aid));
        Assert.Equal(1, s.Users);
        Assert.Equal(1, s.ByStatus[HocTapConst.STATUS_TRIAL]);
        Assert.Equal(0, s.ByStatus[HocTapConst.STATUS_LOCKED]);
        Assert.Equal(HocTapConst.STATS_DAYS, s.AttemptsByDay.Count);
        Assert.Equal(1, s.AttemptsByDay.Sum(x => x.Count));
        Assert.Equal(1, s.ActiveStudents7d);
    }

    [Fact]
    public void ChangePassword_RevokesSessions()
    {
        var (e, uid, _) = Setup();
        Assert.Throws<LogicException>(() => e.Auth.ChangePassword(e.Parent(uid), new ChangePasswordModel { OldPassword = "sai", NewPassword = "abcdef" }));
        e.Auth.ChangePassword(e.Parent(uid), new ChangePasswordModel { OldPassword = "123456", NewPassword = "abcdef" });
        Assert.All(e.Users.Tokens, t => Assert.True(t.revoked));
        Assert.NotNull(e.Auth.Login(new LoginModel { Login = "a@x.vn", Password = "abcdef" }).AccessToken);
    }
}
