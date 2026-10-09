using System.Text.Json;
using HocTap.Domain.Aggregates.Assignments;
using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Commons;

namespace HocTap.Tests;

/// <summary>08/10/2026 - Giai đoạn 4: giao bài, nộp bài giao, quá hạn / nộp trễ, đóng bài, giao lại câu sai</summary>
public class AssignmentTests
{
    private record Ctx(Env E, Guid Uid, Guid Minh, Guid Na);

    private static Ctx Setup()
    {
        var e = new Env();
        var uid = e.Auth.Register(new RegisterModel { FullName = "Bố", Email = "b@x.vn", Password = "123456" }).User.Id;
        var minh = e.StudentSvc.InsertStudent(e.Parent(uid), new StudentSaveModel { NickName = "Minh", Grade = 6 }).Id;
        var na = e.StudentSvc.InsertStudent(e.Parent(uid), new StudentSaveModel { NickName = "Na", Grade = 2 }).Id;
        return new Ctx(e, uid, minh, na);
    }

    private static AssignmentCreateModel NewAsg(Ctx c, int days = 2) => new()
    {
        Title = "Ôn tập hợp", Grade = 6, Subject = "toan", TopicIds = new() { "g6.toan.taphop", "g6.toan.ghiso" }, Level = 2,
        QuestionCount = 5, DueDate = c.E.Clock.UtcNow.AddDays(days), StudentIds = new() { c.Minh },
    };

    private static AttemptSaveModel Submit(Ctx c, AssignmentReadModel a, int correct, DateTime at, bool lastWrong = true) => new()
    {
        AssignmentId = a.Id, TopicId = a.TopicKey, Mode = HocTapConst.MODE_TEST, Total = a.QuestionCount, Correct = correct,
        FinishedAt = AttemptRules.ToUnixMs(at),
        Items = Enumerable.Range(0, a.QuestionCount).Select(i => new AttemptItemModel
        {
            Idx = i, Question = JsonDocument.Parse($"{{\"q\":\"Câu {i}\"}}").RootElement, IsCorrect = !(lastWrong && i >= correct),
        }).ToList(),
    };

    [Fact]
    public void Create_Validates()
    {
        var c = Setup();
        var p = c.E.Parent(c.Uid);
        var m = NewAsg(c); m.StudentIds = new() { c.Na };
        Assert.Contains("lớp 2", Assert.Throws<LogicException>(() => c.E.AsgSvc.InsertAssignment(p, m)).Message);
        m = NewAsg(c); m.TopicIds = new() { "g2.toan.x" };
        Assert.Throws<LogicException>(() => c.E.AsgSvc.InsertAssignment(p, m));
        m = NewAsg(c); m.QuestionCount = 30;
        Assert.Throws<LogicException>(() => c.E.AsgSvc.InsertAssignment(p, m));
        m = NewAsg(c, -1);
        Assert.Throws<LogicException>(() => c.E.AsgSvc.InsertAssignment(p, m));
        Assert.Throws<LogicException>(() => c.E.AsgSvc.InsertAssignment(c.E.Student(c.Uid, c.Minh), NewAsg(c)));
        var ok = c.E.AsgSvc.InsertAssignment(p, NewAsg(c));
        Assert.True(ok.Seed > 0);
        Assert.Equal($"as:g6.toan.{ok.Id:N}", ok.TopicKey);
        Assert.Equal(HocTapConst.AS_TODO, ok.Students.Single().Status);
    }

    [Fact]
    public void Submit_KeepsBestScore_AndCounts()
    {
        var c = Setup();
        var a = c.E.AsgSvc.InsertAssignment(c.E.Parent(c.Uid), NewAsg(c));
        var me = c.E.Student(c.Uid, c.Minh);
        var now = c.E.Clock.UtcNow;
        var s1 = Submit(c, a, 4, now); s1.Score10 = 8;
        var s2 = Submit(c, a, 2, now.AddMinutes(5)); s2.Score10 = 4;
        c.E.AttemptSvc.InsertAttempt(me, s1);
        c.E.AttemptSvc.InsertAttempt(me, s2);
        var mine = c.E.AsgSvc.GetListAssignmentOfStudent(me, c.Minh).Single().Mine!;
        Assert.Equal(HocTapConst.AS_DONE, mine.Status);
        Assert.Equal(8m, mine.BestScore);
        Assert.Equal(2, mine.AttemptCount);
        Assert.False(mine.IsLate);
        Assert.Equal(2, c.E.AsgSvc.GetAssignmentResult(c.E.Parent(c.Uid), a.Id).Attempts.Count);
    }

    [Fact]
    public void Submit_WrongKeyOrCount_Rejected()
    {
        var c = Setup();
        var a = c.E.AsgSvc.InsertAssignment(c.E.Parent(c.Uid), NewAsg(c));
        var me = c.E.Student(c.Uid, c.Minh);
        var bad = Submit(c, a, 3, c.E.Clock.UtcNow); bad.Total = 6; bad.Correct = 3;
        Assert.Throws<LogicException>(() => c.E.AttemptSvc.InsertAttempt(me, bad));
        var bad2 = Submit(c, a, 3, c.E.Clock.UtcNow); bad2.TopicId = "as:g6.toan.abc";
        Assert.Throws<LogicException>(() => c.E.AttemptSvc.InsertAttempt(me, bad2));
        var noAsg = Submit(c, a, 3, c.E.Clock.UtcNow); noAsg.AssignmentId = null;
        Assert.Throws<LogicException>(() => c.E.AttemptSvc.InsertAttempt(me, noAsg));
    }

    [Fact]
    public void Overdue_ThenLateSubmit_ThenClosed()
    {
        var c = Setup();
        var p = c.E.Parent(c.Uid);
        var a = c.E.AsgSvc.InsertAssignment(p, NewAsg(c, 1));
        var me = c.E.Student(c.Uid, c.Minh);
        c.E.Clock.UtcNow = c.E.Clock.UtcNow.AddDays(2);
        Assert.Equal(HocTapConst.AS_OVERDUE, c.E.AsgSvc.GetListAssignmentOfStudent(me, c.Minh).Single().Mine!.Status);
        c.E.AttemptSvc.InsertAttempt(me, Submit(c, a, 5, c.E.Clock.UtcNow.AddMinutes(-1)));
        var mine = c.E.AsgSvc.GetListAssignmentOfStudent(me, c.Minh).Single().Mine!;
        Assert.Equal(HocTapConst.AS_DONE, mine.Status);
        Assert.True(mine.IsLate);
        c.E.AsgSvc.UpdateAssignment(p, a.Id, new AssignmentUpdateModel { Status = HocTapConst.ASG_CLOSED });
        Assert.Empty(c.E.AsgSvc.GetListAssignmentOfStudent(me, c.Minh));
        Assert.Contains("đã đóng", Assert.Throws<LogicException>(() => c.E.AttemptSvc.InsertAttempt(me, Submit(c, a, 5, c.E.Clock.UtcNow))).Message);
    }

    [Fact]
    public void RetryWrong_CopiesWrongQuestions()
    {
        var c = Setup();
        var p = c.E.Parent(c.Uid);
        var a = c.E.AsgSvc.InsertAssignment(p, NewAsg(c));
        Assert.Throws<LogicException>(() => c.E.AsgSvc.InsertRetryWrong(p, a.Id, new AssignmentRetryModel { StudentId = c.Minh }));
        c.E.AttemptSvc.InsertAttempt(c.E.Student(c.Uid, c.Minh), Submit(c, a, 3, c.E.Clock.UtcNow));   // câu 3, 4 sai
        var r = c.E.AsgSvc.InsertRetryWrong(p, a.Id, new AssignmentRetryModel { StudentId = c.Minh });
        Assert.Equal(2, r.QuestionCount);
        Assert.True(r.HasQuestions);
        Assert.Equal(a.Id, r.SourceId);
        var detail = c.E.AsgSvc.GetAssignmentDetail(c.E.Student(c.Uid, c.Minh), r.Id);
        Assert.Equal("Câu 3", detail.Questions!.Value[0].GetProperty("q").GetString());
    }

    [Fact]
    public void Rules_EffectiveStatus()
    {
        var now = new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal("CHUALAM", AssignmentRules.EffectiveStatus("CHUALAM", now.AddHours(1), now));
        Assert.Equal("QUAHAN", AssignmentRules.EffectiveStatus("CHUALAM", now.AddHours(-1), now));
        Assert.Equal("DANOP", AssignmentRules.EffectiveStatus("DANOP", now.AddHours(-1), now));
    }
}
