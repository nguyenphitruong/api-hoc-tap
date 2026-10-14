namespace HocTap.Infrastructure.Entities;

/// <summary>14/10/2026 - Bảng plan_price: bảng giá gói gia hạn (khoá chính code; script sql-dump/20261014_hoctap_phase9.sql)</summary>
public class plan_price
{
    public string code { get; set; } = "";
    public string name { get; set; } = "";
    public int months { get; set; }
    public int subjects { get; set; }
    public decimal price { get; set; }
    public string? note { get; set; }
    public int sortorder { get; set; }
    public bool isactive { get; set; }
    public DateTime timecr { get; set; }
    public DateTime? timeup { get; set; }
    public Guid? userup { get; set; }
}
