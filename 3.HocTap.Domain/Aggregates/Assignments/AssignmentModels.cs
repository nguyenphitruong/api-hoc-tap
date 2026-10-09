using System.Text.Json;

namespace HocTap.Domain.Aggregates.Assignments;

/// <summary>
/// 08/10/2026 - Giao bài mới: 1 hoặc nhiều con cùng lớp, 1 môn, 1–3 chủ đề, mức 1–3, 5–20 câu,
/// thời gian làm (phút, tuỳ chọn), hạn nộp, lời nhắn.
/// </summary>
public class AssignmentCreateModel
{
    public string? Title { get; set; }
    public short Grade { get; set; }
    public string? Subject { get; set; }
    public List<string>? TopicIds { get; set; }
    public short Level { get; set; }
    public int QuestionCount { get; set; }
    public int? TimeLimitMin { get; set; }
    public DateTime DueDate { get; set; }
    public string? Note { get; set; }
    public List<Guid>? StudentIds { get; set; }
}

/// <summary>08/10/2026 - Sửa bài giao: tiêu đề, hạn nộp, lời nhắn, thời gian; Status = DANGGIAO / DADONG (mở lại / đóng)</summary>
public class AssignmentUpdateModel
{
    public string? Title { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Note { get; set; }
    public int? TimeLimitMin { get; set; }
    public bool ClearTimeLimit { get; set; }
    public string? Status { get; set; }
}

/// <summary>08/10/2026 - Giao lại các câu con làm sai ở lượt gần nhất của bài</summary>
public class AssignmentRetryModel
{
    public Guid StudentId { get; set; }
    public DateTime? DueDate { get; set; }
}

/// <summary>08/10/2026 - Tình trạng bài giao của 1 con</summary>
public class AssignmentStudentReadModel
{
    public Guid StudentId { get; set; }
    public string NickName { get; set; } = "";
    public string Avatar { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal? BestScore { get; set; }
    public Guid? BestAttemptId { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public bool IsLate { get; set; }
}

/// <summary>08/10/2026 - Bài giao (danh sách / chi tiết). Questions chỉ có ở chi tiết của bài "giao lại câu sai"</summary>
public class AssignmentReadModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public short Grade { get; set; }
    public string Subject { get; set; } = "";
    public List<string> TopicIds { get; set; } = new();
    public short Level { get; set; }
    public int QuestionCount { get; set; }
    public int? TimeLimitMin { get; set; }
    public long Seed { get; set; }
    public DateTime DueDate { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "";
    public bool HasQuestions { get; set; }
    public Guid? SourceId { get; set; }
    public DateTime TimeCr { get; set; }
    public string TopicKey { get; set; } = "";
    public JsonElement? Questions { get; set; }
    public List<AssignmentStudentReadModel> Students { get; set; } = new();
    /// <summary>Phía học sinh: tình trạng của chính con</summary>
    public AssignmentStudentReadModel? Mine { get; set; }
}

/// <summary>08/10/2026 - 1 lượt làm bài giao (trang kết quả của phụ huynh)</summary>
public class AssignmentAttemptReadModel
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public int Total { get; set; }
    public int Correct { get; set; }
    public decimal? Score10 { get; set; }
    public int DurationSec { get; set; }
    public DateTime FinishedAt { get; set; }
    public bool IsLate { get; set; }
}

/// <summary>08/10/2026 - Kết quả bài giao: bài + từng con + các lượt</summary>
public class AssignmentResultReadModel
{
    public AssignmentReadModel Assignment { get; set; } = new();
    public List<AssignmentAttemptReadModel> Attempts { get; set; } = new();
}
