namespace HocTap.Infrastructure.Entities;

/// <summary>
/// 08/10/2026 - Bảng refresh_token: chỉ lưu SHA-256 của token; studentid có giá trị khi là phiên học sinh (chọn hồ sơ + PIN).
/// Làm mới token = thu hồi bản cũ + cấp bản mới (xoay vòng).
/// </summary>
public class refresh_token
{
    public Guid id { get; set; }
    public Guid userid { get; set; }
    public Guid? studentid { get; set; }
    public string tokenhash { get; set; } = "";
    public DateTime expires { get; set; }
    public bool revoked { get; set; }
    public string? device { get; set; }
    public DateTime timecr { get; set; }
}
