using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;

namespace HocTap.Application.Commons;

/// <summary>
/// 08/10/2026 - Kiểm tra quyền dùng chung cho các Service:
/// - EnsureParent / EnsureAdmin: đúng vai trò người gọi
/// - GetOwnedStudent: hồ sơ con phải thuộc tài khoản đang đăng nhập, còn hoạt động;
///   phiên học sinh chỉ được xem chính hồ sơ của mình.
/// </summary>
public static class ServiceGuard
{
    public static void EnsureParent(CurrentUser i_caller)
    {
        if (i_caller.Role != HocTapConst.ROLE_PARENT)
        {
            throw new LogicException("Chức năng dành cho phụ huynh.", 403);
        }
    }

    public static void EnsureAdmin(CurrentUser i_caller)
    {
        if (i_caller.Role != HocTapConst.ROLE_ADMIN)
        {
            throw new LogicException("Chức năng dành cho quản trị.", 403);
        }
    }

    public static StudentProfileModel GetOwnedStudent(IUnitOfWork i_unitOfWork, CurrentUser i_caller, Guid i_studentId)
    {
        StudentProfileModel? student = i_unitOfWork.StudentRepo.GetStudentById(i_studentId);
        if (student == null || !student.active || student.userid != i_caller.UserId)
        {
            throw new LogicException("Không tìm thấy hồ sơ học sinh.", 404);
        }
        if (i_caller.Role == HocTapConst.ROLE_STUDENT && i_caller.StudentId != i_studentId)
        {
            throw new LogicException("Không xem được hồ sơ của bạn khác.", 403);
        }
        return student;
    }
}
