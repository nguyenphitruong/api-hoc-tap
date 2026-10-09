using System.Text.Json;
using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Commons;

namespace HocTap.Tests;

/// <summary>08/10/2026 - Test service: đăng ký, đăng nhập, refresh xoay vòng, hồ sơ con, phiên học sinh, nộp / nhập lượt học</summary>
public class ServiceTests
{
    private static AuthResultReadModel Register(Env e, string email = "a@x.vn") =>
        e.Auth.Register(new RegisterModel { FullName = "Phụ huynh A", Email = email, Password = "123456" });

    private static AttemptSaveModel Practice(Env e, string topic = "g6.toan.taphop", long? at = null) => new()
    {
        TopicId = topic, Mode = HocTapConst.MODE_PRACTICE, Level = 1, Total = 10, Correct = 7,
        FinishedAt = at ?? AttemptRules.ToUnixMs(e.Clock.UtcNow.AddMinutes(-1)),
    };

    [Fact]
    public void Register_StartsTrial14Days_AndIssuesTokens()
    {
        var e = new Env();
        var r = Register(e);
        Assert.Equal(HocTapConst.STATUS_TRIAL, r.User.Status);
        Assert.Equal(e.Clock.UtcNow.AddDays(14), r.User.TrialEnd);
        Assert.True(r.User.CanStudy);
        Assert.False(string.IsNullOrEmpty(r.AccessToken));
        Assert.Single(e.Users.Tokens);
        Assert.NotEqual(r.RefreshToken, e.Users.Tokens[0].tokenhash);   // chỉ lưu hash
    }

    [Fact]
    public void Register_Duplicate_Throws()
    {
        var e = new Env();
        Register(e);
        var ex = Assert.Throws<LogicException>(() => Register(e, "A@X.VN"));
        Assert.Contains("đã được đăng ký", ex.Message);
    }

    [Fact]
    public void Login_WrongPassword_And_Locked()
    {
        var e = new Env();
        Register(e);
        Assert.Throws<LogicException>(() => e.Auth.Login(new LoginModel { Login = "a@x.vn", Password = "sai" }));
        e.Users.Users[0].status = HocTapConst.STATUS_LOCKED;
        var ex = Assert.Throws<LogicException>(() => e.Auth.Login(new LoginModel { Login = "a@x.vn", Password = "123456" }));
        Assert.Equal(403, ex.ErrorCode);
    }

    [Fact]
    public void Refresh_Rotates_OldTokenRejected()
    {
        var e = new Env();
        var r = Register(e);
        var r2 = e.Auth.Refresh(new RefreshModel { RefreshToken = r.RefreshToken });
        Assert.NotEqual(r.RefreshToken, r2.RefreshToken);
        var ex = Assert.Throws<LogicException>(() => e.Auth.Refresh(new RefreshModel { RefreshToken = r.RefreshToken }));
        Assert.Equal(401, ex.ErrorCode);
    }

    [Fact]
    public void Students_Max4_AndPinRequired()
    {
        var e = new Env();
        var uid = Register(e).User.Id;
        var p = e.Parent(uid);
        for (var i = 0; i < 4; i++) e.StudentSvc.InsertStudent(p, new StudentSaveModel { NickName = "Bé " + i, Grade = 2, Pin = i == 0 ? "1234" : null });
        Assert.Throws<LogicException>(() => e.StudentSvc.InsertStudent(p, new StudentSaveModel { NickName = "Bé 5", Grade = 6 }));

        var withPin = e.Students.Rows[0];
        Assert.Throws<LogicException>(() => e.Auth.StudentSession(p, new StudentSessionModel { StudentId = withPin.id, Pin = "0000" }));
        var ss = e.Auth.StudentSession(p, new StudentSessionModel { StudentId = withPin.id, Pin = "1234" });
        Assert.Equal(withPin.id, ss.Student!.Id);
    }

    [Fact]
    public void Students_OtherAccount_NotFound()
    {
        var e = new Env();
        var a = Register(e).User.Id;
        var b = Register(e, "b@x.vn").User.Id;
        var st = e.StudentSvc.InsertStudent(e.Parent(a), new StudentSaveModel { NickName = "Na", Grade = 2 });
        var ex = Assert.Throws<LogicException>(() => HocTap.Application.Commons.ServiceGuard.GetOwnedStudent(e.Uow, e.Parent(b), st.Id));
        Assert.Equal(404, ex.ErrorCode);
        Assert.Throws<LogicException>(() => e.Auth.StudentSession(e.Parent(b), new StudentSessionModel { StudentId = st.Id }));
    }

    [Fact]
    public void SaveAttempt_StudentOnly_GradeChecked_TrialExpiredBlocked()
    {
        var e = new Env();
        var uid = Register(e).User.Id;
        var st = e.StudentSvc.InsertStudent(e.Parent(uid), new StudentSaveModel { NickName = "Minh", Grade = 6 });
        var me = e.Student(uid, st.Id);

        Assert.Equal(403, Assert.Throws<LogicException>(() => e.AttemptSvc.InsertAttempt(e.Parent(uid), Practice(e))).ErrorCode);
        Assert.Throws<LogicException>(() => e.AttemptSvc.InsertAttempt(me, Practice(e, "g2.toan.abc")));

        var saved = e.AttemptSvc.InsertAttempt(me, Practice(e));
        Assert.Equal("g6.toan.taphop", saved.TopicId);
        Assert.Null(saved.Score10);

        e.Clock.UtcNow = e.Clock.UtcNow.AddDays(15);
        var ex = Assert.Throws<LogicException>(() => e.AttemptSvc.InsertAttempt(me, Practice(e)));
        Assert.Equal(403, ex.ErrorCode);
        Assert.Single(e.AttemptSvc.GetListProgress(me, st.Id));   // hết hạn vẫn xem được tiến độ
    }

    [Fact]
    public void SaveTest_ComputesScore_StoresItems()
    {
        var e = new Env();
        var uid = Register(e).User.Id;
        var st = e.StudentSvc.InsertStudent(e.Parent(uid), new StudentSaveModel { NickName = "Minh", Grade = 6 });
        var m = Practice(e, "hk:g6.toan.1");
        m.Mode = HocTapConst.MODE_TEST; m.Total = 4; m.Correct = 3; m.Level = null;
        m.Items = new List<AttemptItemModel>
        {
            new() { Idx = 0, Question = JsonDocument.Parse("{\"q\":\"1+1\",\"ans\":\"2\"}").RootElement, Answer = JsonDocument.Parse("{\"val\":\"2\"}").RootElement, IsCorrect = true },
        };
        var r = e.AttemptSvc.InsertAttempt(e.Student(uid, st.Id), m);
        Assert.Equal(7.5m, r.Score10);
        Assert.True(r.HasItems);
        Assert.Equal("{\"q\":\"1+1\",\"ans\":\"2\"}", e.Attempts.Rows[0].items[0].question);
    }

    [Fact]
    public void Import_SkipsInvalidOtherGradeAndDuplicates()
    {
        var e = new Env();
        var uid = Register(e).User.Id;
        var st = e.StudentSvc.InsertStudent(e.Parent(uid), new StudentSaveModel { NickName = "Minh", Grade = 6 });
        const long at = 1780000000000;
        var list = new List<AttemptSaveModel>
        {
            Practice(e, "g6.toan.taphop", at), Practice(e, "g6.toan.taphop", at), Practice(e, "g2.toan.x", at), Practice(e, "bad", at),
        };
        var r = e.AttemptSvc.ImportAttempt(e.Parent(uid), st.Id, new ImportModel { Attempts = list });
        Assert.Equal(1, r.Added);
        Assert.Equal(3, r.Skipped);
        var r2 = e.AttemptSvc.ImportAttempt(e.Parent(uid), st.Id, new ImportModel { Attempts = new() { Practice(e, "g6.toan.taphop", at) } });
        Assert.Equal(0, r2.Added);
        Assert.Throws<LogicException>(() => e.AttemptSvc.ImportAttempt(e.Student(uid, st.Id), st.Id, new ImportModel { Attempts = list }));
    }
}
