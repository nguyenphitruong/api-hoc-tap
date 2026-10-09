namespace HocTap.Domain.Aggregates.Assignments;

/// <summary>
/// 08/10/2026 - Model Bảng assignment: bài phụ huynh giao (questions: JSON các câu của bài giao lại câu sai).
/// Tên thuộc tính trùng tên cột (chữ thường) như entity assignment ở tầng Infrastructure;
/// repository chuyển đổi entity <-> model qua HocTapEntityMapper (MappingRepos).
/// </summary>
public class AssignmentModel
{
    public Guid id { get; set; }
    public Guid parentid { get; set; }
    public string title { get; set; } = "";
    public short grade { get; set; }
    public string subject { get; set; } = "";
    public string[] topicids { get; set; } = Array.Empty<string>();
    public short level { get; set; }
    public int questioncount { get; set; }
    public int? timelimitmin { get; set; }
    public long seed { get; set; }
    public DateTime duedate { get; set; }
    public string? note { get; set; }
    public string status { get; set; } = "";
    public string? questions { get; set; }
    public Guid? sourceid { get; set; }
    public DateTime timecr { get; set; }
    public DateTime? timeup { get; set; }
}
