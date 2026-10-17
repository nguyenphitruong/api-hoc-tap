namespace HocTap.Infrastructure.Entities;

/// <summary>17/10/2026 - Bảng guest_activity: hoạt động khách học thử (script sql-dump/20261017_hoctap_phase10.sql)</summary>
public class guest_activity
{
    public Guid id { get; set; }
    public string guestid { get; set; } = "";
    public string kind { get; set; } = "";
    public short? grade { get; set; }
    public string? subject { get; set; }
    public string? topicid { get; set; }
    public int? total { get; set; }
    public int? correct { get; set; }
    public decimal? score10 { get; set; }
    public int? durationsec { get; set; }
    public string? note { get; set; }
    public DateTime timecr { get; set; }
    public Guid? userid { get; set; }
    public string? linkvia { get; set; }
    public DateTime? linkedat { get; set; }
}
