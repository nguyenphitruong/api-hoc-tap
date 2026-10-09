using System.Text.Json;

namespace HocTap.Domain.Aggregates.Attempts;

/// <summary>
/// 08/10/2026 - Nộp 1 lượt học (học sinh gửi) / 1 dòng khi nhập dữ liệu cũ (phụ huynh gửi, không có Items).
/// Thời gian là mili-giây Unix (Date.now() ở trình duyệt). Items chỉ có ở lượt kiểm tra.
/// </summary>
public class AttemptSaveModel
{
    public Guid? AssignmentId { get; set; }
    public short Grade { get; set; }
    public string? Subject { get; set; }
    public string? TopicId { get; set; }
    public string? Mode { get; set; }
    public short? Level { get; set; }
    public long? Seed { get; set; }
    public int Total { get; set; }
    public int Correct { get; set; }
    public decimal? Score10 { get; set; }
    public int DurationSec { get; set; }
    public long? StartedAt { get; set; }
    public long FinishedAt { get; set; }
    public List<AttemptItemModel>? Items { get; set; }
}

/// <summary>08/10/2026 - 1 câu: Question = nội dung câu (đề, phương án, đáp án đúng, lời giải), Answer = bài làm</summary>
public class AttemptItemModel
{
    public int Idx { get; set; }
    public JsonElement Question { get; set; }
    public JsonElement? Answer { get; set; }
    public bool IsCorrect { get; set; }
}

/// <summary>08/10/2026 - Dòng tiến độ (không kèm câu hỏi) — client tính sao / % / chủ đề cần ôn như bản chạy trên máy</summary>
public class AttemptReadModel
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid? AssignmentId { get; set; }
    public short Grade { get; set; }
    public string Subject { get; set; } = "";
    public string TopicId { get; set; } = "";
    public string Mode { get; set; } = "";
    public short? Level { get; set; }
    public int Total { get; set; }
    public int Correct { get; set; }
    public decimal? Score10 { get; set; }
    public int DurationSec { get; set; }
    public long FinishedAt { get; set; }
    public bool HasItems { get; set; }
}

/// <summary>08/10/2026 - Xem lại 1 lượt từng câu</summary>
public class AttemptDetailReadModel : AttemptReadModel
{
    public long? Seed { get; set; }
    public List<AttemptItemModel> Items { get; set; } = new();
}

/// <summary>08/10/2026 - Nhập lịch sử (file JSON web cũ / dữ liệu đang lưu trên máy) vào 1 hồ sơ con</summary>
public class ImportModel
{
    public List<AttemptSaveModel>? Attempts { get; set; }
}

public class ImportResultReadModel
{
    public int Added { get; set; }
    public int Skipped { get; set; }
}
