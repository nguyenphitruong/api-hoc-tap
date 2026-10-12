namespace HocTap.Infrastructure.Entities;

/// <summary>08/10/2026 - Bảng app_user: tài khoản phụ huynh / admin (tên cột chữ thường như CSDL)</summary>
public class app_user
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
    public string? termsversion { get; set; }       // 12/10/2026 - phiên bản điều khoản đã đồng ý khi đăng ký
    public DateTime? termsacceptedat { get; set; }
}
