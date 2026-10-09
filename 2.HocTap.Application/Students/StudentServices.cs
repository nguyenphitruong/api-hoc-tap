using System.Text.RegularExpressions;
using HocTap.Application.Auth;
using HocTap.Application.Commons;
using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;

namespace HocTap.Application.Students;

/// <summary>
/// 08/10/2026 - HỒ SƠ CON (viết tường minh theo mẫu Web-MultiClinic)
/// - Tối đa 4 hồ sơ còn hoạt động / tài khoản; tên gọi 1–24 ký tự, không trùng trong tài khoản; lớp 2 hoặc 6.
/// - PIN 4 chữ số (BCrypt), tuỳ chọn. Xoá = ẩn (active = false), lịch sử học vẫn giữ.
/// - Học sinh (phiên HOCSINH) được xem danh sách để đổi hồ sơ, không được thêm / sửa / xoá.
/// Luồng ghi: kiểm tra -> InitTransaction -> unitOfWork.StudentRepo.Insert/Update -> Save -> Commit; lỗi -> Rollback.
/// </summary>
public class StudentServices : IStudentServices
{
    private static readonly Regex PIN_RE = new(@"^\d{4}$", RegexOptions.Compiled);
    private const string DEFAULT_AVATAR = "🐣";

    private readonly IUnitOfWork unitOfWork;
    private readonly IPasswordHasher passwordHasher;
    private readonly IClock clock;

    public StudentServices(IUnitOfWork i_UnitOfWork, IPasswordHasher i_PasswordHasher, IClock i_Clock)
    {
        unitOfWork = i_UnitOfWork;
        passwordHasher = i_PasswordHasher;
        clock = i_Clock;
    }

    public List<StudentReadModel> GetListStudent(CurrentUser i_caller)
    {
        List<StudentReadModel> lstResult = new List<StudentReadModel>();
        List<StudentProfileModel> lstStudent = unitOfWork.StudentRepo.GetListStudentByUser(i_caller.UserId);
        foreach (StudentProfileModel student in lstStudent)
        {
            lstResult.Add(StudentReadModel.From(student));
        }
        return lstResult;
    }

    /// <summary>Thêm hồ sơ: chỉ phụ huynh; kiểm tra tên / PIN / lớp / số hồ sơ tối đa</summary>
    public StudentReadModel InsertStudent(CurrentUser i_caller, StudentSaveModel i_model)
    {
        StudentReadModel _Result = new StudentReadModel();

        // 1. Kiểm tra
        ServiceGuard.EnsureParent(i_caller);
        string nickName = ValidateNickName(i_caller, i_model, null);
        string avatar = NormalizeAvatar(i_model.Avatar);
        if (!HocTapConst.GRADES.Contains(i_model.Grade)) throw new LogicException("Lớp chỉ được chọn 2 hoặc 6.");
        if (unitOfWork.StudentRepo.CountActiveStudent(i_caller.UserId) >= HocTapConst.MAX_PROFILES)
        {
            throw new LogicException($"Mỗi tài khoản tối đa {HocTapConst.MAX_PROFILES} hồ sơ học sinh.");
        }

        // 2. Tạo model
        StudentProfileModel student = new StudentProfileModel();
        student.id = Guid.NewGuid();
        student.userid = i_caller.UserId;
        student.nickname = nickName;
        student.grade = i_model.Grade;
        student.avatar = avatar;
        student.pinhash = string.IsNullOrEmpty(i_model.Pin) ? null : passwordHasher.Hash(i_model.Pin);
        student.active = true;
        student.timecr = clock.UtcNow;

        // 3. Lưu
        try
        {
            unitOfWork.InitTransaction();
            unitOfWork.StudentRepo.InsertStudent(student);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }

        _Result = StudentReadModel.From(student);
        return _Result;
    }

    /// <summary>Sửa hồ sơ: Grade = 0 giữ lớp cũ; ClearPin bỏ PIN; Pin trống giữ PIN cũ</summary>
    public StudentReadModel UpdateStudent(CurrentUser i_caller, Guid i_id, StudentSaveModel i_model)
    {
        StudentReadModel _Result = new StudentReadModel();

        // 1. Kiểm tra
        ServiceGuard.EnsureParent(i_caller);
        StudentProfileModel student = ServiceGuard.GetOwnedStudent(unitOfWork, i_caller, i_id);
        string nickName = ValidateNickName(i_caller, i_model, i_id);
        string avatar = NormalizeAvatar(i_model.Avatar);
        if (i_model.Grade != 0 && !HocTapConst.GRADES.Contains(i_model.Grade)) throw new LogicException("Lớp chỉ được chọn 2 hoặc 6.");

        // 2. Gán giá trị mới
        student.nickname = nickName;
        student.avatar = avatar;
        if (i_model.Grade != 0) student.grade = i_model.Grade;
        if (i_model.ClearPin) student.pinhash = null;
        else if (!string.IsNullOrEmpty(i_model.Pin)) student.pinhash = passwordHasher.Hash(i_model.Pin);
        student.timeup = clock.UtcNow;

        // 3. Lưu
        try
        {
            unitOfWork.InitTransaction();
            unitOfWork.StudentRepo.UpdateStudent(student);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }

        _Result = StudentReadModel.From(student);
        return _Result;
    }

    public void DeleteStudent(CurrentUser i_caller, Guid i_id)
    {
        ServiceGuard.EnsureParent(i_caller);
        StudentProfileModel student = ServiceGuard.GetOwnedStudent(unitOfWork, i_caller, i_id);
        student.active = false;
        student.timeup = clock.UtcNow;

        try
        {
            unitOfWork.InitTransaction();
            unitOfWork.StudentRepo.UpdateStudent(student);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
    }

    // ===== Nội bộ =====

    /// <summary>Tên gọi 1–24 ký tự, không trùng (không phân biệt hoa thường) trong tài khoản; PIN đúng 4 chữ số</summary>
    private string ValidateNickName(CurrentUser i_caller, StudentSaveModel i_model, Guid? i_selfId)
    {
        string nickName = (i_model.NickName ?? "").Trim();
        if (nickName.Length < 1 || nickName.Length > 24) throw new LogicException("Tên gọi của con từ 1 đến 24 ký tự.");
        if (!string.IsNullOrEmpty(i_model.Pin) && !PIN_RE.IsMatch(i_model.Pin)) throw new LogicException("Mã PIN gồm đúng 4 chữ số.");

        List<StudentProfileModel> lstStudent = unitOfWork.StudentRepo.GetListStudentByUser(i_caller.UserId);
        foreach (StudentProfileModel item in lstStudent)
        {
            if (item.id != i_selfId && string.Equals(item.nickname, nickName, StringComparison.OrdinalIgnoreCase))
            {
                throw new LogicException("Tên gọi này đã có trong tài khoản.");
            }
        }
        return nickName;
    }

    /// <summary>Biểu tượng trống hoặc dài quá 16 ký tự -> mặc định</summary>
    private static string NormalizeAvatar(string? i_avatar)
    {
        string avatar = string.IsNullOrWhiteSpace(i_avatar) ? DEFAULT_AVATAR : i_avatar.Trim();
        if (avatar.Length > 16) avatar = DEFAULT_AVATAR;
        return avatar;
    }
}
