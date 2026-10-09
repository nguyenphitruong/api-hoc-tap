using HocTap.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace HocTap.Infrastructure.Persistence;

/// <summary>
/// 08/10/2026 - DbContext PostgreSQL. Bảng tạo bằng script sql-dump/20261008_hoctap_phase3.sql (không dùng migration).
/// Tên bảng / cột chữ thường trùng tên class / thuộc tính.
/// </summary>
public class HocTapDbContext : DbContext
{
    public HocTapDbContext(DbContextOptions<HocTapDbContext> options) : base(options) { }

    public DbSet<app_user> app_user => Set<app_user>();
    public DbSet<refresh_token> refresh_token => Set<refresh_token>();
    public DbSet<student_profile> student_profile => Set<student_profile>();
    public DbSet<attempt> attempt => Set<attempt>();
    public DbSet<attempt_item> attempt_item => Set<attempt_item>();
    public DbSet<assignment> assignment => Set<assignment>();                       // 08/10/2026 - giai đoạn 4
    public DbSet<assignment_student> assignment_student => Set<assignment_student>();
    public DbSet<plan_log> plan_log => Set<plan_log>();                             // 08/10/2026 - giai đoạn 5

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<app_user>().ToTable("app_user").HasKey(x => x.id);
        b.Entity<refresh_token>().ToTable("refresh_token").HasKey(x => x.id);
        b.Entity<student_profile>().ToTable("student_profile").HasKey(x => x.id);
        b.Entity<attempt>(e =>
        {
            e.ToTable("attempt").HasKey(x => x.id);
            e.Property(x => x.score10).HasColumnType("numeric(4,2)");
            e.HasMany(x => x.items).WithOne().HasForeignKey(x => x.attemptid);
        });
        b.Entity<attempt_item>(e =>
        {
            e.ToTable("attempt_item").HasKey(x => x.id);
            e.Property(x => x.question).HasColumnType("jsonb");
            e.Property(x => x.answer).HasColumnType("jsonb");
        });
        // 08/10/2026 - Giai đoạn 4: bài giao (script sql-dump/20261008_hoctap_phase4.sql)
        b.Entity<assignment>(e =>
        {
            e.ToTable("assignment").HasKey(x => x.id);
            e.Property(x => x.topicids).HasColumnType("text[]");
            e.Property(x => x.questions).HasColumnType("jsonb");
        });
        b.Entity<assignment_student>(e =>
        {
            e.ToTable("assignment_student").HasKey(x => new { x.assignmentid, x.studentid });
            e.Property(x => x.bestscore).HasColumnType("numeric(4,2)");
        });
        // 08/10/2026 - Giai đoạn 5: lịch sử gói (script sql-dump/20261008_hoctap_phase5.sql)
        b.Entity<plan_log>().ToTable("plan_log").HasKey(x => x.id);
    }
}
