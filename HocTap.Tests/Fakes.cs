using HocTap.Application.Admin;
using HocTap.Application.Assignments;
using HocTap.Application.Attempts;
using HocTap.Application.Auth;
using HocTap.Application.Commons;
using HocTap.Application.Students;
using HocTap.Domain.Aggregates.Admin;
using HocTap.Domain.Aggregates.Assignments;
using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Aggregates.Payments;   // 12/10/2026
using HocTap.Application.Payments;
using HocTap.Application.Settings;           // 14/10/2026
using HocTap.Domain.Aggregates.Settings;     // 14/10/2026
using HocTap.Application.Guests;             // 17/10/2026
using HocTap.Domain.Aggregates.Guests;       // 17/10/2026
using HocTap.Application.Feedbacks;          // 18/10/2026
using HocTap.Domain.Aggregates.Feedbacks;    // 18/10/2026
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;

namespace HocTap.Tests;

/// <summary>
/// 08/10/2026 - UnitOfWork giả trong bộ nhớ (cấu trúc giống UnitOfWork thật: XxxRepo + 4 hàm transaction).
/// Repository giả giữ Model theo tham chiếu nên Insert/Update ghi thẳng vào danh sách.
/// </summary>
public class FakeUow : IUnitOfWork
{
    public int Commits;
    public int Rollbacks;
    public FakeUsers Users = new();
    public FakeStudents Students = new();
    public FakeAttempts Attempts = new();
    public FakeAssignments Assignments;
    public FakeAdmin Admin;
    public FakePayments Payments;   // 12/10/2026
    public FakeSettings Settings = new();   // 14/10/2026
    public FakeGuests Guests = new();       // 17/10/2026
    public FakeFeedback Feedback = new();   // 18/10/2026

    public FakeUow()
    {
        Assignments = new FakeAssignments(Students, Attempts);
        Admin = new FakeAdmin(Users, Students, Attempts);
        Payments = new FakePayments(Users, Students);
    }

    public IUserRepository UserRepo => Users;
    public IStudentRepository StudentRepo => Students;
    public IAttemptRepository AttemptRepo => Attempts;
    public IAssignmentRepository AssignmentRepo => Assignments;
    public IAdminRepository AdminRepo => Admin;
    public IPaymentRepository PaymentRepo => Payments;
    public ISettingRepository SettingRepo => Settings;   // 14/10/2026
    public IGuestRepository GuestRepo => Guests;         // 17/10/2026
    public IFeedbackRepository FeedbackRepo => Feedback; // 18/10/2026

    public void InitTransaction() { }
    public void Save() { }
    public void CommitTransaction() { Commits++; }
    public void RollbackTransaction() { Rollbacks++; }
}

public class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc);
}

public class FakeUsers : IUserRepository
{
    public List<AppUserModel> Users = new();
    public List<RefreshTokenModel> Tokens = new();
    public AppUserModel? GetUserById(Guid i_id) => Users.FirstOrDefault(x => x.id == i_id);
    public AppUserModel? GetUserByEmail(string i_email) => Users.FirstOrDefault(x => x.email == i_email);
    public AppUserModel? GetUserByPhone(string i_phone) => Users.FirstOrDefault(x => x.phone == i_phone);
    public void InsertUser(AppUserModel i_user) => Users.Add(i_user);
    public void UpdateUser(AppUserModel i_user) { }
    public RefreshTokenModel? GetRefreshTokenByHash(string i_tokenHash) => Tokens.FirstOrDefault(x => x.tokenhash == i_tokenHash);
    public void InsertRefreshToken(RefreshTokenModel i_token) => Tokens.Add(i_token);
    public void UpdateRefreshToken(RefreshTokenModel i_token) { }
    public void RevokeAllRefreshToken(Guid i_userId) { foreach (var t in Tokens.Where(x => x.userid == i_userId)) t.revoked = true; }
}

public class FakeStudents : IStudentRepository
{
    public List<StudentProfileModel> Rows = new();
    public List<StudentProfileModel> GetListStudentByUser(Guid i_userId) => Rows.Where(x => x.userid == i_userId && x.active).ToList();
    public StudentProfileModel? GetStudentById(Guid i_id) => Rows.FirstOrDefault(x => x.id == i_id);
    public int CountActiveStudent(Guid i_userId) => Rows.Count(x => x.userid == i_userId && x.active);
    public void InsertStudent(StudentProfileModel i_student) => Rows.Add(i_student);
    public void UpdateStudent(StudentProfileModel i_student) { }
}

public class FakeAttempts : IAttemptRepository
{
    public List<AttemptModel> Rows = new();
    public void InsertAttempt(AttemptModel i_attempt) => Rows.Add(i_attempt);
    public void InsertListAttempt(List<AttemptModel> i_lstAttempt) => Rows.AddRange(i_lstAttempt);
    public List<AttemptReadModel> GetListAttemptByStudent(Guid i_studentId, int i_max) =>
        Rows.Where(x => x.studentid == i_studentId).Select(x => new AttemptReadModel { Id = x.id, TopicId = x.topicid, Mode = x.mode }).ToList();
    public AttemptModel? GetAttemptWithItems(Guid i_id) => Rows.FirstOrDefault(x => x.id == i_id);
    public HashSet<string> GetAttemptKeys(Guid i_studentId) =>
        Rows.Where(x => x.studentid == i_studentId).Select(x => AttemptRules.Key(x.topicid, x.mode, x.finishedat)).ToHashSet();
}

/// <summary>Băm giả (nhanh) cho test</summary>
public class FakeHasher : IPasswordHasher
{
    public string Hash(string raw) => "H:" + raw;
    public bool Verify(string raw, string? hash) => hash == "H:" + raw;
}

/// <summary>Bộ dựng service dùng chung (constructor giống DI thật: IUnitOfWork + dịch vụ phụ trợ)</summary>
public class Env
{
    public FakeUow Uow = new();
    public FakeClock Clock = new();
    public FakeHasher Hasher = new();
    public JwtTokenService Tokens = new(new JwtOptions { Key = "test-key-0123456789-0123456789-abcdef" });

    public FakeUsers Users => Uow.Users;
    public FakeStudents Students => Uow.Students;
    public FakeAttempts Attempts => Uow.Attempts;
    public FakeAssignments Asg => Uow.Assignments;
    public FakeAdmin Admin => Uow.Admin;

    public IAuthServices Auth => new AuthServices(Uow, Tokens, Hasher, Clock, GuestSvc);   // Update 17/10/2026: gắn khách học thử
    public IGuestServices GuestSvc => new GuestServices(Uow, Clock);                       // 17/10/2026
    public IFeedbackServices FeedbackSvc => new FeedbackServices(Uow, Clock);              // 18/10/2026
    public IStudentServices StudentSvc => new StudentServices(Uow, Hasher, Clock);
    public IAttemptServices AttemptSvc => new AttemptServices(Uow, Clock);
    public IAssignmentServices AsgSvc => new AssignmentServices(Uow, Clock);
    public IAdminServices AdminSvc => new AdminServices(Uow, Clock);
    // 12/10/2026 - thanh toán gia hạn
    public PaymentOptions PayOptions = new PaymentOptions
    {
        BankBin = "970436", BankName = "Vietcombank", AccountNo = "0123456789", AccountName = "NGUYEN VAN A", WebhookApiKey = "key-123",
        Plans = new List<PlanPackageReadModel>
        {
            // 13/10/2026: gói theo số môn
            new PlanPackageReadModel { Code = "1T1", Name = "1 môn · 1 tháng", Months = 1, Subjects = 1, Price = 39000 },
            new PlanPackageReadModel { Code = "3T2", Name = "2 môn · 3 tháng", Months = 3, Subjects = 2, Price = 159000 },
        },
    };
    public IPaymentServices PaySvc => new PaymentServices(Uow, Clock, PayOptions, Cache);
    // 14/10/2026 - cài đặt hệ thống (bảng giá + liên hệ); bộ nhớ đệm dùng chung như singleton thật
    public SettingCache Cache = new();
    public ISettingServices SettingSvc => new SettingServices(Uow, Clock, PayOptions, Cache);

    public CurrentUser AdminUser(Guid id) => new(id, HocTapConst.ROLE_ADMIN, null);
    public CurrentUser Parent(Guid userId) => new(userId, HocTapConst.ROLE_PARENT, null);
    public CurrentUser Student(Guid userId, Guid sid) => new(userId, HocTapConst.ROLE_STUDENT, sid);
}

/// <summary>08/10/2026 - Giai đoạn 4: bài giao trong bộ nhớ</summary>
public class FakeAssignments : IAssignmentRepository
{
    private readonly FakeStudents students;
    private readonly FakeAttempts attempts;
    public List<AssignmentModel> Rows = new();
    public List<AssignmentStudentModel> Links = new();
    public FakeAssignments(FakeStudents i_students, FakeAttempts i_attempts) { students = i_students; attempts = i_attempts; }

    public void InsertAssignment(AssignmentModel i_assignment, List<AssignmentStudentModel> i_lstStudent) { Rows.Add(i_assignment); Links.AddRange(i_lstStudent); }
    public void UpdateAssignment(AssignmentModel i_assignment) { }
    public AssignmentModel? GetAssignmentById(Guid i_id) => Rows.FirstOrDefault(x => x.id == i_id);
    public List<AssignmentModel> GetListAssignmentByParent(Guid i_parentId, int i_max) => Rows.Where(x => x.parentid == i_parentId).ToList();
    public List<AssignmentStudentRow> GetListAssignmentStudentRow(List<Guid> i_lstAssignmentId) =>
        Links.Where(l => i_lstAssignmentId.Contains(l.assignmentid))
            .Select(l => new AssignmentStudentRow { Row = l, NickName = students.GetStudentById(l.studentid)!.nickname, Avatar = students.GetStudentById(l.studentid)!.avatar }).ToList();
    public AssignmentStudentModel? GetAssignmentStudent(Guid i_assignmentId, Guid i_studentId) => Links.FirstOrDefault(x => x.assignmentid == i_assignmentId && x.studentid == i_studentId);
    public void UpdateAssignmentStudent(AssignmentStudentModel i_row) { }
    public List<AssignmentOfStudentRow> GetListOpenAssignmentOfStudent(Guid i_studentId) =>
        Links.Where(l => l.studentid == i_studentId)
            .Select(l => new AssignmentOfStudentRow { Assignment = GetAssignmentById(l.assignmentid)!, Row = l })
            .Where(x => x.Assignment.status == HocTapConst.ASG_OPEN).ToList();
    public List<AttemptModel> GetListAttemptOfAssignment(Guid i_assignmentId) => attempts.Rows.Where(x => x.assignmentid == i_assignmentId).ToList();
    public AttemptModel? GetLatestAttemptOfStudent(Guid i_assignmentId, Guid i_studentId) =>
        attempts.Rows.Where(x => x.assignmentid == i_assignmentId && x.studentid == i_studentId).OrderByDescending(x => x.finishedat).FirstOrDefault();
}

/// <summary>08/10/2026 - Giai đoạn 5: truy vấn quản trị trong bộ nhớ</summary>
public class FakeAdmin : IAdminRepository
{
    private readonly FakeUsers users; private readonly FakeStudents students; private readonly FakeAttempts attempts;
    public List<PlanLogModel> Logs = new();
    public FakeAdmin(FakeUsers i_users, FakeStudents i_students, FakeAttempts i_attempts) { users = i_users; students = i_students; attempts = i_attempts; }
    public List<AdminUserRow> GetListParentUser(string? i_query) => users.Users
        .Where(x => x.role == HocTapConst.ROLE_PARENT && (i_query == null || x.fullname.Contains(i_query) || (x.email ?? "").Contains(i_query)))
        .Select(x => new AdminUserRow
        {
            User = x,
            Students = students.Rows.Count(s => s.userid == x.id && s.active),
            Attempts = attempts.Rows.Count(a => students.Rows.Any(s => s.id == a.studentid && s.userid == x.id)),
        }).ToList();
    public List<AppUserModel> GetAllParentUser() => users.Users.Where(x => x.role == HocTapConst.ROLE_PARENT).ToList();
    public void InsertPlanLog(PlanLogModel i_log) => Logs.Add(i_log);
    public List<PlanLogRow> GetListPlanLog(Guid i_userId) => Logs.Where(x => x.userid == i_userId).Select(x => new PlanLogRow { Log = x, AdminName = "Admin" }).ToList();
    public int CountActiveStudent() => students.Rows.Count(x => x.active);
    public int CountStudentHasAttemptSince(DateTime i_sinceUtc) => attempts.Rows.Where(x => x.finishedat >= i_sinceUtc).Select(x => x.studentid).Distinct().Count();
    public int CountAttempt() => attempts.Rows.Count;
    public int CountAssignment() => 0;
    public List<DateTime> GetListAttemptTime(DateTime i_sinceUtc) => attempts.Rows.Where(x => x.finishedat >= i_sinceUtc).Select(x => x.finishedat).ToList();
}

/// <summary>12/10/2026 - Repository đơn gia hạn giả (giữ Model theo tham chiếu)</summary>
public class FakePayments : IPaymentRepository
{
    private readonly FakeUsers users;
    private readonly FakeStudents students;   // 13/10/2026
    public List<PaymentOrderModel> Rows = new();
    public List<StudentPlanModel> Plans = new();   // 13/10/2026 - student_plan
    public FakePayments(FakeUsers i_users, FakeStudents i_students) { users = i_users; students = i_students; }
    public void InsertOrder(PaymentOrderModel i_order) => Rows.Add(i_order);
    public void UpdateOrder(PaymentOrderModel i_order) { }
    public PaymentOrderModel? GetOrderById(Guid i_id) => Rows.FirstOrDefault(x => x.id == i_id);
    public PaymentOrderModel? GetOrderByCode(string i_code) => Rows.FirstOrDefault(x => x.code == i_code);
    public PaymentOrderModel? GetOrderByTxnRef(string i_gateway, string i_txnRef) => Rows.FirstOrDefault(x => x.gateway == i_gateway && x.txnref == i_txnRef);
    public PaymentOrderModel? GetLastPendingOrder(Guid i_userId, string i_planCode) =>
        Rows.Where(x => x.userid == i_userId && x.plancode == i_planCode && x.status == HocTapConst.PAY_PENDING).OrderByDescending(x => x.timecr).FirstOrDefault();
    public List<PaymentOrderRow> GetListOrder(string? i_status, int i_max) => Rows
        .Where(x => i_status == null || x.status == i_status)
        .Select(x => new PaymentOrderRow { Order = x, FullName = users.Users.FirstOrDefault(u => u.id == x.userid)?.fullname ?? "" })
        .Take(i_max).ToList();
    // 13/10/2026 - hạn học con × môn
    public List<StudentPlanModel> GetListStudentPlanByUser(Guid i_userId) =>
        Plans.Where(p => students.Rows.Any(st => st.id == p.studentid && st.userid == i_userId && st.active)).ToList();
    public List<StudentPlanModel> GetListStudentPlan(Guid i_studentId) => Plans.Where(p => p.studentid == i_studentId).ToList();
    public void UpsertStudentPlan(StudentPlanModel i_plan)
    {
        StudentPlanModel? old = Plans.FirstOrDefault(p => p.studentid == i_plan.studentid && p.subject == i_plan.subject);
        if (old == null) Plans.Add(i_plan); else { old.planend = i_plan.planend; old.timeup = i_plan.timeup; }
    }
}

/// <summary>14/10/2026 - Bảng giá (plan_price) + tham số (sys_setting) trong bộ nhớ; Fail = giả lập chưa tạo bảng</summary>
public class FakeSettings : ISettingRepository
{
    public List<PlanPriceModel> Plans = new();
    public List<SysSettingModel> Values = new();
    public bool Fail;
    public int Reads;

    public List<PlanPriceModel> GetListPlanPrice()
    {
        Reads++;
        if (Fail) throw new InvalidOperationException("relation \"plan_price\" does not exist");
        return Plans.OrderBy(x => x.sortorder).ThenBy(x => x.code).ToList();
    }

    public void UpsertPlanPrice(PlanPriceModel i_plan)
    {
        Plans.RemoveAll(x => x.code == i_plan.code);
        Plans.Add(i_plan);
    }

    public List<SysSettingModel> GetListSetting()
    {
        if (Fail) throw new InvalidOperationException("relation \"sys_setting\" does not exist");
        return Values.ToList();
    }

    public void UpsertSetting(SysSettingModel i_setting)
    {
        Values.RemoveAll(x => x.key == i_setting.key);
        Values.Add(i_setting);
    }
}

/// <summary>17/10/2026 - Hoạt động khách học thử trong bộ nhớ (cùng ngữ nghĩa GuestRepository)</summary>
public class FakeGuests : IGuestRepository
{
    public List<GuestActivityModel> Rows = new();
    public bool Fail;

    public void InsertActivity(GuestActivityModel i_activity) => Rows.Add(i_activity);
    public int CountActivitySince(string i_guestId, DateTime i_since) => Rows.Count(x => x.guestid == i_guestId && x.timecr >= i_since);

    public int LinkGuest(string i_guestId, Guid i_userId, string i_via, DateTime i_now, DateTime i_since)
    {
        if (Fail) throw new InvalidOperationException("relation \"guest_activity\" does not exist");
        var lst = Rows.Where(x => x.guestid == i_guestId && x.userid == null && x.timecr >= i_since).ToList();
        foreach (var r in lst) { r.userid = i_userId; r.linkvia = i_via; r.linkedat = i_now; }
        return lst.Count;
    }

    public List<GuestActivityModel> GetListActivitySince(DateTime i_since, int i_max)
    {
        var ids = Rows.Where(x => x.timecr >= i_since).Select(x => x.guestid).ToHashSet();
        return Rows.Where(x => ids.Contains(x.guestid)).OrderByDescending(x => x.timecr).Take(i_max).ToList();
    }
}

/// <summary>18/10/2026 - Góp ý trong bộ nhớ (cùng ngữ nghĩa FeedbackRepository)</summary>
public class FakeFeedback : IFeedbackRepository
{
    public List<FeedbackModel> Rows = new();
    public void InsertFeedback(FeedbackModel i_feedback) => Rows.Add(i_feedback);
    public int CountFeedbackSince(Guid? i_userId, string? i_guestId, DateTime i_since)
    {
        if (i_userId.HasValue) return Rows.Count(x => x.timecr >= i_since && x.userid == i_userId);
        if (!string.IsNullOrEmpty(i_guestId)) return Rows.Count(x => x.timecr >= i_since && x.guestid == i_guestId);
        return 0;
    }
    public List<FeedbackModel> GetListFeedback() => Rows.OrderByDescending(x => x.timecr).ToList();
    public FeedbackModel? GetFeedbackById(Guid i_id) => Rows.FirstOrDefault(x => x.id == i_id);
    public void UpdateFeedback(FeedbackModel i_feedback) { }
}
