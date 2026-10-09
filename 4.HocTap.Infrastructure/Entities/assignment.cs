namespace HocTap.Infrastructure.Entities;

/// <summary>
/// 08/10/2026 - Bảng assignment: bài phụ huynh giao. Đề sinh lại ở trình duyệt từ (topicids, level, questioncount, seed);
/// riêng bài "giao lại câu sai" lưu sẵn nội dung câu trong questions (jsonb mảng) vì không sinh lại được bằng seed.
/// </summary>
public class assignment
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
