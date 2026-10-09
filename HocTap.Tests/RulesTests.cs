using HocTap.Domain.Commons;

namespace HocTap.Tests;

/// <summary>08/10/2026 - Test quy tắc thuần: trạng thái tài khoản, chuẩn hoá đăng nhập, mã chủ đề</summary>
public class RulesTests
{
    private static readonly DateTime NOW = new(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("DUNGTHU", 1, null, "DUNGTHU")]
    [InlineData("DUNGTHU", -1, null, "HETHAN")]
    [InlineData("HOATDONG", -30, 10, "HOATDONG")]
    [InlineData("HOATDONG", -30, -1, "HETHAN")]
    [InlineData("HOATDONG", 5, null, "HETHAN")]
    [InlineData("KHOA", 5, 100, "KHOA")]
    public void EffectiveStatus(string status, int trialDays, int? planDays, string expected)
    {
        var plan = planDays.HasValue ? NOW.AddDays(planDays.Value) : (DateTime?)null;
        Assert.Equal(expected, AccountRules.EffectiveStatus(status, NOW.AddDays(trialDays), plan, NOW));
    }

    [Fact]
    public void CanStudy_OnlyTrialOrActive()
    {
        Assert.True(AccountRules.CanStudy("DUNGTHU"));
        Assert.True(AccountRules.CanStudy("HOATDONG"));
        Assert.False(AccountRules.CanStudy("HETHAN"));
        Assert.False(AccountRules.CanStudy("KHOA"));
    }

    [Theory]
    [InlineData(" A@X.vn ", "a@x.vn", null)]
    [InlineData("0912 345 678", null, "0912345678")]
    [InlineData("+84912345678", null, "0912345678")]
    [InlineData("84912345678", null, "0912345678")]
    public void NormalizeLogin(string login, string? email, string? phone)
    {
        var r = AccountRules.NormalizeLogin(login);
        Assert.Equal(email, r.Email);
        Assert.Equal(phone, r.Phone);
    }

    [Theory]
    [InlineData("g6.toan.taphop", 6, "toan", "topic")]
    [InlineData("ch:g2.anh.u3", 2, "anh", "ch")]
    [InlineData("hk:g6.toan.1", 6, "toan", "hk")]
    public void ParseTopic_Valid(string code, short grade, string subj, string kind)
    {
        var p = AttemptRules.ParseTopic(code);
        Assert.NotNull(p);
        Assert.Equal(grade, p!.Value.Grade);
        Assert.Equal(subj, p.Value.Subject);
        Assert.Equal(kind, p.Value.Kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("g3.toan.x")]
    [InlineData("g6.van.x")]
    [InlineData("xx:g6.toan.x")]
    [InlineData("g6.toan.<script>")]
    public void ParseTopic_Invalid(string code) => Assert.Null(AttemptRules.ParseTopic(code));

    [Fact]
    public void UnixMs_RoundTrip()
    {
        const long ms = 1791427506878;
        Assert.Equal(ms, AttemptRules.ToUnixMs(AttemptRules.FromUnixMs(ms)));
    }
}
