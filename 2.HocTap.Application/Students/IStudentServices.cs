using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Bases;

namespace HocTap.Application.Students;

/// <summary>08/10/2026 - Service hồ sơ con (student_profile)</summary>
public interface IStudentServices
{
    /// <summary>Danh sách hồ sơ còn hoạt động của tài khoản (phụ huynh hoặc phiên học sinh)</summary>
    List<StudentReadModel> GetListStudent(CurrentUser i_caller);
    StudentReadModel InsertStudent(CurrentUser i_caller, StudentSaveModel i_model);
    StudentReadModel UpdateStudent(CurrentUser i_caller, Guid i_id, StudentSaveModel i_model);
    /// <summary>Xoá = ẩn hồ sơ (active = false), lịch sử học vẫn giữ</summary>
    void DeleteStudent(CurrentUser i_caller, Guid i_id);
}
