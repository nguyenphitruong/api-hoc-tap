namespace HocTap.Domain.Aggregates.Attempts;

/// <summary>
/// 08/10/2026 - Model Bảng attempt: 1 lượt luyện tập / kiểm tra (items = các câu của lượt kiểm tra).
/// Tên thuộc tính trùng tên cột (chữ thường) như entity attempt ở tầng Infrastructure;
/// repository chuyển đổi entity <-> model qua HocTapEntityMapper (MappingRepos).
/// </summary>
public class AttemptModel
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

    public List<AttemptLineModel> items { get; set; } = new();
}
