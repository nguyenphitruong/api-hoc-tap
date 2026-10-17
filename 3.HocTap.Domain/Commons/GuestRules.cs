using System.Text.RegularExpressions;
using HocTap.Domain.Aggregates.Guests;

namespace HocTap.Domain.Commons;

/// <summary>
/// 17/10/2026 - QUY TẮC KHÁCH HỌC THỬ (guest_activity)
/// - Mã khách: g_ + 16–40 ký tự a-z0-9 (trình duyệt tự tạo, không chứa thông tin cá nhân).
/// - Loại hoạt động: TRUYCAP | LUYENTAP | KIEMTRA | KHOA. Số liệu ngoài khoảng hợp lý bị cắt về khoảng (không báo lỗi để web không phải xử lý).
/// - Gắn tài khoản: DANGKY (đăng ký mới — tính là chuyển đổi) | DANGNHAP (đăng nhập tài khoản có sẵn).
/// </summary>
public static class GuestRules
{
    public const string KIND_VISIT = "TRUYCAP";
    public const string KIND_PRACTICE = "LUYENTAP";
    public const string KIND_TEST = "KIEMTRA";
    public const string KIND_LOCK = "KHOA";
    public static readonly string[] KINDS = { KIND_VISIT, KIND_PRACTICE, KIND_TEST, KIND_LOCK };

    public const string VIA_REGISTER = "DANGKY";
    public const string VIA_LOGIN = "DANGNHAP";

    public const int MAX_PER_DAY = 300;               // 1 mã khách tối đa 300 hoạt động / ngày (chặn gửi rác)
    public const int LINK_DAYS = 90;                  // chỉ gắn các hoạt động trong 90 ngày gần nhất
    public const int MAX_STAT_ROWS = 200000;          // đọc tối đa khi thống kê

    private static readonly Regex GUEST_RE = new(@"^g_[a-z0-9]{16,40}$", RegexOptions.Compiled);
    private static readonly Regex TOPIC_RE = new(@"^[a-z0-9_.:\-]{1,80}$", RegexOptions.Compiled);
    private static readonly Regex NOTE_RE = new(@"^[a-z_]{1,20}$", RegexOptions.Compiled);

    /// <summary>Mã khách hợp lệ (đã chuẩn hoá chữ thường) hoặc null</summary>
    public static string? NormalizeGuestId(string? i_guestId)
    {
        string s = (i_guestId ?? "").Trim().ToLowerInvariant();
        return GUEST_RE.IsMatch(s) ? s : null;
    }

    /// <summary>
    /// Kiểm tra + chuẩn hoá dữ liệu web gửi lên thành Model (chưa có id, timecr).
    /// Sai mã khách / loại hoạt động -> LogicException 400; lớp / môn / chủ đề sai -> bỏ trường đó; số liệu -> cắt về khoảng hợp lệ.
    /// </summary>
    public static GuestActivityModel ToModel(GuestEventModel i_event)
    {
        string guestId = NormalizeGuestId(i_event?.GuestId) ?? throw new LogicException("Mã khách không hợp lệ.");
        string kind = (i_event!.Kind ?? "").Trim().ToUpperInvariant();
        if (!KINDS.Contains(kind)) throw new LogicException("Loại hoạt động không hợp lệ.");

        GuestActivityModel _Result = new GuestActivityModel();
        _Result.guestid = guestId;
        _Result.kind = kind;
        _Result.grade = i_event.Grade.HasValue && HocTapConst.GRADES.Contains(i_event.Grade.Value) ? i_event.Grade : null;
        string subject = (i_event.Subject ?? "").Trim().ToLowerInvariant();
        _Result.subject = HocTapConst.SUBJECTS.Contains(subject) ? subject : null;
        string topic = (i_event.TopicId ?? "").Trim().ToLowerInvariant();
        _Result.topicid = TOPIC_RE.IsMatch(topic) ? topic : null;
        string note = (i_event.Note ?? "").Trim().ToLowerInvariant();
        _Result.note = NOTE_RE.IsMatch(note) ? note : null;

        if (kind == KIND_PRACTICE || kind == KIND_TEST)
        {
            int total = Math.Clamp(i_event.Total ?? 0, 0, 1000);
            _Result.total = total;
            _Result.correct = Math.Clamp(i_event.Correct ?? 0, 0, total);
            _Result.score10 = i_event.Score10.HasValue ? Math.Round(Math.Clamp(i_event.Score10.Value, 0m, 10m), 1) : null;
            _Result.durationsec = i_event.DurationSec.HasValue ? Math.Clamp(i_event.DurationSec.Value, 0, 86400) : null;
        }
        return _Result;
    }
}
