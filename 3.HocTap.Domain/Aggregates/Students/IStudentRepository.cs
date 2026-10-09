namespace HocTap.Domain.Aggregates.Students;

/// <summary>08/10/2026 - Repository hồ sơ con (student_profile)</summary>
public interface IStudentRepository
{
    /// <summary>Hồ sơ còn hoạt động của tài khoản, sắp theo thời điểm tạo</summary>
    List<StudentProfileModel> GetListStudentByUser(Guid i_userId);
    StudentProfileModel? GetStudentById(Guid i_id);
    int CountActiveStudent(Guid i_userId);
    void InsertStudent(StudentProfileModel i_student);
    void UpdateStudent(StudentProfileModel i_student);
}
