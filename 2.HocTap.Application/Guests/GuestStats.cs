using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Guests;
using HocTap.Domain.Commons;

namespace HocTap.Application.Guests;

/// <summary>
/// 17/10/2026 - TỔNG HỢP THỐNG KÊ KHÁCH HỌC THỬ (hàm thuần, có xUnit)
/// Đầu vào: mọi hoạt động của các mã khách có hoạt động trong kỳ (repository đã lọc), giờ hiện tại, số ngày, hàm tra tài khoản.
/// - 1 "khách" = 1 mã trình duyệt. Ngày tính theo giờ Việt Nam (UTC+7).
/// - Đã đăng ký = mã khách được gắn với tài khoản tạo MỚI (DANGKY); đăng nhập tài khoản có sẵn (DANGNHAP) tính riêng, không là chuyển đổi.
/// - Không tính trình duyệt đã gắn với tài khoản ADMIN (quản trị viên mở trang đăng nhập).
/// - Ngày đầu tiên của khách tính trên toàn bộ hoạt động đã đọc (kể cả trước kỳ) nên "khách mới" theo ngày không bị đếm trùng.
/// </summary>
public static class GuestStats
{
    private static readonly TimeSpan VN = TimeSpan.FromHours(7);
    public const string STATUS_NONE = "CHUA";
    private const int RECENT = 50;
    private const int TOP = 10;

    private static DateTime VnDay(DateTime i_utc) => (i_utc + VN).Date;

    /// <summary>Đầu ngày (giờ VN) của ngày cách hôm nay (i_days - 1) ngày, đổi về UTC</summary>
    public static DateTime FromUtc(DateTime i_nowUtc, int i_days) => VnDay(i_nowUtc).AddDays(-(i_days - 1)) - VN;

    private class Agg
    {
        public string Id = "";
        public DateTime First, Last;
        public HashSet<DateTime> DaysAll = new();
        public HashSet<DateTime> DaysIn = new();
        public int Practices, Tests, Locks;
        public List<decimal> Scores = new();
        public SortedSet<short> Grades = new();
        public string Status = STATUS_NONE;
        public Guid? UserId;
        public DateTime? LinkedAt;
        public int Activities => Practices + Tests;
    }

    public static AdminGuestStatsReadModel Build(List<GuestActivityModel> i_rows, DateTime i_nowUtc, int i_days, Func<Guid, AppUserModel?> i_user)
    {
        int days = Math.Clamp(i_days, 1, 90);
        DateTime from = FromUtc(i_nowUtc, days);
        AdminGuestStatsReadModel _Result = new AdminGuestStatsReadModel { Days = days };

        // 1. Gom theo mã khách (chỉ giữ khách có hoạt động trong kỳ; số bài / điểm tính trong kỳ)
        Dictionary<string, Agg> map = new Dictionary<string, Agg>();
        foreach (GuestActivityModel r in i_rows)
        {
            if (!map.TryGetValue(r.guestid, out Agg? g)) { g = new Agg { Id = r.guestid, First = r.timecr, Last = r.timecr }; map[r.guestid] = g; }
            if (r.timecr < g.First) g.First = r.timecr;
            if (r.timecr > g.Last) g.Last = r.timecr;
            g.DaysAll.Add(VnDay(r.timecr));
            if (r.userid.HasValue && (g.Status == STATUS_NONE || r.linkvia == GuestRules.VIA_REGISTER))
            {
                g.Status = r.linkvia ?? GuestRules.VIA_LOGIN; g.UserId = r.userid; g.LinkedAt = r.linkedat;
            }
            if (r.timecr < from) continue;
            g.DaysIn.Add(VnDay(r.timecr));
            if (r.grade.HasValue) g.Grades.Add(r.grade.Value);
            if (r.kind == GuestRules.KIND_PRACTICE) g.Practices++;
            else if (r.kind == GuestRules.KIND_TEST) { g.Tests++; if (r.score10.HasValue) g.Scores.Add(r.score10.Value); }
            else if (r.kind == GuestRules.KIND_LOCK) g.Locks++;
        }
        // Update 17/10/2026: bỏ trình duyệt của quản trị viên (mở trang đăng nhập rồi đăng nhập ADMIN) — không phải khách thật
        Dictionary<Guid, AppUserModel?> users = new Dictionary<Guid, AppUserModel?>();
        AppUserModel? UserOf(Guid id) { if (!users.TryGetValue(id, out AppUserModel? u)) { u = i_user(id); users[id] = u; } return u; }
        List<Agg> guests = map.Values.Where(g => g.DaysIn.Count > 0 && !(g.UserId.HasValue && UserOf(g.UserId.Value)?.role == HocTapConst.ROLE_ADMIN)).ToList();

        // 2. Số tổng
        _Result.Guests = guests.Count;
        _Result.Learners = guests.Count(g => g.Activities > 0);
        _Result.Returning = guests.Count(g => g.DaysIn.Count >= 2);
        _Result.LockHits = guests.Count(g => g.Locks > 0);
        _Result.Registered = guests.Count(g => g.Status == GuestRules.VIA_REGISTER);
        _Result.ExistingUsers = guests.Count(g => g.Status == GuestRules.VIA_LOGIN);
        _Result.ConversionPct = Pct(_Result.Registered, _Result.Guests);
        _Result.LearnerConversionPct = Pct(guests.Count(g => g.Activities > 0 && g.Status == GuestRules.VIA_REGISTER), _Result.Learners);

        // 3. Theo ngày (giờ VN)
        DateTime day0 = VnDay(from);
        for (int i = 0; i < days; i++)
        {
            DateTime d = day0.AddDays(i);
            _Result.DayStats.Add(new GuestDayReadModel
            {
                Day = d.ToString("yyyy-MM-dd"),
                NewGuests = guests.Count(g => VnDay(g.First) == d),
                ActiveGuests = guests.Count(g => g.DaysIn.Contains(d)),
                Registered = guests.Count(g => g.Status == GuestRules.VIA_REGISTER && g.LinkedAt.HasValue && VnDay(g.LinkedAt.Value) == d),
            });
        }

        // 4. Phễu theo số bài đã làm
        (string code, string label, Func<Agg, bool> test)[] buckets =
        {
            ("XEM", "Chỉ vào xem, chưa làm bài", g => g.Activities == 0),
            ("B1", "Làm 1 bài", g => g.Activities == 1),
            ("B2_4", "Làm 2–4 bài", g => g.Activities >= 2 && g.Activities <= 4),
            ("B5", "Làm từ 5 bài trở lên", g => g.Activities >= 5),
        };
        foreach (var b in buckets)
        {
            List<Agg> lst = guests.Where(b.test).ToList();
            _Result.Funnel.Add(new GuestBucketReadModel { Code = b.code, Label = b.label, Guests = lst.Count, Registered = lst.Count(g => g.Status == GuestRules.VIA_REGISTER) });
        }

        // 5. Theo lớp + chủ đề học thử nhiều nhất
        _Result.Grades = guests.SelectMany(g => g.Grades.Select(x => new { x, g.Id })).GroupBy(x => x.x).OrderBy(x => x.Key)
            .Select(x => new GuestCountReadModel { Key = x.Key.ToString(), Guests = x.Select(y => y.Id).Distinct().Count() }).ToList();
        _Result.TopTopics = i_rows.Where(r => r.timecr >= from && r.topicid != null && (r.kind == GuestRules.KIND_PRACTICE || r.kind == GuestRules.KIND_TEST))
            .GroupBy(r => r.topicid!)
            .Select(x => new GuestTopicReadModel { TopicId = x.Key, Guests = x.Select(r => r.guestid).Distinct().Count(), Activities = x.Count() })
            .OrderByDescending(x => x.Guests).ThenByDescending(x => x.Activities).ThenBy(x => x.TopicId).Take(TOP).ToList();

        // 6. Khách gần đây
        foreach (Agg g in guests.OrderByDescending(x => x.Last).Take(RECENT))
        {
            GuestRowReadModel row = new GuestRowReadModel
            {
                GuestId = g.Id.Length > 8 ? g.Id[^8..] : g.Id, FirstSeen = g.First, LastSeen = g.Last, ActiveDays = g.DaysIn.Count,
                Practices = g.Practices, Tests = g.Tests, LockHits = g.Locks, Grades = string.Join(", ", g.Grades),
                AvgScore = g.Scores.Count > 0 ? Math.Round(g.Scores.Average(), 1) : null, Status = g.Status, LinkedAt = g.LinkedAt,
            };
            if (g.UserId.HasValue)
            {
                AppUserModel? u = UserOf(g.UserId.Value);
                if (u != null) { row.UserName = u.fullname; row.UserContact = u.phone ?? u.email; }
            }
            _Result.Recent.Add(row);
        }
        return _Result;
    }

    private static decimal Pct(int i_part, int i_total) => i_total == 0 ? 0 : Math.Round(i_part * 100m / i_total, 1);
}
