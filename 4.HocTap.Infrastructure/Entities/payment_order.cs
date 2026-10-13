namespace HocTap.Infrastructure.Entities;

/// <summary>12/10/2026 - Bảng payment_order: đơn gia hạn gói bằng chuyển khoản QR (script sql-dump/20261012_hoctap_phase6.sql)</summary>
public class payment_order
{
    public Guid id { get; set; }
    public string code { get; set; } = "";
    public Guid userid { get; set; }
    public string plancode { get; set; } = "";
    public int months { get; set; }
    public decimal amount { get; set; }
    public string status { get; set; } = "";
    public decimal? paidamount { get; set; }
    public string? gateway { get; set; }
    public string? txnref { get; set; }
    public string? txncontent { get; set; }
    public DateTime? planfrom { get; set; }
    public DateTime? planto { get; set; }
    public DateTime timecr { get; set; }
    public DateTime? paidat { get; set; }
    public DateTime? timeup { get; set; }
    public Guid? studentid { get; set; }        // 13/10/2026 - phase8: con được gia hạn
    public string? subjects { get; set; }       // 13/10/2026 - phase8: "toan" | "anh" | "anh,toan"
}
