namespace HocTap.Infrastructure.Entities;

/// <summary>14/10/2026 - Bảng sys_setting: tham số hệ thống khoá – giá trị (khoá chính key; script sql-dump/20261014_hoctap_phase9.sql)</summary>
public class sys_setting
{
    public string key { get; set; } = "";
    public string value { get; set; } = "";
    public string? note { get; set; }
    public DateTime? timeup { get; set; }
    public Guid? userup { get; set; }
}
