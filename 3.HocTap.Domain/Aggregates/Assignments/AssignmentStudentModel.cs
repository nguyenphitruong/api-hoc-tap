namespace HocTap.Domain.Aggregates.Assignments;

/// <summary>
/// 08/10/2026 - Model Bảng assignment_student: bài giao cho từng con.
/// Tên thuộc tính trùng tên cột (chữ thường) như entity assignment_student ở tầng Infrastructure;
/// repository chuyển đổi entity <-> model qua HocTapEntityMapper (MappingRepos).
/// </summary>
public class AssignmentStudentModel
{
    public Guid assignmentid { get; set; }
    public Guid studentid { get; set; }
    public string status { get; set; } = "";
    public decimal? bestscore { get; set; }
    public Guid? bestattemptid { get; set; }
    public int attemptcount { get; set; }
    public DateTime? submittedat { get; set; }
    public DateTime? lastattemptat { get; set; }
    public bool islate { get; set; }
}
