using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Payments;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;

namespace HocTap.Application.Commons;

/// <summary>
/// 13/10/2026 - CỘNG HẠN / KIỂM TRA QUYỀN HỌC THEO HỒ SƠ CON × MÔN (dùng chung: thanh toán, admin kích hoạt, lưu bài, giao bài).
/// Chỉ đọc / ghi qua unitOfWork, KHÔNG gọi Save (service gọi Save 1 lần cho cả thao tác).
/// </summary>
public static class PlanHelper
{
    /// <summary>
    /// Cộng i_months tháng cho mọi cặp (con, môn): mỗi môn nối tiếp từ hạn cũ nếu còn (PlanRules.ExtendFrom).
    /// Cập nhật tài khoản: planend = hạn xa nhất (hiển thị tổng quát), trạng thái HOATDONG (tài khoản đang khoá giữ KHOA).
    /// Trả về (From sớm nhất, To xa nhất) để ghi plan_log / đơn.
    /// </summary>
    public static (DateTime From, DateTime To) Extend(IUnitOfWork i_uow, AppUserModel i_user, IEnumerable<Guid> i_studentIds,
        IEnumerable<string> i_subjects, int i_months, DateTime i_nowUtc)
    {
        List<Guid> lstStudentId = i_studentIds.Distinct().ToList();
        List<string> lstSubject = PlanRules.NormalizeSubjects(i_subjects);
        if (lstStudentId.Count == 0) throw new LogicException("Tài khoản chưa có hồ sơ con để gia hạn.");
        if (lstSubject.Count == 0) throw new LogicException("Chưa chọn môn gia hạn.");

        List<StudentPlanModel> lstPlan = i_uow.PaymentRepo.GetListStudentPlanByUser(i_user.id);
        DateTime minFrom = DateTime.MaxValue, maxTo = DateTime.MinValue;
        foreach (Guid studentId in lstStudentId)
        {
            foreach (string subject in lstSubject)
            {
                StudentPlanModel? old = lstPlan.FirstOrDefault(p => p.studentid == studentId && p.subject == subject);
                DateTime from = PlanRules.ExtendFrom(old?.planend, i_nowUtc);
                DateTime to = from.AddMonths(i_months);
                i_uow.PaymentRepo.UpsertStudentPlan(new StudentPlanModel
                {
                    studentid = studentId, subject = subject, planend = to, timecr = old?.timecr ?? i_nowUtc, timeup = i_nowUtc,
                });
                if (from < minFrom) minFrom = from;
                if (to > maxTo) maxTo = to;
            }
        }

        if (!i_user.planend.HasValue || i_user.planend < maxTo) i_user.planend = maxTo;
        if (i_user.status != HocTapConst.STATUS_LOCKED) i_user.status = HocTapConst.STATUS_ACTIVE;
        i_user.timeup = i_nowUtc;
        return (minFrom, maxTo);
    }

    /// <summary>Con được học môn này không (dùng thử hoặc còn hạn môn) — ném lỗi 403 với câu báo rõ ràng nếu không</summary>
    public static void EnsureCanStudy(IUnitOfWork i_uow, AppUserModel i_user, Guid i_studentId, string i_nickName, string i_subject, DateTime i_nowUtc)
    {
        List<StudentPlanModel> lstPlan = i_uow.PaymentRepo.GetListStudentPlan(i_studentId);
        if (PlanRules.CanStudySubject(i_user, lstPlan, i_studentId, i_subject, i_nowUtc)) return;
        string subjectName = HocTapConst.SUBJECT_NAMES.GetValueOrDefault(i_subject, i_subject);
        throw new LogicException($"{i_nickName} chưa có gói môn {subjectName} (hoặc đã hết hạn). Phụ huynh vui lòng gia hạn để tiếp tục.", 403);
    }
}
