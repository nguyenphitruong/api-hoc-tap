namespace HocTap.Domain.Commons;

/// <summary>
/// 08/10/2026 - Quy tắc bài giao (hàm thuần, có xUnit):
/// - Trạng thái của con: đã nộp → DANOP; chưa nộp và quá hạn → QUAHAN; còn lại CHUALAM (tính khi đọc, không cần tác vụ nền).
/// - Mã lượt làm bài: as:g{lớp}.{môn}.{id dạng N} (để tiến độ phân biệt với chủ đề / chương).
/// </summary>
public static class AssignmentRules
{
    public static string EffectiveStatus(string stored, DateTime dueDate, DateTime nowUtc)
    {
        if (stored == HocTapConst.AS_DONE) return HocTapConst.AS_DONE;
        return nowUtc > dueDate ? HocTapConst.AS_OVERDUE : HocTapConst.AS_TODO;
    }

    public static string TopicKey(short grade, string subject, Guid assignmentId) =>
        $"{HocTapConst.TOPIC_ASSIGN}:g{grade}.{subject}.{assignmentId:N}";

    /// <summary>Cập nhật dòng của con sau 1 lượt nộp: tăng số lượt, giữ điểm cao nhất, đánh dấu nộp trễ ở lần nộp đầu</summary>
    public static void ApplySubmission(Aggregates.Assignments.AssignmentStudentModel row, Guid attemptId, decimal score, DateTime finishedAt, DateTime dueDate)
    {
        row.attemptcount++;
        row.lastattemptat = finishedAt;
        if (row.submittedat == null)
        {
            row.submittedat = finishedAt;
            row.islate = finishedAt > dueDate;
        }
        if (row.bestscore == null || score > row.bestscore)
        {
            row.bestscore = score;
            row.bestattemptid = attemptId;
        }
        row.status = HocTapConst.AS_DONE;
    }
}
