using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Bases;

namespace HocTap.Application.Attempts;

/// <summary>08/10/2026 - Service lượt học (attempt / attempt_item)</summary>
public interface IAttemptServices
{
    /// <summary>Nộp 1 lượt (phiên học sinh); có AssignmentId thì cập nhật bài được giao</summary>
    AttemptReadModel InsertAttempt(CurrentUser i_caller, AttemptSaveModel i_model);
    /// <summary>Tiến độ: các lượt của 1 hồ sơ, mới nhất trước</summary>
    List<AttemptReadModel> GetListProgress(CurrentUser i_caller, Guid i_studentId);
    /// <summary>Xem lại 1 lượt kèm từng câu</summary>
    AttemptDetailReadModel GetAttemptDetail(CurrentUser i_caller, Guid i_id);
    /// <summary>Phụ huynh nhập lịch sử cũ vào 1 hồ sơ</summary>
    ImportResultReadModel ImportAttempt(CurrentUser i_caller, Guid i_studentId, ImportModel i_model);
}
