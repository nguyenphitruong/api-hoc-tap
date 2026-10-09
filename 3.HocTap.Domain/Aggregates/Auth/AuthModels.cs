namespace HocTap.Domain.Aggregates.Auth;

/// <summary>08/10/2026 - Đăng ký tài khoản phụ huynh: cần email hoặc số điện thoại</summary>
public class RegisterModel
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Password { get; set; }
    public string? Device { get; set; }
}

/// <summary>08/10/2026 - Đăng nhập: Login là email hoặc số điện thoại</summary>
public class LoginModel
{
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string? Device { get; set; }
}

public class RefreshModel
{
    public string? RefreshToken { get; set; }
}

/// <summary>08/10/2026 - Vào phiên học sinh: chọn hồ sơ con + PIN (nếu hồ sơ có đặt PIN)</summary>
public class StudentSessionModel
{
    public Guid StudentId { get; set; }
    public string? Pin { get; set; }
    public string? Device { get; set; }
}

/// <summary>08/10/2026 - Thông tin tài khoản trả về client (Status là trạng thái hiệu lực, CanStudy = được làm bài mới)</summary>
public class UserReadModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime TrialEnd { get; set; }
    public DateTime? PlanEnd { get; set; }
    public bool CanStudy { get; set; }
}

/// <summary>08/10/2026 - Kết quả đăng nhập / làm mới token / vào phiên học sinh</summary>
public class AuthResultReadModel
{
    public string AccessToken { get; set; } = "";
    public DateTime AccessExpires { get; set; }
    public string RefreshToken { get; set; } = "";
    public DateTime RefreshExpires { get; set; }
    public UserReadModel User { get; set; } = new();
    public Students.StudentReadModel? Student { get; set; }
}

/// <summary>08/10/2026 - Giai đoạn 5: đổi mật khẩu (phụ huynh / admin)</summary>
public class ChangePasswordModel
{
    public string? OldPassword { get; set; }
    public string? NewPassword { get; set; }
}
