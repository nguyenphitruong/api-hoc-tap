using System.Text.RegularExpressions;

namespace HocTap.Domain.Commons;

/// <summary>
/// 08/10/2026 - Quy tắc lượt học (hàm thuần, có xUnit):
/// - Mã chủ đề: g{lớp}.{môn}.{id}; bài kiểm tra chương: ch:g{lớp}.{môn}.{chương}; học kì: hk:g{lớp}.{môn}.{1|2}
/// - 08/10/2026 giai đoạn 4: bài được giao: as:g{lớp}.{môn}.{id bài giao dạng N}
/// - Khoá chống trùng khi nhập: topicid|mode|finishedat(ms)
/// </summary>
public static class AttemptRules
{
    private static readonly Regex TOPIC_RE = new(@"^(?:(ch|hk|as):)?g(2|6)\.(toan|anh)\.([a-z0-9_\-]{1,60})$", RegexOptions.Compiled);

    /// <summary>Tách mã chủ đề; null nếu sai định dạng</summary>
    public static (short Grade, string Subject, string Kind)? ParseTopic(string? topicId)
    {
        if (string.IsNullOrEmpty(topicId) || topicId.Length > 80) return null;
        var m = TOPIC_RE.Match(topicId);
        if (!m.Success) return null;
        return ((short)(m.Groups[2].Value == "2" ? 2 : 6), m.Groups[3].Value, m.Groups[1].Success ? m.Groups[1].Value : "topic");
    }

    public static DateTime FromUnixMs(long ms) => DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
    public static long ToUnixMs(DateTime utc) => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

    public static string Key(string topicId, string mode, DateTime finishedAt) => $"{topicId}|{mode}|{ToUnixMs(finishedAt)}";
}
