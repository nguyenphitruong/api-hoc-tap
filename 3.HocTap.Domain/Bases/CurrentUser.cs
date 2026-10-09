namespace HocTap.Domain.Bases;

/// <summary>
/// 08/10/2026 - Người gọi API (đọc từ JWT ở controller): UserId là tài khoản phụ huynh / admin;
/// StudentId có giá trị khi đang ở phiên học sinh (Role = HOCSINH).
/// </summary>
public record CurrentUser(Guid UserId, string Role, Guid? StudentId);
