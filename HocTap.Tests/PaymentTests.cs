using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Aggregates.Payments;
using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Commons;

namespace HocTap.Tests;

/// <summary>12/10/2026 - Điều khoản khi đăng ký + gia hạn bằng chuyển khoản QR (đơn, webhook, admin xác nhận). 13/10/2026: gói theo con × môn</summary>
public class PaymentTests
{
    private static Guid NewParent(Env e) =>
        e.Auth.Register(new RegisterModel { FullName = "Phụ huynh A", Email = "a@x.vn", Password = "123456", AcceptTerms = true }).User.Id;

    private static BankTransactionModel Txn(string content, decimal amount, string id = "1001") =>
        new BankTransactionModel { Gateway = "SEPAY", TransactionId = id, Content = content, Amount = amount, TransferType = "in" };

    [Fact]
    public void Register_requires_terms_and_stores_version()
    {
        var e = new Env();
        var ex = Assert.Throws<LogicException>(() => e.Auth.Register(new RegisterModel { FullName = "PH", Email = "x@x.vn", Password = "123456" }));
        Assert.Contains("Điều khoản", ex.Message);
        var uid = NewParent(e);
        var u = e.Users.GetUserById(uid)!;
        Assert.Equal(HocTapConst.TERMS_VERSION, u.termsversion);
        Assert.NotNull(u.termsacceptedat);
    }

    [Fact]
    public void Code_finder_handles_bank_noise()
    {
        var code = PaymentRules.NewCode(new Random(1));
        Assert.Matches("^HT[A-Z2-9]{8}$", code);
        Assert.Contains(code, PaymentRules.FindCodes($"MBVCB.123456.{code.ToLower()}.CT tu 0123 NGUYEN VAN A"));
        Assert.Contains(code, PaymentRules.FindCodes($"chuyen tien {code[..5]} {code[5..]}"));
        Assert.Empty(PaymentRules.FindCodes("chuyen tien hoc phi"));
    }

    // 13/10/2026: đơn gia hạn cho 1 con × môn (gói theo số môn)
    private static Guid NewStudent(Env e, Guid uid, string nick = "Minh", short grade = 6) =>
        e.StudentSvc.InsertStudent(e.Parent(uid), new StudentSaveModel { NickName = nick, Grade = grade }).Id;

    private static CreatePaymentOrderModel Order(string plan, Guid sid, params string[] subjects) =>
        new CreatePaymentOrderModel { PlanCode = plan, StudentId = sid, Subjects = subjects.ToList() };

    [Fact]
    public void Create_order_returns_qr_and_reuses_pending()
    {
        var e = new Env();
        var uid = NewParent(e);
        var sid = NewStudent(e, uid);
        var o1 = e.PaySvc.CreateOrder(e.Parent(uid), Order("3T2", sid, "toan", "anh"));
        Assert.Equal(159000, o1.Amount);
        Assert.Equal(HocTapConst.PAY_PENDING, o1.Status);
        Assert.Equal(new List<string> { "anh", "toan" }, o1.Subjects);
        Assert.Equal("Minh", o1.StudentName);
        Assert.Contains("img.vietqr.io/image/970436-0123456789", o1.QrUrl);
        Assert.Contains("amount=159000", o1.QrUrl);
        Assert.Contains(o1.Code, o1.QrUrl);
        // cùng gói + con + môn -> dùng lại; khác môn -> đơn mới
        Assert.Equal(o1.Id, e.PaySvc.CreateOrder(e.Parent(uid), Order("3T2", sid, "anh", "toan")).Id);
        var o3 = e.PaySvc.CreateOrder(e.Parent(uid), Order("1T1", sid, "anh"));
        Assert.NotEqual(o1.Id, o3.Id);
        Assert.NotEqual(o3.Id, e.PaySvc.CreateOrder(e.Parent(uid), Order("1T1", sid, "toan")).Id);
        // gói sai / số môn sai / thiếu con / con của tài khoản khác
        Assert.Throws<LogicException>(() => e.PaySvc.CreateOrder(e.Parent(uid), Order("XX", sid, "toan")));
        Assert.Throws<LogicException>(() => e.PaySvc.CreateOrder(e.Parent(uid), Order("1T1", sid, "toan", "anh")));
        Assert.Throws<LogicException>(() => e.PaySvc.CreateOrder(e.Parent(uid), Order("3T2", sid, "toan")));
        Assert.Throws<LogicException>(() => e.PaySvc.CreateOrder(e.Parent(uid), new CreatePaymentOrderModel { PlanCode = "1T1", Subjects = new() { "toan" } }));
        var other = e.Auth.Register(new RegisterModel { FullName = "PH B", Email = "b@x.vn", Password = "123456", AcceptTerms = true }).User.Id;
        Assert.ThrowsAny<LogicException>(() => e.PaySvc.CreateOrder(e.Parent(other), Order("1T1", sid, "toan")));
        // quá 24 giờ -> đơn mới
        e.Clock.UtcNow = e.Clock.UtcNow.AddHours(25);
        Assert.Equal(HocTapConst.PAY_EXPIRED, e.PaySvc.GetOrder(e.Parent(uid), o1.Id).Status);
        Assert.NotEqual(o1.Id, e.PaySvc.CreateOrder(e.Parent(uid), Order("3T2", sid, "toan", "anh")).Id);
    }

    [Fact]
    public void Webhook_extends_student_subject_once_and_appends()
    {
        var e = new Env();
        var uid = NewParent(e);
        var sid = NewStudent(e, uid);
        var na = NewStudent(e, uid, "Na", 2);
        var now = e.Clock.UtcNow;
        var order = e.PaySvc.CreateOrder(e.Parent(uid), Order("1T1", sid, "toan"));

        Assert.Throws<LogicException>(() => e.PaySvc.HandleBankTransaction("Apikey sai", Txn(order.Code, 39000)));
        var r = e.PaySvc.HandleBankTransaction("Apikey key-123", Txn("IBFT " + order.Code + " thanh toan", 39000));
        Assert.True(r.Matched);

        // chỉ Minh × Toán được cộng 1 tháng
        var plan = Assert.Single(e.Uow.Payments.Plans);
        Assert.Equal((sid, "toan", now.AddMonths(1)), (plan.studentid, plan.subject, plan.planend));
        var u = e.Users.GetUserById(uid)!;
        Assert.Equal(HocTapConst.STATUS_ACTIVE, u.status);
        Assert.Equal(now.AddMonths(1), u.planend);
        var paid = e.PaySvc.GetOrder(e.Parent(uid), order.Id);
        Assert.Equal(HocTapConst.PAY_PAID, paid.Status);
        Assert.Equal(now.AddMonths(1), paid.PlanTo);
        Assert.Contains("Minh – Toán", e.Admin.Logs.Single(x => x.action == HocTapConst.PLAN_PAYMENT).note);

        // cổng gửi lại -> không cộng thêm
        Assert.True(e.PaySvc.HandleBankTransaction("Apikey key-123", Txn(order.Code, 39000)).Matched);
        Assert.Equal(now.AddMonths(1), e.Uow.Payments.Plans.Single().planend);

        // gói 2 môn 3 tháng: Toán nối tiếp hạn cũ, Anh tính từ bây giờ; con khác không đổi
        var order2 = e.PaySvc.CreateOrder(e.Parent(uid), Order("3T2", sid, "toan", "anh"));
        e.PaySvc.HandleBankTransaction("key-123", Txn(order2.Code, 159000, "1002"));
        Assert.Equal(now.AddMonths(1).AddMonths(3), e.Uow.Payments.Plans.Single(p => p.subject == "toan").planend);
        Assert.Equal(now.AddMonths(3), e.Uow.Payments.Plans.Single(p => p.subject == "anh").planend);
        Assert.DoesNotContain(e.Uow.Payments.Plans, p => p.studentid == na);

        // thông tin tài khoản trả client: hạn từng con × môn
        var me = e.Auth.Me(e.Parent(uid));
        Assert.Equal(2, me.User.Plans.Count);
        Assert.True(me.User.Plans.All(p => p.Active && p.StudentId == sid));
    }

    [Fact]
    public void Legacy_order_without_student_extends_every_child_both_subjects()
    {
        var e = new Env();
        var uid = NewParent(e);
        var a = NewStudent(e, uid, "An");
        var b = NewStudent(e, uid, "Bình", 3);
        var legacy = new PaymentOrderModel { id = Guid.NewGuid(), code = "HTAAAABBBB", userid = uid, plancode = "1T", months = 1, amount = 49000, status = HocTapConst.PAY_PENDING, timecr = e.Clock.UtcNow };
        e.Uow.Payments.Rows.Add(legacy);
        Assert.True(e.PaySvc.HandleBankTransaction("key-123", Txn(legacy.code, 49000, "9")).Matched);
        Assert.Equal(4, e.Uow.Payments.Plans.Count);
        Assert.True(e.Uow.Payments.Plans.All(p => (p.studentid == a || p.studentid == b) && p.planend == e.Clock.UtcNow.AddMonths(1)));
    }

    [Fact]
    public void Study_access_is_per_student_and_subject_after_trial()
    {
        var e = new Env();
        var uid = NewParent(e);
        var sid = NewStudent(e, uid);
        var me = e.Student(uid, sid);
        AttemptSaveModel Practice(string topic) => new()
        {
            TopicId = topic, Mode = HocTapConst.MODE_PRACTICE, Level = 1, Total = 10, Correct = 7,
            FinishedAt = AttemptRules.ToUnixMs(e.Clock.UtcNow.AddMinutes(-1)),
        };

        // đang dùng thử: học được cả 2 môn; mua gói Toán vẫn giữ dùng thử môn Anh
        e.PaySvc.HandleBankTransaction("key-123", Txn(e.PaySvc.CreateOrder(e.Parent(uid), Order("1T1", sid, "toan")).Code, 39000, "7"));
        e.AttemptSvc.InsertAttempt(me, Practice("g6.anh.e1v"));

        // hết dùng thử: Toán còn hạn, Anh bị chặn (403, câu báo nêu tên con + môn); giao bài Anh cũng bị chặn
        e.Clock.UtcNow = e.Clock.UtcNow.AddDays(20);
        e.AttemptSvc.InsertAttempt(me, Practice("g6.toan.taphop"));
        var ex = Assert.Throws<LogicException>(() => e.AttemptSvc.InsertAttempt(me, Practice("g6.anh.e1v")));
        Assert.Equal(403, ex.ErrorCode);
        Assert.Contains("Minh", ex.Message);
        Assert.Contains("Tiếng Anh", ex.Message);
        Assert.False(e.Auth.Me(e.Parent(uid)).User.TrialActive);
        Assert.True(e.Auth.Me(e.Parent(uid)).User.CanStudy);
    }

    [Fact]
    public void Webhook_ignores_wrong_amount_and_unknown_code_then_admin_confirms()
    {
        var e = new Env();
        var uid = NewParent(e);
        var sid = NewStudent(e, uid);
        var adminId = Guid.NewGuid();
        var order = e.PaySvc.CreateOrder(e.Parent(uid), Order("1T1", sid, "anh"));

        Assert.False(e.PaySvc.HandleBankTransaction("key-123", Txn(order.Code, 10000)).Matched);
        Assert.False(e.PaySvc.HandleBankTransaction("key-123", Txn("khong co ma", 39000, "2")).Matched);
        Assert.False(e.PaySvc.HandleBankTransaction("key-123", new BankTransactionModel { TransactionId = "3", Content = order.Code, Amount = 39000, TransferType = "out" }).Matched);
        Assert.Equal(HocTapConst.STATUS_TRIAL, e.Users.GetUserById(uid)!.status);
        Assert.Empty(e.Uow.Payments.Plans);

        Assert.Throws<LogicException>(() => e.PaySvc.ConfirmOrderByAdmin(e.Parent(uid), order.Id, new AdminConfirmPaymentModel()));
        var read = e.PaySvc.ConfirmOrderByAdmin(e.AdminUser(adminId), order.Id, new AdminConfirmPaymentModel { Note = "đã kiểm tra sao kê" });
        Assert.Equal(HocTapConst.PAY_PAID, read.Status);
        Assert.Equal("Minh", read.StudentName);
        Assert.Equal(new List<string> { "anh" }, read.Subjects);
        Assert.Equal(HocTapConst.STATUS_ACTIVE, e.Users.GetUserById(uid)!.status);
        Assert.Equal(adminId, e.Admin.Logs.Single(x => x.action == HocTapConst.PLAN_PAYMENT).adminid);
        Assert.Throws<LogicException>(() => e.PaySvc.ConfirmOrderByAdmin(e.AdminUser(adminId), order.Id, new AdminConfirmPaymentModel()));
        Assert.Single(e.PaySvc.GetListOrderForAdmin(e.AdminUser(adminId), HocTapConst.PAY_PAID));
    }

    [Fact]
    public void Grade_change_limited_after_first_week()
    {
        var e = new Env();
        var uid = NewParent(e);
        var sid = NewStudent(e, uid, "Minh", 6);
        StudentSaveModel Grade(short g) => new StudentSaveModel { NickName = "Minh", Grade = g };
        // tuần đầu: sửa nhầm lớp tự do, không tính lượt
        e.StudentSvc.UpdateStudent(e.Parent(uid), sid, Grade(7));
        e.StudentSvc.UpdateStudent(e.Parent(uid), sid, Grade(6));
        // sau 7 ngày: đổi 1 lần, lần tiếp phải chờ 180 ngày
        e.Clock.UtcNow = e.Clock.UtcNow.AddDays(10);
        var r = e.StudentSvc.UpdateStudent(e.Parent(uid), sid, Grade(7));
        Assert.Equal(7, r.Grade);
        Assert.NotNull(r.GradeLockedUntil);
        Assert.Throws<LogicException>(() => e.StudentSvc.UpdateStudent(e.Parent(uid), sid, Grade(8)));
        e.StudentSvc.UpdateStudent(e.Parent(uid), sid, Grade(7));   // giữ nguyên lớp, sửa tên: được
        e.Clock.UtcNow = e.Clock.UtcNow.AddDays(181);
        Assert.Equal(8, e.StudentSvc.UpdateStudent(e.Parent(uid), sid, Grade(8)).Grade);
    }

    [Fact]
    public void Webhook_disabled_when_no_key()
    {
        var e = new Env();
        e.PayOptions.WebhookApiKey = "";
        Assert.Throws<LogicException>(() => e.PaySvc.HandleBankTransaction("", Txn("HTAAAAAAAA", 1)));
        Assert.False(e.PaySvc.GetConfig().AutoConfirm);
    }
}
