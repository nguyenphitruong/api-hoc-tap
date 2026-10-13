namespace HocTap.Infrastructure.Entities;

/// <summary>13/10/2026 - Bảng student_plan: hạn học 1 hồ sơ con × 1 môn (khoá chính studentid + subject; script sql-dump/20261013_hoctap_phase8.sql)</summary>
public class student_plan
{
    public Guid studentid { get; set; }
    public string subject { get; set; } = "";
    public DateTime planend { get; set; }
    public DateTime timecr { get; set; }
    public DateTime? timeup { get; set; }
}
