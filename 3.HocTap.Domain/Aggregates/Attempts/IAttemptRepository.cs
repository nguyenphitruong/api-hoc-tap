namespace HocTap.Domain.Aggregates.Attempts;

/// <summary>08/10/2026 - Repository lượt học (attempt) + từng câu (attempt_item)</summary>
public interface IAttemptRepository
{
    /// <summary>Thêm 1 lượt kèm các câu (i_attempt.items)</summary>
    void InsertAttempt(AttemptModel i_attempt);
    void InsertListAttempt(List<AttemptModel> i_lstAttempt);
    /// <summary>Tiến độ: các lượt của 1 hồ sơ (mới nhất trước, không đọc nội dung câu)</summary>
    List<AttemptReadModel> GetListAttemptByStudent(Guid i_studentId, int i_max);
    AttemptModel? GetAttemptWithItems(Guid i_id);
    /// <summary>Khoá chống trùng khi nhập: topicid|mode|finishedat (ms)</summary>
    HashSet<string> GetAttemptKeys(Guid i_studentId);
}
