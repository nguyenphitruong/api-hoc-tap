using HocTap.Application.Guests;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Guests;
using HocTap.Domain.Commons;

namespace HocTap.Tests;

/// <summary>17/10/2026 - Khách học thử: ghi hoạt động, chặn dữ liệu sai, gắn tài khoản khi đăng ký / đăng nhập, thống kê quản trị</summary>
public class GuestTests
{
    private const string G1 = "g_aaaaaaaaaaaaaaaaaaaa";
    private const string G2 = "g_bbbbbbbbbbbbbbbbbbbb";
    private const string G3 = "g_cccccccccccccccccccc";

    private static GuestEventModel Ev(string g, string kind, string? topic = null, decimal? score = null, short grade = 6) =>
        new GuestEventModel { GuestId = g, Kind = kind, Grade = grade, Subject = "toan", TopicId = topic, Total = 10, Correct = 7, Score10 = score };

    private static RegisterModel Reg(string email, string? guest) =>
        new RegisterModel { FullName = "Phụ huynh B", Email = email, Phone = null, Password = "123456", AcceptTerms = true, GuestId = guest };

    [Fact]
    public void Rules_validate_and_clamp()
    {
        Assert.Throws<LogicException>(() => GuestRules.ToModel(new GuestEventModel { GuestId = "abc", Kind = "LUYENTAP" }));
        Assert.Throws<LogicException>(() => GuestRules.ToModel(new GuestEventModel { GuestId = G1, Kind = "XOA" }));
        var m = GuestRules.ToModel(new GuestEventModel { GuestId = G1.ToUpperInvariant(), Kind = "kiemtra", Grade = 1, Subject = "Van", TopicId = "G6.TOAN.TAPHOP", Total = 5, Correct = 9, Score10 = 12.34m, DurationSec = -5, Note = "x y" });
        Assert.Equal(G1, m.guestid);
        Assert.Equal("KIEMTRA", m.kind);
        Assert.Null(m.grade);
        Assert.Null(m.subject);
        Assert.Equal("g6.toan.taphop", m.topicid);
        Assert.Equal(5, m.correct);
        Assert.Equal(10m, m.score10);
        Assert.Equal(0, m.durationsec);
        Assert.Null(m.note);
        var v = GuestRules.ToModel(new GuestEventModel { GuestId = G1, Kind = "TRUYCAP", Total = 5 });
        Assert.Null(v.total);   // truy cập không có số câu
    }

    [Fact]
    public void Track_writes_and_caps_per_day()
    {
        var e = new Env();
        Assert.True(e.GuestSvc.Track(Ev(G1, "LUYENTAP", "g6.toan.taphop")));
        var row = Assert.Single(e.Uow.Guests.Rows);
        Assert.Equal(e.Clock.UtcNow, row.timecr);
        Assert.NotEqual(Guid.Empty, row.id);
        for (int i = 1; i < GuestRules.MAX_PER_DAY; i++) e.GuestSvc.Track(Ev(G1, "TRUYCAP"));
        Assert.False(e.GuestSvc.Track(Ev(G1, "TRUYCAP")));
        Assert.Equal(GuestRules.MAX_PER_DAY, e.Uow.Guests.Rows.Count);
        e.Clock.UtcNow = e.Clock.UtcNow.AddDays(1).AddMinutes(1);
        Assert.True(e.GuestSvc.Track(Ev(G1, "TRUYCAP")));
    }

    [Fact]
    public void Register_links_guest_activity_of_that_browser_only()
    {
        var e = new Env();
        e.GuestSvc.Track(Ev(G1, "LUYENTAP"));
        e.GuestSvc.Track(Ev(G1, "KIEMTRA", score: 8));
        e.GuestSvc.Track(Ev(G2, "LUYENTAP"));
        var r = e.Auth.Register(Reg("b@x.vn", G1));
        Assert.All(e.Uow.Guests.Rows.Where(x => x.guestid == G1), x => { Assert.Equal(r.User.Id, x.userid); Assert.Equal(GuestRules.VIA_REGISTER, x.linkvia); });
        Assert.Null(e.Uow.Guests.Rows.Single(x => x.guestid == G2).userid);
    }

    [Fact]
    public void Login_links_as_existing_user_and_never_relinks()
    {
        var e = new Env();
        var first = e.Auth.Register(Reg("c@x.vn", null));
        e.GuestSvc.Track(Ev(G2, "LUYENTAP"));
        e.Auth.Login(new LoginModel { Login = "c@x.vn", Password = "123456", GuestId = G2 });
        var row = e.Uow.Guests.Rows.Single(x => x.guestid == G2);
        Assert.Equal(first.User.Id, row.userid);
        Assert.Equal(GuestRules.VIA_LOGIN, row.linkvia);
        // mã khách đã gắn: đăng ký tài khoản khác cùng mã không gắn lại
        var other = e.Auth.Register(Reg("d@x.vn", G2));
        Assert.Equal(first.User.Id, e.Uow.Guests.Rows.Single(x => x.guestid == G2).userid);
    }

    [Fact]
    public void Link_errors_never_break_register()
    {
        var e = new Env();
        e.Uow.Guests.Fail = true;
        var r = e.Auth.Register(Reg("e@x.vn", G1));
        Assert.NotNull(r.AccessToken);
        Assert.Single(e.Users.Users);
        e.Auth.Register(Reg("f@x.vn", "không-phải-mã"));   // mã sai: bỏ qua
        Assert.Equal(2, e.Users.Users.Count);
    }

    [Fact]
    public void Stats_admin_only()
    {
        var e = new Env();
        var r = e.Auth.Register(Reg("g@x.vn", null));
        Assert.Equal(403, Assert.Throws<LogicException>(() => e.GuestSvc.GetAdminStats(e.Parent(r.User.Id), 30)).ErrorCode);
    }

    [Fact]
    public void Stats_counts_funnel_conversion_and_days()
    {
        var e = new Env();
        var day1 = e.Clock.UtcNow;                          // 08/10 07:00 giờ VN
        // G1: vào xem, làm 2 bài ngày 1, quay lại ngày 2 làm kiểm tra rồi đăng ký
        e.GuestSvc.Track(Ev(G1, "TRUYCAP"));
        e.GuestSvc.Track(Ev(G1, "LUYENTAP", "g6.toan.taphop"));
        e.Clock.UtcNow = day1.AddDays(1);
        e.GuestSvc.Track(Ev(G1, "KIEMTRA", "g6.toan.taphop", 8));
        e.GuestSvc.Track(Ev(G1, "KIEMTRA", "g6.toan.luythua", 9));
        var u = e.Auth.Register(Reg("h@x.vn", G1));
        // G2: chỉ vào xem, gặp giới hạn, lớp 2
        e.GuestSvc.Track(Ev(G2, "TRUYCAP", grade: 2));
        e.GuestSvc.Track(new GuestEventModel { GuestId = G2, Kind = "KHOA", Note = "guest", Grade = 2 });
        // G3: hoạt động từ 40 ngày trước -> ngoài kỳ 30 ngày
        e.Uow.Guests.Rows.Add(new GuestActivityModel { id = Guid.NewGuid(), guestid = G3, kind = "LUYENTAP", timecr = day1.AddDays(-40), topicid = "g2.toan.x" });

        var s = e.GuestSvc.GetAdminStats(e.AdminUser(Guid.NewGuid()), 30);
        Assert.Equal(30, s.Days);
        Assert.Equal(2, s.Guests);
        Assert.Equal(1, s.Learners);
        Assert.Equal(1, s.Returning);
        Assert.Equal(1, s.LockHits);
        Assert.Equal(1, s.Registered);
        Assert.Equal(0, s.ExistingUsers);
        Assert.Equal(50m, s.ConversionPct);
        Assert.Equal(100m, s.LearnerConversionPct);
        Assert.Equal(30, s.DayStats.Count);
        Assert.Equal("2026-10-09", s.DayStats[^1].Day);
        Assert.Equal(1, s.DayStats[^2].NewGuests);           // 08/10: G1 mới
        Assert.Equal(1, s.DayStats[^1].NewGuests);           // 09/10: G2 mới
        Assert.Equal(2, s.DayStats[^1].ActiveGuests);
        Assert.Equal(1, s.DayStats[^1].Registered);
        Assert.Equal(new[] { 1, 0, 1, 0 }, s.Funnel.Select(x => x.Guests));
        Assert.Equal(1, s.Funnel.Single(x => x.Code == "B2_4").Registered);
        Assert.Equal(new[] { "2", "6" }, s.Grades.Select(x => x.Key));
        Assert.Equal("g6.toan.taphop", s.TopTopics[0].TopicId);
        Assert.Equal(2, s.TopTopics[0].Activities);
        Assert.DoesNotContain(s.TopTopics, x => x.TopicId == "g2.toan.x");
        var row = s.Recent.Single(x => x.Status == GuestRules.VIA_REGISTER);
        Assert.Equal("aaaaaaaa", row.GuestId);
        Assert.Equal(8.5m, row.AvgScore);
        Assert.Equal(1, row.Practices);
        Assert.Equal(2, row.Tests);
        Assert.Equal(2, row.ActiveDays);
        Assert.Equal("Phụ huynh B", row.UserName);
        Assert.Equal("h@x.vn", row.UserContact);
        Assert.Equal(u.User.Id, e.Uow.Guests.Rows.First(x => x.guestid == G1).userid);
    }

    [Fact]
    public void Day_boundary_uses_vietnam_time()
    {
        // 16:59 UTC = 23:59 VN ngày 08/10; 17:01 UTC = 00:01 VN ngày 09/10
        var now = new DateTime(2026, 10, 8, 17, 1, 0, DateTimeKind.Utc);
        var rows = new List<GuestActivityModel>
        {
            new() { guestid = G1, kind = "TRUYCAP", timecr = now.AddMinutes(-2) },
            new() { guestid = G2, kind = "TRUYCAP", timecr = now },
        };
        var s = GuestStats.Build(rows, now, 2, _ => null);
        Assert.Equal(new[] { "2026-10-08", "2026-10-09" }, s.DayStats.Select(x => x.Day));
        Assert.Equal(new[] { 1, 1 }, s.DayStats.Select(x => x.NewGuests));
        Assert.Equal(1, GuestStats.Build(rows, now, 1, _ => null).Guests);   // kỳ 1 ngày: chỉ hôm nay (giờ VN)
    }

    [Fact]
    public void Stats_skip_admin_browsers()
    {
        var e = new Env();
        var admin = new AppUserModel { id = Guid.NewGuid(), email = "admin@x.vn", fullname = "Quản trị", role = HocTapConst.ROLE_ADMIN, passwordhash = "h:Admin@2026", status = HocTapConst.STATUS_ACTIVE };
        e.Users.Users.Add(admin);
        e.GuestSvc.Track(Ev(G1, "TRUYCAP"));
        e.GuestSvc.Track(Ev(G2, "TRUYCAP"));
        e.GuestSvc.LinkGuest(G1, admin.id, GuestRules.VIA_LOGIN);
        var s = e.GuestSvc.GetAdminStats(e.AdminUser(admin.id), 7);
        Assert.Equal(1, s.Guests);
        Assert.Equal("bbbbbbbb", Assert.Single(s.Recent).GuestId);
        Assert.Equal(0, s.ExistingUsers);
    }

    /* ===== 02/11/2026 - đo lường đúng: bước phễu, nguồn vào, loại máy ===== */
    [Fact]
    public void Step_kind_requires_known_step_code()
    {
        var m = GuestRules.ToModel(new GuestEventModel { GuestId = G1, Kind = "buoc", Note = "Answer" });
        Assert.Equal("BUOC", m.kind);
        Assert.Equal("answer", m.note);
        Assert.Throws<LogicException>(() => GuestRules.ToModel(new GuestEventModel { GuestId = G1, Kind = "BUOC", Note = "xoa" }));
        Assert.Throws<LogicException>(() => GuestRules.ToModel(new GuestEventModel { GuestId = G1, Kind = "BUOC" }));
        Assert.Equal("zalo_dt", GuestRules.ToModel(new GuestEventModel { GuestId = G1, Kind = "TRUYCAP", Note = "zalo_dt" }).note);
    }

    [Fact]
    public void Stats_funnel_steps_sources_devices()
    {
        var now = new DateTime(2026, 11, 2, 5, 0, 0, DateTimeKind.Utc);
        GuestActivityModel R(string g, string kind, string? note = null, int minutes = 0) => new() { guestid = g, kind = kind, note = note, timecr = now.AddMinutes(-minutes) };
        var rows = new List<GuestActivityModel>
        {
            R(G1, "TRUYCAP", "zalo_dt", 30), R(G1, "BUOC", "intro", 29), R(G1, "BUOC", "quick", 28), R(G1, "BUOC", "answer", 27), R(G1, "BUOC", "quickdone", 20),
            R(G2, "TRUYCAP", "google_mt", 10), R(G2, "BUOC", "intro", 9),
            R(G3, "TRUYCAP", null, 5),                                // khách cũ / chưa có thao tác: không tính vào nguồn
        };
        var s = GuestStats.Build(rows, now, 7, _ => null);
        Assert.Equal(3, s.Guests);
        Assert.Equal(2, s.Engaged);
        Assert.Equal(1, s.Starters);
        Assert.Equal(GuestRules.STEPS.Length, s.Steps.Count);
        Assert.Equal(2, s.Steps.Single(x => x.Code == "intro").Guests);
        Assert.Equal(1, s.Steps.Single(x => x.Code == "answer").Guests);
        Assert.Equal(0, s.Steps.Single(x => x.Code == "regclick").Guests);
        Assert.Equal(new[] { "google", "zalo" }, s.Sources.Select(x => x.Key).OrderBy(x => x));
        Assert.Equal(1, s.Sources.Single(x => x.Key == "zalo").Starters);
        Assert.Equal(0, s.Sources.Single(x => x.Key == "google").Starters);
        Assert.Equal(new[] { "dt", "mt" }, s.Devices.Select(x => x.Key).OrderBy(x => x));
    }
}
