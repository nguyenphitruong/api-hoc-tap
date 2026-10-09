using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Bases;

namespace HocTap.Application.Auth;

/// <summary>08/10/2026 - Xác thực: đăng ký, đăng nhập, làm mới token, đăng xuất, phiên học sinh, thông tin tài khoản, đổi mật khẩu</summary>
public interface IAuthServices
{
    AuthResultReadModel Register(RegisterModel i_model);
    AuthResultReadModel Login(LoginModel i_model);
    AuthResultReadModel Refresh(RefreshModel i_model);
    void Logout(RefreshModel i_model);
    AuthResultReadModel StudentSession(CurrentUser i_caller, StudentSessionModel i_model);
    AuthResultReadModel Me(CurrentUser i_caller);
    void ChangePassword(CurrentUser i_caller, ChangePasswordModel i_model);
}
