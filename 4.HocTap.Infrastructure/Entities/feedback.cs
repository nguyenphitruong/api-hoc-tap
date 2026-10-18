namespace HocTap.Infrastructure.Entities;

/// <summary>18/10/2026 - Bảng feedback: góp ý của phụ huynh / khách, báo lỗi câu hỏi (script sql-dump/20261018_hoctap_phase11.sql)</summary>
public class feedback
{
    public Guid id { get; set; }
    public Guid? userid { get; set; }
    public Guid? studentid { get; set; }
    public string? guestid { get; set; }
    public string kind { get; set; } = "";
    public short? rating { get; set; }
    public short? grade { get; set; }
    public string? subject { get; set; }
    public string? topicid { get; set; }
    public string? question { get; set; }
    public string content { get; set; } = "";
    public bool contactok { get; set; }
    public string? contact { get; set; }
    public string? page { get; set; }
    public string status { get; set; } = "";
    public string? adminnote { get; set; }
    public DateTime timecr { get; set; }
    public DateTime? timeup { get; set; }
    public Guid? userup { get; set; }
}
