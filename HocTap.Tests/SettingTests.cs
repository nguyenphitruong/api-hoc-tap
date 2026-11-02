using HocTap.Application.Commons;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Payments;
using HocTap.Domain.Aggregates.Settings;
using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Commons;

namespace HocTap.Tests;

/// <summary>14/10/2026 - Cài đặt hệ thống: bảng giá trong CSDL (plan_price) + liên hệ hỗ trợ (sys_setting), bộ nhớ đệm, dự phòng appsettings</summary>
public class SettingTests
{
    private static Guid NewParent(Env e) =>
        e.Auth.Register(new RegisterModel { FullName = "Phụ huynh A", Email = "a@x.vn", Password = "123456", AcceptTerms = true }).User.Id;

    private static Guid NewStudent(Env e, Guid uid) =>
        e.StudentSvc.InsertStudent(e.Parent(uid), new StudentSaveModel { NickName = "Minh", Grade = 6 }).Id;

    private static SavePlanPriceModel Plan(string code, int months, int subjects, decimal price, bool active = true, int order = 10) =>
        new SavePlanPriceModel { Code = code, Name = $"{subjects} môn · {months} tháng", Months = months, Subjects = subjects, Price = price, SortOrder = order, IsActive = active };

    [Fact]
    public void Only_admin()
    {
        var e = new Env();
        var uid = NewParent(e);
        Assert.Equal(403, Assert.Throws<LogicException>(() => e.SettingSvc.GetAdminSettings(e.Parent(uid))).ErrorCode);
        Assert.Equal(403, Assert.Throws<LogicException>(() => e.SettingSvc.SaveSupport(e.Parent(uid), new SaveSupportModel { Phone = "0909123456" })).ErrorCode);
    }

    [Fact]
    public void Empty_table_falls_back_to_appsettings()
    {
        var e = new Env();
        var cfg = e.PaySvc.GetConfig();
        Assert.Equal(new[] { "1T1", "3T2" }, cfg.Plans.Select(p => p.Code));
        Assert.Equal("", cfg.Support.Phone);
        var admin = e.SettingSvc.GetAdminSettings(e.AdminUser(Guid.NewGuid()));
        Assert.True(admin.PlansFromConfig);
        Assert.Equal(2, admin.Plans.Count);
    }

    [Fact]
    public void Db_error_falls_back_but_admin_is_told()
    {
        var e = new Env();
        e.Uow.Settings.Fail = true;
        Assert.Equal(2, e.PaySvc.GetConfig().Plans.Count);   // vẫn bán được gói khi chưa chạy script phase9
        Assert.Contains("phase9", Assert.Throws<LogicException>(() => e.SettingSvc.GetAdminSettings(e.AdminUser(Guid.NewGuid()))).Message);
    }

    [Fact]
    public void Saved_prices_apply_immediately_to_new_orders_only()
    {
        var e = new Env();
        var uid = NewParent(e);
        var sid = NewStudent(e, uid);
        var old = e.PaySvc.CreateOrder(e.Parent(uid), new CreatePaymentOrderModel { PlanCode = "3T2", StudentId = sid, Subjects = new() { "toan", "anh" } });
        Assert.Equal(159000, old.Amount);

        // lưu lần đầu: tăng giá 3T2, ngưng bán 1T1, thêm gói mới 6t2 (mã tự viết hoa)
        var res = e.SettingSvc.SavePlans(e.AdminUser(Guid.NewGuid()), new()
        {
            Plan("1T1", 1, 1, 39000, active: false, order: 10),
            Plan("3T2", 3, 2, 169000, order: 20),
            Plan("6t2", 6, 2, 299000, order: 30),
        });
        Assert.False(res.PlansFromConfig);
        Assert.Equal(new[] { "1T1", "3T2", "6T2" }, res.Plans.Select(p => p.Code));

        var cfg = e.PaySvc.GetConfig();   // bộ nhớ đệm đã xoá khi lưu
        Assert.Equal(new[] { "3T2", "6T2" }, cfg.Plans.Select(p => p.Code));
        Assert.Equal(169000, cfg.Plans[0].Price);

        // gói ngưng bán không tạo đơn được; đơn cũ giữ số tiền + tên gói
        Assert.Throws<LogicException>(() => e.PaySvc.CreateOrder(e.Parent(uid), new CreatePaymentOrderModel { PlanCode = "1T1", StudentId = sid, Subjects = new() { "toan" } }));
        Assert.Equal(159000, e.PaySvc.GetOrder(e.Parent(uid), old.Id).Amount);
        var o6 = e.PaySvc.CreateOrder(e.Parent(uid), new CreatePaymentOrderModel { PlanCode = "6T2", StudentId = sid, Subjects = new() { "toan", "anh" } });
        Assert.Equal(299000, o6.Amount);
        Assert.Equal("2 môn · 6 tháng", o6.PlanName);
    }

    [Theory]
    [InlineData("3 T", 1, 1, 1000)]        // mã có khoảng trắng
    [InlineData("X", 0, 1, 1000)]          // 0 tháng
    [InlineData("X", 1, 3, 1000)]          // 3 môn
    [InlineData("X", 1, 1, 0)]             // giá 0
    [InlineData("X", 1, 1, 1000.5)]        // giá lẻ
    public void Invalid_plan_rejected(string code, int months, int subjects, decimal price)
    {
        var e = new Env();
        Assert.Throws<LogicException>(() => e.SettingSvc.SavePlans(e.AdminUser(Guid.NewGuid()), new() { Plan(code, months, subjects, price) }));
        Assert.Empty(e.Uow.Settings.Plans);
    }

    [Fact]
    public void Duplicate_or_no_active_plan_rejected()
    {
        var e = new Env();
        var admin = e.AdminUser(Guid.NewGuid());
        Assert.Contains("trùng", Assert.Throws<LogicException>(() => e.SettingSvc.SavePlans(admin, new() { Plan("A", 1, 1, 1000), Plan("a", 2, 1, 2000) })).Message);
        Assert.Contains("ít nhất 1 gói", Assert.Throws<LogicException>(() => e.SettingSvc.SavePlans(admin, new() { Plan("A", 1, 1, 1000, active: false) })).Message);
    }

    [Fact]
    public void Support_contact_saved_and_public()
    {
        var e = new Env();
        var admin = e.AdminUser(Guid.NewGuid());
        Assert.Throws<LogicException>(() => e.SettingSvc.SaveSupport(admin, new SaveSupportModel { Phone = "12345" }));
        var res = e.SettingSvc.SaveSupport(admin, new SaveSupportModel { Name = " Cô Lan ", Phone = "+84 909.123.456", Zalo = true, Hours = "8:00 – 21:00" });
        Assert.Equal("0909123456", res.Support.Phone);
        var s = e.PaySvc.GetConfig().Support;
        Assert.Equal("Cô Lan", s.Name);
        Assert.Equal("0909123456", s.Phone);
        Assert.True(s.Zalo);
        Assert.Equal("8:00 – 21:00", s.Hours);
        // xoá số -> ẩn liên hệ, Zalo tắt theo
        e.SettingSvc.SaveSupport(admin, new SaveSupportModel { Phone = "", Zalo = true });
        Assert.False(e.PaySvc.GetConfig().Support.Zalo);
    }

    [Fact]
    public void Config_is_cached_for_ttl()
    {
        var e = new Env();
        e.PaySvc.GetConfig();
        e.PaySvc.GetConfig();
        Assert.Equal(1, e.Uow.Settings.Reads);
        e.Clock.UtcNow = e.Clock.UtcNow.Add(SettingCache.TTL);
        e.PaySvc.GetConfig();
        Assert.Equal(2, e.Uow.Settings.Reads);
    }

    [Fact]
    public void Phone_rules()
    {
        Assert.Equal("0909123456", SettingRules.NormalizePhone("0909 123 456"));
        Assert.Equal("0909123456", SettingRules.NormalizePhone("+84909123456"));
        Assert.Equal("", SettingRules.NormalizePhone(null));
    }
}
