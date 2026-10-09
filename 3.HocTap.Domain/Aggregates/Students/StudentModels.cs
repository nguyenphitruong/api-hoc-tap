
namespace HocTap.Domain.Aggregates.Students;

/// <summary>
/// 08/10/2026 - Thêm / sửa hồ sơ con. Pin: 4 chữ số; để trống khi sửa = giữ PIN cũ; ClearPin = bỏ PIN.
/// </summary>
public class StudentSaveModel
{
    public string? NickName { get; set; }
    public short Grade { get; set; }
    public string? Avatar { get; set; }
    public string? Pin { get; set; }
    public bool ClearPin { get; set; }
}

/// <summary>08/10/2026 - Hồ sơ con trả về client (không trả PIN, chỉ cho biết có PIN hay không)</summary>
public class StudentReadModel
{
    public Guid Id { get; set; }
    public string NickName { get; set; } = "";
    public short Grade { get; set; }
    public string Avatar { get; set; } = "";
    public bool HasPin { get; set; }

    public static StudentReadModel From(StudentProfileModel s) => new()
    {
        Id = s.id, NickName = s.nickname, Grade = s.grade, Avatar = s.avatar, HasPin = !string.IsNullOrEmpty(s.pinhash),
    };
}
