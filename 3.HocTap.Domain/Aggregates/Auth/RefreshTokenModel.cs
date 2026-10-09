namespace HocTap.Domain.Aggregates.Auth;

/// <summary>
/// 08/10/2026 - Model Bảng refresh_token: refresh token (chỉ lưu SHA-256), studentid có giá trị khi là phiên học sinh.
/// Tên thuộc tính trùng tên cột (chữ thường) như entity refresh_token ở tầng Infrastructure;
/// repository chuyển đổi entity <-> model qua HocTapEntityMapper (MappingRepos).
/// </summary>
public class RefreshTokenModel
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
