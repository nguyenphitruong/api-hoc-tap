using HocTap.Domain.Aggregates.Admin;
using HocTap.Domain.Aggregates.Assignments;
using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Bases;
using HocTap.Infrastructure.Repositories;

namespace HocTap.Infrastructure.Persistence;

/// <summary>
/// 08/10/2026 - Đơn vị công việc (giống UnitOfWork_Context của Web-MultiClinic): 1 DbContext / request (DI quản lý vòng đời),
/// repository tạo khi dùng lần đầu và dùng chung DbContext đó.
///
/// Transaction: CSDL Neon có thể cắt kết nối -> DbContext bật EnableRetryOnFailure (thử lại khi lỗi tạm thời).
/// EF không cho mở transaction tay khi bật thử lại, nên:
///   - InitTransaction : đánh dấu bắt đầu, xoá thay đổi chưa lưu còn sót
///   - Save            : SaveChanges — EF tự gói mọi Insert / Update trong 1 transaction CSDL (lỗi kết nối thì tự chạy lại)
///   - CommitTransaction: kết thúc (dữ liệu đã ghi ở Save)
///   - RollbackTransaction: bỏ mọi thay đổi chưa lưu trong DbContext
/// => Mỗi thao tác ghi của Service chỉ gọi Save() 1 lần để đảm bảo "tất cả hoặc không gì cả".
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly HocTapDbContext _context;
    private bool _inTransaction;

    public UnitOfWork(HocTapDbContext i_context)
    {
        _context = i_context;
    }

    private IUserRepository? userRepo;
    public IUserRepository UserRepo
    {
        get
        {
            if (this.userRepo == null)
            {
                this.userRepo = new UserRepository(_context);
            }
            return userRepo;
        }
    }

    private IStudentRepository? studentRepo;
    public IStudentRepository StudentRepo
    {
        get
        {
            if (this.studentRepo == null)
            {
                this.studentRepo = new StudentRepository(_context);
            }
            return studentRepo;
        }
    }

    private IAttemptRepository? attemptRepo;
    public IAttemptRepository AttemptRepo
    {
        get
        {
            if (this.attemptRepo == null)
            {
                this.attemptRepo = new AttemptRepository(_context);
            }
            return attemptRepo;
        }
    }

    private IAssignmentRepository? assignmentRepo;
    public IAssignmentRepository AssignmentRepo
    {
        get
        {
            if (this.assignmentRepo == null)
            {
                this.assignmentRepo = new AssignmentRepository(_context);
            }
            return assignmentRepo;
        }
    }

    private IAdminRepository? adminRepo;
    public IAdminRepository AdminRepo
    {
        get
        {
            if (this.adminRepo == null)
            {
                this.adminRepo = new AdminRepository(_context);
            }
            return adminRepo;
        }
    }

    public void InitTransaction()
    {
        if (!_inTransaction)
        {
            _context.ChangeTracker.Clear();
        }
        _inTransaction = true;
    }

    public void Save()
    {
        _context.SaveChanges();
    }

    public void CommitTransaction()
    {
        _inTransaction = false;
    }

    public void RollbackTransaction()
    {
        _context.ChangeTracker.Clear();
        _inTransaction = false;
    }
}
