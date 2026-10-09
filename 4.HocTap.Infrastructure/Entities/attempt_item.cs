namespace HocTap.Infrastructure.Entities;

/// <summary>08/10/2026 - Bảng attempt_item: từng câu của lượt kiểm tra (question/answer là jsonb) để xem lại đúng đề</summary>
public class attempt_item
{
    public Guid id { get; set; }
    public Guid attemptid { get; set; }
    public int idx { get; set; }
    public string question { get; set; } = "{}";
    public string? answer { get; set; }
    public bool iscorrect { get; set; }
}
