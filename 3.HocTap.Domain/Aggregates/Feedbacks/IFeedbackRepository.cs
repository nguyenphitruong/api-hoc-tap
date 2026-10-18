namespace HocTap.Domain.Aggregates.Feedbacks;

/// <summary>18/10/2026 - Repository góp ý / báo lỗi câu hỏi (feedback)</summary>
public interface IFeedbackRepository
{
    /// <summary>Thêm 1 góp ý; Service gọi unitOfWork.Save()</summary>
    void InsertFeedback(FeedbackModel i_feedback);
    /// <summary>Số góp ý của 1 tài khoản (hoặc 1 mã khách) từ thời điểm i_since — chặn gửi quá nhiều</summary>
    int CountFeedbackSince(Guid? i_userId, string? i_guestId, DateTime i_since);
    /// <summary>Mọi góp ý, mới nhất trước (lọc / phân trang ở Service; số lượng nhỏ)</summary>
    List<FeedbackModel> GetListFeedback();
    FeedbackModel? GetFeedbackById(Guid i_id);
    /// <summary>Cập nhật trạng thái / ghi chú; Service gọi unitOfWork.Save()</summary>
    void UpdateFeedback(FeedbackModel i_feedback);
}
