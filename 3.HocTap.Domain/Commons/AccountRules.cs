namespace HocTap.Domain.Commons;

/// <summary>
/// 08/10/2026 - Quy tắc trạng thái tài khoản (hàm thuần, có xUnit):
/// - KHOA giữ nguyên; DUNGTHU còn hạn khi trialend >= now; HOATDONG còn hạn khi planend >= now; còn lại là HETHAN.
/// - Được làm bài mới khi trạng thái hiệu lực là DUNGTHU hoặc HOATDONG.
/// </summary>
public static class AccountRules
{
    public static string EffectiveStatus(string status, DateTime trialEnd, DateTime? planEnd, DateTime nowUtc)
    {
        if (status == HocTapConst.STATUS_LOCKED) return HocTapConst.STATUS_LOCKED;
        if (status == HocTapConst.STATUS_ACTIVE && planEnd.HasValue && planEnd.Value >= nowUtc) return HocTapConst.STATUS_ACTIVE;
        if (status == HocTapConst.STATUS_TRIAL && trialEnd >= nowUtc) return HocTapConst.STATUS_TRIAL;
        return HocTapConst.STATUS_EXPIRED;
    }

    public static bool CanStudy(string effectiveStatus) =>
        effectiveStatus == HocTapConst.STATUS_TRIAL || effectiveStatus == HocTapConst.STATUS_ACTIVE;

    /// <summary>Chuẩn hoá đăng nhập: có '@' là email (chữ thường), còn lại là số điện thoại (bỏ ký tự thừa, +84 -> 0)</summary>
    public static (string? Email, string? Phone) NormalizeLogin(string? login)
    {
        var s = (login ?? "").Trim();
        if (s.Length == 0) return (null, null);
        if (s.Contains('@')) return (s.ToLowerInvariant(), null);
        return (null, NormalizePhone(s));
    }

    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (phone.Trim().StartsWith("+84") || (digits.StartsWith("84") && digits.Length == 11)) digits = "0" + digits[2..];
        return digits;
    }
}
