namespace HocTap.Infrastructure.Entities;

/// <summary>08/10/2026 - Bảng student_profile: hồ sơ con (chỉ tên gọi, lớp, biểu tượng; không lưu thông tin cá nhân của trẻ)</summary>
public class student_profile
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
