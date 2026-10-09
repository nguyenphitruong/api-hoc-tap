namespace HocTap.Domain.Aggregates.Auth;

/// <summary>
/// 08/10/2026 - Repository tài khoản (app_user) + refresh token (refresh_token).
/// Đọc trả về Model (không theo dõi thay đổi); ghi nhận Model, chuyển sang entity ở tầng Infrastructure.
/// Các hàm ghi chỉ đưa thay đổi vào DbContext, Service gọi unitOfWork.Save() để lưu.
/// </summary>
public interface IUserRepository
{
    AppUserModel? GetUserById(Guid i_id);
    AppUserModel? GetUserByEmail(string i_email);
    AppUserModel? GetUserByPhone(string i_phone);
    void InsertUser(AppUserModel i_user);
    void UpdateUser(AppUserModel i_user);

    RefreshTokenModel? GetRefreshTokenByHash(string i_tokenHash);
    void InsertRefreshToken(RefreshTokenModel i_token);
    void UpdateRefreshToken(RefreshTokenModel i_token);
    /// <summary>Thu hồi mọi refresh token còn hiệu lực của tài khoản (đổi mật khẩu / bị khoá)</summary>
    void RevokeAllRefreshToken(Guid i_userId);
}
