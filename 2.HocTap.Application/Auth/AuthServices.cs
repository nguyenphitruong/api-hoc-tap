using System.Text.RegularExpressions;
using HocTap.Application.Commons;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;

namespace HocTap.Application.Auth;

/// <summary>
/// 08/10/2026 - XÁC THỰC (viết tường minh theo mẫu PayMentServices của Web-MultiClinic)
/// Thao tác ghi: kiểm tra dữ liệu -> unitOfWork.InitTransaction() -> unitOfWork.XxxRepo.Insert/Update -> Save -> Commit;
///               lỗi -> RollbackTransaction rồi ném lại (LogicException -> controller trả 400/401/403).
/// - Đăng ký: tài khoản phụ huynh, trạng thái DUNGTHU 14 ngày, đăng nhập luôn.
/// - Đăng nhập: email hoặc số điện thoại + mật khẩu (BCrypt). Tài khoản KHOA bị chặn; HETHAN vẫn vào xem được.
/// - Refresh: xoay vòng (thu hồi token cũ, cấp token mới, giữ nguyên phiên học sinh nếu có).
/// - Phiên học sinh: phụ huynh (hoặc học sinh khác cùng tài khoản) chọn hồ sơ con + PIN -> token role HOCSINH.
/// - Đổi mật khẩu (giai đoạn 5): kiểm tra mật khẩu cũ, thu hồi mọi phiên.
/// </summary>
public class AuthServices : IAuthServices
{
    private static readonly Regex EMAIL_RE = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PHONE_RE = new(@"^0\d{8,10}$", RegexOptions.Compiled);

    private readonly IUnitOfWork unitOfWork;
    private readonly ITokenService tokenService;
    private readonly IPasswordHasher passwordHasher;
    private readonly IClock clock;

    public AuthServices(IUnitOfWork i_UnitOfWork, ITokenService i_TokenService, IPasswordHasher i_PasswordHasher, IClock i_Clock)
    {
        unitOfWork = i_UnitOfWork;
        tokenService = i_TokenService;
        passwordHasher = i_PasswordHasher;
        clock = i_Clock;
    }

    /// <summary>Đăng ký phụ huynh: kiểm tra họ tên, email / SĐT (ít nhất 1, chưa dùng), mật khẩu ≥ 6 ký tự</summary>
    public AuthResultReadModel Register(RegisterModel i_model)
    {
        AuthResultReadModel _Result = new AuthResultReadModel();

        // 1. Chuẩn hoá + kiểm tra dữ liệu
        string fullName = (i_model.FullName ?? "").Trim();
        string? email = string.IsNullOrWhiteSpace(i_model.Email) ? null : i_model.Email.Trim().ToLowerInvariant();
        string? phone = AccountRules.NormalizePhone(i_model.Phone);
        string password = i_model.Password ?? "";

        if (fullName.Length < 2 || fullName.Length > 100) throw new LogicException("Họ tên phụ huynh từ 2 đến 100 ký tự.");
        if (email == null && phone == null) throw new LogicException("Cần nhập email hoặc số điện thoại.");
        if (email != null && (email.Length > 150 || !EMAIL_RE.IsMatch(email))) throw new LogicException("Email không hợp lệ.");
        if (phone != null && !PHONE_RE.IsMatch(phone)) throw new LogicException("Số điện thoại không hợp lệ.");
        if (password.Length < 6 || password.Length > 100) throw new LogicException("Mật khẩu từ 6 đến 100 ký tự.");
        if (email != null && unitOfWork.UserRepo.GetUserByEmail(email) != null) throw new LogicException("Email đã được đăng ký.");
        if (phone != null && unitOfWork.UserRepo.GetUserByPhone(phone) != null) throw new LogicException("Số điện thoại đã được đăng ký.");

        // 2. Tài khoản mới: dùng thử 14 ngày
        DateTime now = clock.UtcNow;
        AppUserModel user = new AppUserModel();
        user.id = Guid.NewGuid();
        user.email = email;
        user.phone = phone;
        user.fullname = fullName;
        user.passwordhash = passwordHasher.Hash(password);
        user.role = HocTapConst.ROLE_PARENT;
        user.status = HocTapConst.STATUS_TRIAL;
        user.trialend = now.AddDays(HocTapConst.TRIAL_DAYS);
        user.lastlogin = now;
        user.timecr = now;

        // 3. Lưu tài khoản + cấp token
        try
        {
            unitOfWork.InitTransaction();
            unitOfWork.UserRepo.InsertUser(user);
            _Result = IssueTokens(user, null, i_model.Device);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
        return _Result;
    }

    /// <summary>Đăng nhập phụ huynh / admin</summary>
    public AuthResultReadModel Login(LoginModel i_model)
    {
        AuthResultReadModel _Result = new AuthResultReadModel();

        // 1. Tìm tài khoản theo email / SĐT, kiểm tra mật khẩu
        (string? email, string? phone) = AccountRules.NormalizeLogin(i_model.Login);
        AppUserModel? user = null;
        if (email != null) user = unitOfWork.UserRepo.GetUserByEmail(email);
        else if (phone != null) user = unitOfWork.UserRepo.GetUserByPhone(phone);

        if (user == null || !passwordHasher.Verify(i_model.Password ?? "", user.passwordhash))
        {
            throw new LogicException("Sai email / số điện thoại hoặc mật khẩu.");
        }
        if (user.status == HocTapConst.STATUS_LOCKED)
        {
            throw new LogicException("Tài khoản đã bị khoá. Vui lòng liên hệ quản trị.", 403);
        }

        // 2. Ghi lần đăng nhập + cấp token
        try
        {
            unitOfWork.InitTransaction();
            user.lastlogin = clock.UtcNow;
            unitOfWork.UserRepo.UpdateUser(user);
            _Result = IssueTokens(user, null, i_model.Device);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
        return _Result;
    }

    /// <summary>Làm mới token (xoay vòng). Token sai / hết hạn / đã thu hồi -> 401 để client đăng nhập lại</summary>
    public AuthResultReadModel Refresh(RefreshModel i_model)
    {
        AuthResultReadModel _Result = new AuthResultReadModel();
        DateTime now = clock.UtcNow;

        // 1. Token còn hiệu lực?
        RefreshTokenModel? token = null;
        if (!string.IsNullOrEmpty(i_model.RefreshToken))
        {
            token = unitOfWork.UserRepo.GetRefreshTokenByHash(tokenService.HashRefreshToken(i_model.RefreshToken));
        }
        if (token == null || token.revoked || token.expires < now)
        {
            throw new LogicException("Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.", 401);
        }

        // 2. Tài khoản / hồ sơ con còn dùng được?
        AppUserModel? user = unitOfWork.UserRepo.GetUserById(token.userid);
        if (user == null || user.status == HocTapConst.STATUS_LOCKED)
        {
            throw new LogicException("Tài khoản không còn sử dụng được.", 401);
        }
        if (token.studentid.HasValue)
        {
            StudentProfileModel? student = unitOfWork.StudentRepo.GetStudentById(token.studentid.Value);
            if (student == null || !student.active || student.userid != user.id)
            {
                throw new LogicException("Hồ sơ học sinh không còn tồn tại.", 401);
            }
        }

        // 3. Thu hồi token cũ + cấp token mới
        try
        {
            unitOfWork.InitTransaction();
            token.revoked = true;
            unitOfWork.UserRepo.UpdateRefreshToken(token);
            _Result = IssueTokens(user, token.studentid, token.device);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
        return _Result;
    }

    /// <summary>Đăng xuất: thu hồi refresh token (không báo lỗi nếu không tìm thấy)</summary>
    public void Logout(RefreshModel i_model)
    {
        if (string.IsNullOrEmpty(i_model.RefreshToken))
        {
            return;
        }
        RefreshTokenModel? token = unitOfWork.UserRepo.GetRefreshTokenByHash(tokenService.HashRefreshToken(i_model.RefreshToken));
        if (token == null || token.revoked)
        {
            return;
        }
        try
        {
            unitOfWork.InitTransaction();
            token.revoked = true;
            unitOfWork.UserRepo.UpdateRefreshToken(token);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
    }

    /// <summary>
    /// Vào phiên học sinh: người gọi là phụ huynh hoặc học sinh cùng tài khoản (đổi hồ sơ);
    /// hồ sơ phải thuộc tài khoản, còn hoạt động; có PIN thì phải đúng PIN.
    /// </summary>
    public AuthResultReadModel StudentSession(CurrentUser i_caller, StudentSessionModel i_model)
    {
        AuthResultReadModel _Result = new AuthResultReadModel();

        // 1. Kiểm tra người gọi, hồ sơ, PIN
        if (i_caller.Role != HocTapConst.ROLE_PARENT && i_caller.Role != HocTapConst.ROLE_STUDENT)
        {
            throw new LogicException("Chỉ tài khoản phụ huynh mới chọn được hồ sơ học sinh.", 403);
        }
        AppUserModel? user = unitOfWork.UserRepo.GetUserById(i_caller.UserId);
        if (user == null) throw new LogicException("Không tìm thấy tài khoản.", 401);
        if (user.status == HocTapConst.STATUS_LOCKED) throw new LogicException("Tài khoản đã bị khoá.", 403);

        StudentProfileModel? student = unitOfWork.StudentRepo.GetStudentById(i_model.StudentId);
        if (student == null || !student.active || student.userid != user.id)
        {
            throw new LogicException("Không tìm thấy hồ sơ học sinh.", 404);
        }
        if (!string.IsNullOrEmpty(student.pinhash) && !passwordHasher.Verify(i_model.Pin ?? "", student.pinhash))
        {
            throw new LogicException("Mã PIN không đúng.");
        }

        // 2. Cấp token phiên học sinh
        try
        {
            unitOfWork.InitTransaction();
            _Result = IssueTokens(user, student.id, i_model.Device);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
        return _Result;
    }

    /// <summary>Thông tin tài khoản hiện tại (client gọi khi mở app để cập nhật trạng thái dùng thử / hết hạn)</summary>
    public AuthResultReadModel Me(CurrentUser i_caller)
    {
        AuthResultReadModel _Result = new AuthResultReadModel();
        AppUserModel? user = unitOfWork.UserRepo.GetUserById(i_caller.UserId);
        if (user == null) throw new LogicException("Không tìm thấy tài khoản.", 401);
        _Result.User = ToUserReadModel(user);

        if (i_caller.StudentId.HasValue)
        {
            StudentProfileModel? student = unitOfWork.StudentRepo.GetStudentById(i_caller.StudentId.Value);
            if (student == null || !student.active || student.userid != user.id)
            {
                throw new LogicException("Hồ sơ học sinh không còn tồn tại.", 401);
            }
            _Result.Student = StudentReadModel.From(student);
        }
        return _Result;
    }

    /// <summary>
    /// Giai đoạn 5: đổi mật khẩu (phụ huynh / admin, không áp dụng phiên học sinh).
    /// Kiểm tra mật khẩu cũ, mật khẩu mới 6–100 ký tự và khác mật khẩu cũ; đổi xong thu hồi mọi phiên (các máy khác phải đăng nhập lại).
    /// </summary>
    public void ChangePassword(CurrentUser i_caller, ChangePasswordModel i_model)
    {
        // 1. Kiểm tra
        if (i_caller.Role == HocTapConst.ROLE_STUDENT) throw new LogicException("Phiên học sinh không đổi được mật khẩu.", 403);
        AppUserModel? user = unitOfWork.UserRepo.GetUserById(i_caller.UserId);
        if (user == null) throw new LogicException("Không tìm thấy tài khoản.", 401);
        if (!passwordHasher.Verify(i_model.OldPassword ?? "", user.passwordhash)) throw new LogicException("Mật khẩu hiện tại không đúng.");
        string newPassword = i_model.NewPassword ?? "";
        if (newPassword.Length < 6 || newPassword.Length > 100) throw new LogicException("Mật khẩu mới từ 6 đến 100 ký tự.");
        if (newPassword == i_model.OldPassword) throw new LogicException("Mật khẩu mới phải khác mật khẩu cũ.");

        // 2. Lưu mật khẩu mới + thu hồi mọi phiên
        try
        {
            unitOfWork.InitTransaction();
            user.passwordhash = passwordHasher.Hash(newPassword);
            user.timeup = clock.UtcNow;
            unitOfWork.UserRepo.UpdateUser(user);
            unitOfWork.UserRepo.RevokeAllRefreshToken(user.id);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
    }

    // =====================================================================
    // Nội bộ
    // =====================================================================

    /// <summary>Cấp access token + refresh token (thêm dòng refresh_token; Save do hàm gọi thực hiện)</summary>
    private AuthResultReadModel IssueTokens(AppUserModel i_user, Guid? i_studentId, string? i_device)
    {
        DateTime now = clock.UtcNow;
        (string accessToken, DateTime accessExpires) = tokenService.CreateAccessToken(i_user, i_studentId, now);
        string rawRefreshToken = tokenService.NewRefreshToken();
        DateTime refreshExpires = now.AddDays(tokenService.Options.RefreshDays);

        RefreshTokenModel token = new RefreshTokenModel();
        token.id = Guid.NewGuid();
        token.userid = i_user.id;
        token.studentid = i_studentId;
        token.tokenhash = tokenService.HashRefreshToken(rawRefreshToken);
        token.expires = refreshExpires;
        token.revoked = false;
        token.device = Truncate(i_device, 100);
        token.timecr = now;
        unitOfWork.UserRepo.InsertRefreshToken(token);

        AuthResultReadModel _Result = new AuthResultReadModel();
        _Result.AccessToken = accessToken;
        _Result.AccessExpires = accessExpires;
        _Result.RefreshToken = rawRefreshToken;
        _Result.RefreshExpires = refreshExpires;
        _Result.User = ToUserReadModel(i_user);
        if (i_studentId.HasValue)
        {
            StudentProfileModel? student = unitOfWork.StudentRepo.GetStudentById(i_studentId.Value);
            if (student != null) _Result.Student = StudentReadModel.From(student);
        }
        return _Result;
    }

    /// <summary>Thông tin tài khoản trả client: trạng thái hiệu lực + được làm bài mới hay không</summary>
    private UserReadModel ToUserReadModel(AppUserModel i_user)
    {
        string status = AccountRules.EffectiveStatus(i_user.status, i_user.trialend, i_user.planend, clock.UtcNow);
        UserReadModel _Result = new UserReadModel();
        _Result.Id = i_user.id;
        _Result.FullName = i_user.fullname;
        _Result.Email = i_user.email;
        _Result.Phone = i_user.phone;
        _Result.Role = i_user.role;
        _Result.Status = status;
        _Result.TrialEnd = i_user.trialend;
        _Result.PlanEnd = i_user.planend;
        _Result.CanStudy = AccountRules.CanStudy(status);
        return _Result;
    }

    private static string? Truncate(string? i_value, int i_max)
    {
        if (i_value == null) return null;
        return i_value.Length > i_max ? i_value.Substring(0, i_max) : i_value;
    }
}
