using HocTap.Domain.Aggregates.Admin;
using HocTap.Domain.Aggregates.Assignments;
using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Students;

namespace HocTap.Domain.Bases;

/// <summary>
/// 08/10/2026 - Đơn vị công việc (giống Web-MultiClinic): giữ các repository dùng chung 1 DbContext.
/// Service: unitOfWork.InitTransaction() -> unitOfWork.XxxRepo.Insert/Update... -> unitOfWork.Save() -> unitOfWork.CommitTransaction();
///          lỗi -> unitOfWork.RollbackTransaction() rồi ném lại.
/// Quy ước: mỗi thao tác ghi chỉ gọi Save() 1 lần (SaveChanges của EF tự gói trong 1 transaction CSDL và được thử lại khi mất kết nối).
/// </summary>
public interface IUnitOfWork
{
    IUserRepository UserRepo { get; }
    IStudentRepository StudentRepo { get; }
    IAttemptRepository AttemptRepo { get; }
    IAssignmentRepository AssignmentRepo { get; }
    IAdminRepository AdminRepo { get; }

    void InitTransaction();
    void Save();
    void CommitTransaction();
    void RollbackTransaction();
}
