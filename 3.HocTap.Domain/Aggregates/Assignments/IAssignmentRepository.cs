using HocTap.Domain.Aggregates.Attempts;

namespace HocTap.Domain.Aggregates.Assignments;

/// <summary>08/10/2026 - Dòng bài giao của 1 con kèm tên gọi / biểu tượng (trang phụ huynh)</summary>
public class AssignmentStudentRow
{
    public AssignmentStudentModel Row { get; set; } = new();
    public string NickName { get; set; } = "";
    public string Avatar { get; set; } = "";
}

/// <summary>08/10/2026 - Bài đang giao cho 1 con kèm dòng tình trạng của con</summary>
public class AssignmentOfStudentRow
{
    public AssignmentModel Assignment { get; set; } = new();
    public AssignmentStudentModel Row { get; set; } = new();
}

/// <summary>08/10/2026 - Repository bài giao (assignment, assignment_student) + đọc lượt làm bài giao (attempt)</summary>
public interface IAssignmentRepository
{
    void InsertAssignment(AssignmentModel i_assignment, List<AssignmentStudentModel> i_lstStudent);
    void UpdateAssignment(AssignmentModel i_assignment);
    AssignmentModel? GetAssignmentById(Guid i_id);
    /// <summary>Bài của phụ huynh, mới nhất trước (questions chỉ cho biết có / không: "" hoặc null)</summary>
    List<AssignmentModel> GetListAssignmentByParent(Guid i_parentId, int i_max);
    List<AssignmentStudentRow> GetListAssignmentStudentRow(List<Guid> i_lstAssignmentId);
    AssignmentStudentModel? GetAssignmentStudent(Guid i_assignmentId, Guid i_studentId);
    void UpdateAssignmentStudent(AssignmentStudentModel i_row);
    /// <summary>Bài đang giao (DANGGIAO) cho 1 con, hạn nộp gần trước</summary>
    List<AssignmentOfStudentRow> GetListOpenAssignmentOfStudent(Guid i_studentId);
    List<AttemptModel> GetListAttemptOfAssignment(Guid i_assignmentId);
    AttemptModel? GetLatestAttemptOfStudent(Guid i_assignmentId, Guid i_studentId);
}
