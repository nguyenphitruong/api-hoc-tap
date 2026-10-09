namespace HocTap.Application.Commons;

/// <summary>08/10/2026 - Đồng hồ (UTC) tách ra để xUnit giả lập thời gian hết hạn dùng thử</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
