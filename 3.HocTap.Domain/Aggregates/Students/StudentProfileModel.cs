namespace HocTap.Domain.Aggregates.Students;

/// <summary>
/// 08/10/2026 - Model Bảng student_profile: hồ sơ con.
/// Tên thuộc tính trùng tên cột (chữ thường) như entity student_profile ở tầng Infrastructure;
/// repository chuyển đổi entity <-> model qua HocTapEntityMapper (MappingRepos).
/// </summary>
public class StudentProfileModel
{
    public Guid id { get; set; }
    public Guid userid { get; set; }
    public string nickname { get; set; } = "";
    public short grade { get; set; }
    public string avatar { get; set; } = "";
    public string? pinhash { get; set; }
    public bool active { get; set; }
    public DateTime timecr { get; set; }
    public DateTime? timeup { get; set; }
}
