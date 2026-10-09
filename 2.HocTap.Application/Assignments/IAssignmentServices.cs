using HocTap.Domain.Aggregates.Assignments;
using HocTap.Domain.Bases;

namespace HocTap.Application.Assignments;

/// <summary>08/10/2026 - Service giao bài tập (assignment / assignment_student)</summary>
public interface IAssignmentServices
{
    /// <summary>Danh sách bài phụ huynh đã giao (tối đa 200, mới nhất trước) kèm tình trạng từng con</summary>
    List<AssignmentReadModel> GetListAssignment(CurrentUser i_caller);
    AssignmentReadModel InsertAssignment(CurrentUser i_caller, AssignmentCreateModel i_model);
    AssignmentReadModel UpdateAssignment(CurrentUser i_caller, Guid i_id, AssignmentUpdateModel i_model);
    /// <summary>Giao lại các câu sai ở lượt gần nhất của 1 con</summary>
    AssignmentReadModel InsertRetryWrong(CurrentUser i_caller, Guid i_id, AssignmentRetryModel i_model);
    AssignmentResultReadModel GetAssignmentResult(CurrentUser i_caller, Guid i_id);
    AssignmentReadModel GetAssignmentDetail(CurrentUser i_caller, Guid i_id);
    /// <summary>Bài đang giao cho 1 con (trang học sinh)</summary>
    List<AssignmentReadModel> GetListAssignmentOfStudent(CurrentUser i_caller, Guid i_studentId);
}
