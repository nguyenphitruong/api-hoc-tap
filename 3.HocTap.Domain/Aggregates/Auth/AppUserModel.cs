namespace HocTap.Domain.Aggregates.Auth;

/// <summary>
/// 08/10/2026 - Model Bảng app_user: tài khoản phụ huynh / admin.
/// Tên thuộc tính trùng tên cột (chữ thường) như entity app_user ở tầng Infrastructure;
/// repository chuyển đổi entity <-> model qua HocTapEntityMapper (MappingRepos).
/// </summary>
public class AppUserModel
{
    public Guid id { get; set; }
    public string? email { get; set; }
    public string? phone { get; set; }
    public string passwordhash { get; set; } = "";
    public string fullname { get; set; } = "";
    public string role { get; set; } = "";
    public string status { get; set; } = "";
    public DateTime trialend { get; set; }
    public DateTime? planend { get; set; }
    public DateTime? lastlogin { get; set; }
    public DateTime timecr { get; set; }
    public DateTime? timeup { get; set; }
}
