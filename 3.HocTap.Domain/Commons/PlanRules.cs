using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Payments;
using HocTap.Domain.Aggregates.Students;

namespace HocTap.Domain.Commons;

/// <summary>
/// 13/10/2026 - QUY TẮC GÓI HỌC THEO HỒ SƠ CON × MÔN (hàm thuần, có xUnit).
/// - Gói tính theo số môn (1 môn / 2 môn), không phân biệt lớp. Mỗi dòng student_plan = hạn học 1 con × 1 môn.
/// - Được học (lưu bài, nhận bài giao) môn M của con C khi: tài khoản không khoá VÀ (đang dùng thử HOẶC student_plan(C, M) còn hạn).
///   Dùng thử (trialend) áp dụng cho mọi con, mọi môn và vẫn giữ nguyên khi phụ huynh đã mua gói 1 môn.
/// - Gia hạn: nối tiếp từ hạn cũ nếu còn, ngược lại tính từ bây giờ (mỗi môn tính riêng).
/// - Đổi lớp: hồ sơ mới tạo sửa tự do trong GRADE_FIX_DAYS ngày; sau đó mỗi lần đổi cách lần trước GRADE_CHANGE_DAYS ngày.
/// </summary>
public static class PlanRules
{
    /// <summary>Danh sách môn hợp lệ, bỏ trùng, sắp xếp (để so sánh / lưu "anh,toan")</summary>
    public static List<string> NormalizeSubjects(IEnumerable<string>? i_subjects) =>
        (i_subjects ?? Enumerable.Empty<string>())
            .Select(x => (x ?? "").Trim().ToLowerInvariant())
            .Where(x => HocTapConst.SUBJECTS.Contains(x))
            .Distinct()
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

    /// <summary>"anh,toan" <-> danh sách môn</summary>
    public static string JoinSubjects(IEnumerable<string> i_subjects) => string.Join(",", NormalizeSubjects(i_subjects));
    public static List<string> SplitSubjects(string? i_subjects) => NormalizeSubjects((i_subjects ?? "").Split(','));

    /// <summary>Tên môn để hiển thị: "Toán, Tiếng Anh"</summary>
    public static string SubjectNames(IEnumerable<string> i_subjects) =>
        string.Join(", ", NormalizeSubjects(i_subjects).OrderByDescending(x => x == "toan").Select(x => HocTapConst.SUBJECT_NAMES[x]));

    /// <summary>Đang dùng thử (mọi con, mọi môn): chưa bị khoá và trialend còn hạn</summary>
    public static bool TrialActive(AppUserModel i_user, DateTime i_nowUtc) =>
        i_user.status != HocTapConst.STATUS_LOCKED && i_user.trialend >= i_nowUtc;

    /// <summary>Con được học môn này không (xem quy tắc ở đầu lớp)</summary>
    public static bool CanStudySubject(AppUserModel i_user, IEnumerable<StudentPlanModel> i_plans, Guid i_studentId, string i_subject, DateTime i_nowUtc)
    {
        if (i_user.status == HocTapConst.STATUS_LOCKED) return false;
        if (TrialActive(i_user, i_nowUtc)) return true;
        return i_plans.Any(p => p.studentid == i_studentId && p.subject == i_subject && p.planend >= i_nowUtc);
    }

    /// <summary>Tài khoản còn dùng được (dùng thử hoặc ít nhất 1 gói còn hạn) — banner chung, thống kê</summary>
    public static bool CanStudyAny(AppUserModel i_user, IEnumerable<StudentPlanModel> i_plans, DateTime i_nowUtc) =>
        i_user.status != HocTapConst.STATUS_LOCKED && (TrialActive(i_user, i_nowUtc) || i_plans.Any(p => p.planend >= i_nowUtc));

    /// <summary>Mốc bắt đầu cộng hạn: còn hạn thì nối tiếp, hết hạn / chưa có thì từ bây giờ</summary>
    public static DateTime ExtendFrom(DateTime? i_planEnd, DateTime i_nowUtc) =>
        i_planEnd.HasValue && i_planEnd.Value > i_nowUtc ? i_planEnd.Value : i_nowUtc;

    /// <summary>Chưa được đổi lớp đến ngày nào (null = đổi được ngay)</summary>
    public static DateTime? GradeLockedUntil(StudentProfileModel i_student, DateTime i_nowUtc)
    {
        if (i_nowUtc < i_student.timecr.AddDays(HocTapConst.GRADE_FIX_DAYS)) return null;   // mới tạo: sửa nhầm lớp
        if (!i_student.gradechangedat.HasValue) return null;                                  // chưa đổi lần nào
        DateTime until = i_student.gradechangedat.Value.AddDays(HocTapConst.GRADE_CHANGE_DAYS);
        return i_nowUtc < until ? until : null;
    }

    /// <summary>Hạn từng con × môn trả client</summary>
    public static StudentPlanReadModel ToReadModel(StudentPlanModel i_plan, DateTime i_nowUtc) => new()
    {
        StudentId = i_plan.studentid, Subject = i_plan.subject, PlanEnd = i_plan.planend, Active = i_plan.planend >= i_nowUtc,
    };
}
