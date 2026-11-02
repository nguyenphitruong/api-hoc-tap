using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Feedbacks;
using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Commons;

namespace HocTap.Tests;

/// <summary>18/10/2026 - Góp ý / báo lỗi câu hỏi: kiểm tra dữ liệu, người gửi, giới hạn, quyền admin, lọc + tổng hợp, cập nhật</summary>
public class FeedbackTests
{
    private const string G1 = "g_aaaaaaaaaaaaaaaaaaaa";
    private static SubmitFeedbackModel Fb(string kind = "NOIDUNG", string content = "Bài học rất hay, con thích lắm.", short? rating = 5) =>
        new SubmitFeedbackModel { Kind = kind, Content = content, Rating = rating, Grade = 6, Subject = "toan", Page = "/phu-huynh" };

    private static Guid NewParent(Env e, string email = "p@x.vn") =>
        e.Auth.Register(new RegisterModel { FullName = "Phụ huynh C", Email = email, Phone = "0909000111", Password = "123456", AcceptTerms = true }).User.Id;

    [Fact]
    public void Rules_validate()
    {
        Assert.Throws<LogicException>(() => FeedbackRules.ToModel(Fb(kind: "XYZ")));
        Assert.Throws<LogicException>(() => FeedbackRules.ToModel(Fb(content: "ngắn")));
        Assert.Throws<LogicException>(() => FeedbackRules.ToModel(Fb(content: new string('a', 1001))));
        Assert.Throws<LogicException>(() => FeedbackRules.ToModel(Fb(rating: 6)));
        // báo lỗi đề: nội dung ngắn được, nhưng phải kèm câu hỏi hoặc mã chủ đề
        Assert.Throws<LogicException>(() => FeedbackRules.ToModel(new SubmitFeedbackModel { Kind = "LOIDE", Content = "sai đáp án" }));
        var m = FeedbackRules.ToModel(new SubmitFeedbackModel { Kind = "loide", Content = "sai", TopicId = "G6.TOAN.UCLN", Question = "{\"q\":\"Tìm ƯCLN(60, 75)\",\"ans\":15}", Grade = 1, Subject = "van" });
        Assert.Equal("LOIDE", m.kind);
        Assert.Equal("g6.toan.ucln", m.topicid);
        Assert.NotNull(m.question);
        Assert.Null(m.grade);
        Assert.Null(m.subject);
        Assert.Equal(FeedbackRules.STATUS_NEW, m.status);
        Assert.Null(FeedbackRules.NormalizeQuestion("[1,2]"));
        Assert.Null(FeedbackRules.NormalizeQuestion("{khong phai json"));
    }

    [Fact]
    public void Guest_submit_keeps_contact_only_when_allowed()
    {
        var e = new Env();
        e.FeedbackSvc.Submit(null, new SubmitFeedbackModel { Kind = "TINHNANG", Content = "Nên có thêm môn Khoa học.", ContactOk = true, Contact = "0912 345 678", GuestId = G1 });
        e.FeedbackSvc.Submit(null, new SubmitFeedbackModel { Kind = "KHAC", Content = "Web chạy hơi chậm buổi tối.", ContactOk = false, Contact = "0912 000 000" });
        var a = e.Uow.Feedback.Rows[0];
        Assert.Null(a.userid);
        Assert.Equal(G1, a.guestid);
        Assert.Equal("0912 345 678", a.contact);
        Assert.Null(e.Uow.Feedback.Rows[1].contact);
        Assert.Null(e.Uow.Feedback.Rows[1].guestid);
    }

    [Fact]
    public void Parent_and_student_sender()
    {
        var e = new Env();
        var uid = NewParent(e);
        var sid = e.StudentSvc.InsertStudent(e.Parent(uid), new StudentSaveModel { NickName = "Bin", Grade = 6 }).Id;
        e.FeedbackSvc.Submit(e.Parent(uid), new SubmitFeedbackModel { Kind = "GIA", Content = "Giá hợp lý, mong có gói gia đình.", ContactOk = true, Contact = "bỏ qua" });
        e.FeedbackSvc.Submit(e.Student(uid, sid), new SubmitFeedbackModel { Kind = "LOIDE", Content = "đáp án sai", TopicId = "g6.toan.ucln" });
        var list = e.FeedbackSvc.GetListFeedback(e.AdminUser(Guid.NewGuid()), new AdminFeedbackQuery());
        var parent = list.Items.Single(x => x.Kind == "GIA");
        Assert.Equal(FeedbackRules.SENDER_PARENT, parent.Sender);
        Assert.Equal("Phụ huynh C", parent.UserName);
        Assert.Equal("0909000111", parent.Contact);           // liên hệ của tài khoản, không lấy chuỗi tự điền
        var kid = list.Items.Single(x => x.Kind == "LOIDE");
        Assert.Equal(FeedbackRules.SENDER_STUDENT, kid.Sender);
        Assert.Equal("Bin", kid.StudentName);
        Assert.Null(kid.Contact);                             // không đồng ý liên hệ
    }

    [Fact]
    public void Daily_cap_per_account()
    {
        var e = new Env();
        var uid = NewParent(e);
        for (int i = 0; i < FeedbackRules.MAX_PER_DAY; i++) e.FeedbackSvc.Submit(e.Parent(uid), Fb());
        Assert.Equal(429, Assert.Throws<LogicException>(() => e.FeedbackSvc.Submit(e.Parent(uid), Fb())).ErrorCode);
        e.Clock.UtcNow = e.Clock.UtcNow.AddDays(1).AddMinutes(1);
        e.FeedbackSvc.Submit(e.Parent(uid), Fb());
    }

    [Fact]
    public void Admin_only_list_and_update()
    {
        var e = new Env();
        var uid = NewParent(e);
        var id = e.FeedbackSvc.Submit(e.Parent(uid), Fb());
        Assert.Equal(403, Assert.Throws<LogicException>(() => e.FeedbackSvc.GetListFeedback(e.Parent(uid), new AdminFeedbackQuery())).ErrorCode);
        Assert.Equal(403, Assert.Throws<LogicException>(() => e.FeedbackSvc.UpdateFeedback(e.Parent(uid), id, new UpdateFeedbackModel { Status = "XONG" })).ErrorCode);
        var admin = e.AdminUser(Guid.NewGuid());
        Assert.Equal(404, Assert.Throws<LogicException>(() => e.FeedbackSvc.UpdateFeedback(admin, Guid.NewGuid(), new UpdateFeedbackModel { Status = "XONG" })).ErrorCode);
        Assert.Throws<LogicException>(() => e.FeedbackSvc.UpdateFeedback(admin, id, new UpdateFeedbackModel { Status = "LAM" }));
        var r = e.FeedbackSvc.UpdateFeedback(admin, id, new UpdateFeedbackModel { Status = "dangxl", AdminNote = "  Gọi lại thứ 2  " });
        Assert.Equal("DANGXL", r.Status);
        Assert.Equal("Gọi lại thứ 2", r.AdminNote);
        Assert.NotNull(r.TimeUp);
        // chỉ đổi ghi chú: trạng thái giữ nguyên; ghi chú trống -> xoá
        r = e.FeedbackSvc.UpdateFeedback(admin, id, new UpdateFeedbackModel { AdminNote = "" });
        Assert.Equal("DANGXL", r.Status);
        Assert.Null(r.AdminNote);
    }

    [Fact]
    public void Admin_filter_summary_paging()
    {
        var e = new Env();
        var admin = e.AdminUser(Guid.NewGuid());
        e.FeedbackSvc.Submit(null, Fb(rating: 4));
        e.FeedbackSvc.Submit(null, Fb(rating: 5, content: "Giao diện dễ dùng, chữ to rõ."));
        var err = e.FeedbackSvc.Submit(null, new SubmitFeedbackModel { Kind = "LOIDE", Content = "câu 3 sai", TopicId = "g2.toan.cong" });
        e.Clock.UtcNow = e.Clock.UtcNow.AddDays(10);
        e.FeedbackSvc.Submit(null, Fb(kind: "GIAODIEN", rating: null, content: "Nút bấm hơi nhỏ trên điện thoại."));
        e.FeedbackSvc.UpdateFeedback(admin, err, new UpdateFeedbackModel { Status = "DANGXL" });

        var all = e.FeedbackSvc.GetListFeedback(admin, new AdminFeedbackQuery());
        Assert.Equal(4, all.Total);
        Assert.Equal(4, all.Summary.Total);
        Assert.Equal(3, all.Summary.New);
        Assert.Equal(1, all.Summary.InProgress);
        Assert.Equal(1, all.Summary.OpenErrors);
        Assert.Equal(4.5m, all.Summary.AvgRating);
        Assert.Equal(1, all.Summary.Last7Days);
        Assert.Equal("GIAODIEN", all.Items[0].Kind);          // mới nhất trước
        Assert.Equal(1, e.FeedbackSvc.GetListFeedback(admin, new AdminFeedbackQuery { Kind = "loide" }).Total);
        Assert.Equal(1, e.FeedbackSvc.GetListFeedback(admin, new AdminFeedbackQuery { Status = "DANGXL" }).Total);
        Assert.Equal(1, e.FeedbackSvc.GetListFeedback(admin, new AdminFeedbackQuery { Q = "g2.toan" }).Total);
        Assert.Equal(1, e.FeedbackSvc.GetListFeedback(admin, new AdminFeedbackQuery { Q = "GIAO DIỆN" }).Total);
        var p2 = e.FeedbackSvc.GetListFeedback(admin, new AdminFeedbackQuery { Page = 2, PageSize = 1 });   // pageSize tối thiểu 10
        Assert.Empty(p2.Items);
        Assert.All(all.Items.Where(x => x.Sender == FeedbackRules.SENDER_GUEST), x => Assert.Null(x.UserName));
    }
}
