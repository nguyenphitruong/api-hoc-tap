namespace HocTap.Infrastructure.Entities;

/// <summary>
/// 08/10/2026 - Bảng assignment_student: bài giao cho từng con. Cập nhật khi con nộp: số lượt, điểm cao nhất,
/// lượt điểm cao nhất, lần nộp đầu (submittedat), nộp trễ (islate = lần nộp đầu sau hạn).
/// </summary>
public class assignment_student
{
    public Guid assignmentid { get; set; }
    public Guid studentid { get; set; }
    public string status { get; set; } = "";
    public decimal? bestscore { get; set; }
    public Guid? bestattemptid { get; set; }
    public int attemptcount { get; set; }
    public DateTime? submittedat { get; set; }
    public DateTime? lastattemptat { get; set; }
    public bool islate { get; set; }
}
