namespace HocTap.Domain.Aggregates.Guests;

/// <summary>
/// 17/10/2026 - Model bảng guest_activity: 1 hoạt động của khách học thử (chưa đăng nhập).
/// guestid  : mã ngẫu nhiên trình duyệt tự tạo (g_xxxxxxxx…), không chứa thông tin cá nhân.
/// kind     : TRUYCAP (vào web, tối đa 1 lần / ngày) | LUYENTAP | KIEMTRA | KHOA (gặp giới hạn học thử).
/// userid / linkvia / linkedat: khi trình duyệt đó đăng ký (DANGKY) hoặc đăng nhập tài khoản có sẵn (DANGNHAP) thì gắn tài khoản.
/// </summary>
public class GuestActivityModel
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
    public string? note { get; set; }                  // KHOA: lý do (guest | quota)
    public DateTime timecr { get; set; }
    public Guid? userid { get; set; }
    public string? linkvia { get; set; }               // DANGKY | DANGNHAP
    public DateTime? linkedat { get; set; }
}

/// <summary>17/10/2026 - Dữ liệu web gửi lên POST api/v1/guest/events (không cần đăng nhập)</summary>
public class GuestEventModel
{
    public string? GuestId { get; set; }
    public string? Kind { get; set; }
    public short? Grade { get; set; }
    public string? Subject { get; set; }
    public string? TopicId { get; set; }
    public int? Total { get; set; }
    public int? Correct { get; set; }
    public decimal? Score10 { get; set; }
    public int? DurationSec { get; set; }
    public string? Note { get; set; }
}

/* ===================== THỐNG KÊ TRANG QUẢN TRỊ (tab Học thử) ===================== */

/// <summary>17/10/2026 - Thống kê khách học thử trong N ngày gần nhất (giờ Việt Nam)</summary>
public class AdminGuestStatsReadModel
{
    public int Days { get; set; }
    public int Guests { get; set; }                    // số trình duyệt khách có hoạt động trong kỳ
    public int Learners { get; set; }                  // có làm ít nhất 1 bài (luyện tập / kiểm tra)
    public int Returning { get; set; }                 // quay lại từ 2 ngày trở lên
    public int LockHits { get; set; }                  // gặp giới hạn học thử
    public int Registered { get; set; }                // đã đăng ký tài khoản mới từ trình duyệt đó
    public int ExistingUsers { get; set; }             // đăng nhập tài khoản có sẵn (không tính là chuyển đổi)
    public decimal ConversionPct { get; set; }         // Registered / Guests × 100 (1 chữ số thập phân)
    public decimal LearnerConversionPct { get; set; }  // Registered (trong số Learners) / Learners × 100
    public List<GuestDayReadModel> DayStats { get; set; } = new();
    public List<GuestBucketReadModel> Funnel { get; set; } = new();
    public List<GuestCountReadModel> Grades { get; set; } = new();
    public List<GuestTopicReadModel> TopTopics { get; set; } = new();
    public List<GuestRowReadModel> Recent { get; set; } = new();
}

public class GuestDayReadModel
{
    public string Day { get; set; } = "";              // yyyy-MM-dd (giờ VN)
    public int NewGuests { get; set; }
    public int ActiveGuests { get; set; }
    public int Registered { get; set; }
}

public class GuestBucketReadModel
{
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public int Guests { get; set; }
    public int Registered { get; set; }
}

public class GuestCountReadModel
{
    public string Key { get; set; } = "";
    public int Guests { get; set; }
}

public class GuestTopicReadModel
{
    public string TopicId { get; set; } = "";
    public int Guests { get; set; }
    public int Activities { get; set; }
}

public class GuestRowReadModel
{
    public string GuestId { get; set; } = "";          // rút gọn 8 ký tự cuối (đủ phân biệt, không cần lộ cả mã)
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public int ActiveDays { get; set; }
    public int Practices { get; set; }
    public int Tests { get; set; }
    public decimal? AvgScore { get; set; }
    public int LockHits { get; set; }
    public string Grades { get; set; } = "";           // vd "2, 6"
    public string Status { get; set; } = "";           // CHUA | DANGKY | DANGNHAP
    public string? UserName { get; set; }
    public string? UserContact { get; set; }
    public DateTime? LinkedAt { get; set; }
}
