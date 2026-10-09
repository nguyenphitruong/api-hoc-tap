namespace HocTap.Infrastructure.Entities;

/// <summary>
/// 08/10/2026 - Bảng attempt: 1 lượt luyện tập / kiểm tra. topicid là mã chủ đề (g6.toan.taphop) hoặc
/// mã bài kiểm tra chương / học kì (ch:g6.toan.c2, hk:g6.toan.1). assignmentid dùng ở giai đoạn 4.
/// </summary>
public class attempt
{
    public Guid id { get; set; }
    public Guid studentid { get; set; }
    public Guid? assignmentid { get; set; }
    public short grade { get; set; }
    public string subject { get; set; } = "";
    public string topicid { get; set; } = "";
    public string mode { get; set; } = "";
    public short? level { get; set; }
    public long? seed { get; set; }
    public int total { get; set; }
    public int correct { get; set; }
    public decimal? score10 { get; set; }
    public int durationsec { get; set; }
    public DateTime? startedat { get; set; }
    public DateTime finishedat { get; set; }
    public string source { get; set; } = "";
    public DateTime timecr { get; set; }

    public List<attempt_item> items { get; set; } = new();
}
