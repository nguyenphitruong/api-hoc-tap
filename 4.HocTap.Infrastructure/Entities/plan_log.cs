namespace HocTap.Infrastructure.Entities;

/// <summary>08/10/2026 - Bảng plan_log: lịch sử admin kích hoạt / gia hạn / khoá / mở khoá tài khoản</summary>
public class plan_log
{
    public Guid id { get; set; }
    public Guid userid { get; set; }
    public string action { get; set; } = "";
    public DateTime? fromdate { get; set; }
    public DateTime? todate { get; set; }
    public string? note { get; set; }
    public Guid adminid { get; set; }
    public DateTime timecr { get; set; }
}
