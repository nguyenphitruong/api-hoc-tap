namespace HocTap.Domain.Aggregates.Admin;

/// <summary>
/// 08/10/2026 - Model Bảng plan_log: lịch sử kích hoạt / gia hạn / khoá / mở khoá.
/// Tên thuộc tính trùng tên cột (chữ thường) như entity plan_log ở tầng Infrastructure;
/// repository chuyển đổi entity <-> model qua HocTapEntityMapper (MappingRepos).
/// </summary>
public class PlanLogModel
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
