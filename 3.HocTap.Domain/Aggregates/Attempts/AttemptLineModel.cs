namespace HocTap.Domain.Aggregates.Attempts;

/// <summary>
/// 08/10/2026 - Model Bảng attempt_item: 1 câu của lượt kiểm tra (question / answer là chuỗi JSON).
/// Tên thuộc tính trùng tên cột (chữ thường) như entity attempt_item ở tầng Infrastructure;
/// repository chuyển đổi entity <-> model qua HocTapEntityMapper (MappingRepos).
/// </summary>
public class AttemptLineModel
{
    public Guid id { get; set; }
    public Guid attemptid { get; set; }
    public int idx { get; set; }
    public string question { get; set; } = "{}";
    public string? answer { get; set; }
    public bool iscorrect { get; set; }
}
