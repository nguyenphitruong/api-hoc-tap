using System.Text.RegularExpressions;
using HocTap.Domain.Aggregates.Settings;

namespace HocTap.Domain.Commons;

/// <summary>
/// 14/10/2026 - Quy tắc cài đặt hệ thống (hàm thuần, kiểm thử được):
/// - Khoá tham số liên hệ hỗ trợ trong sys_setting
/// - Kiểm tra 1 gói gia hạn admin nhập (mã, tên, số tháng, số môn, giá)
/// - Chuẩn hoá số điện thoại (chỉ giữ chữ số, +84 -> 0)
/// </summary>
public static class SettingRules
{
    // Khoá trong bảng sys_setting
    public const string KEY_SUPPORT_NAME = "SUPPORT_NAME";
    public const string KEY_SUPPORT_PHONE = "SUPPORT_PHONE";
    public const string KEY_SUPPORT_ZALO = "SUPPORT_ZALO";     // "1" = số điện thoại dùng Zalo, "0" = không
    public const string KEY_SUPPORT_HOURS = "SUPPORT_HOURS";

    public const int PLAN_MAX_MONTHS = 36;
    public const decimal PLAN_MAX_PRICE = 100_000_000m;
    public const int TEXT_MAX = 100;

    private static readonly Regex CodeRegex = new("^[A-Z0-9_]{1,20}$", RegexOptions.Compiled);

    /// <summary>Mã gói: chữ in hoa / số / gạch dưới, tối đa 20 ký tự (vd 3T1)</summary>
    public static string NormalizeCode(string? i_code) => (i_code ?? "").Trim().ToUpperInvariant();

    /// <summary>Kiểm tra 1 gói; lỗi -> LogicException 400 kèm thông báo tiếng Việt</summary>
    public static void ValidatePlan(SavePlanPriceModel i_plan)
    {
        string code = NormalizeCode(i_plan.Code);
        if (!CodeRegex.IsMatch(code)) throw new LogicException($"Mã gói '{i_plan.Code}' không hợp lệ (chữ in hoa, số, gạch dưới; tối đa 20 ký tự).");
        string name = (i_plan.Name ?? "").Trim();
        if (name.Length == 0 || name.Length > TEXT_MAX) throw new LogicException($"Gói {code}: tên gói bắt buộc, tối đa {TEXT_MAX} ký tự.");
        if (i_plan.Months < 1 || i_plan.Months > PLAN_MAX_MONTHS) throw new LogicException($"Gói {code}: số tháng từ 1 đến {PLAN_MAX_MONTHS}.");
        if (i_plan.Subjects != 1 && i_plan.Subjects != 2) throw new LogicException($"Gói {code}: số môn là 1 hoặc 2.");
        if (i_plan.Price <= 0 || i_plan.Price > PLAN_MAX_PRICE || i_plan.Price != Math.Round(i_plan.Price)) throw new LogicException($"Gói {code}: giá phải là số nguyên dương (đồng).");
        if ((i_plan.Note ?? "").Trim().Length > TEXT_MAX) throw new LogicException($"Gói {code}: ghi chú tối đa {TEXT_MAX} ký tự.");
    }

    /// <summary>Chỉ giữ chữ số; +84 / 84xxxxxxxxx -> 0xxxxxxxxx</summary>
    public static string NormalizePhone(string? i_phone)
    {
        string d = new string((i_phone ?? "").Where(char.IsDigit).ToArray());
        if (d.StartsWith("84") && d.Length == 11) d = "0" + d[2..];
        return d;
    }

    /// <summary>Số điện thoại hỗ trợ: trống (ẩn liên hệ) hoặc 8–12 chữ số</summary>
    public static void ValidateSupport(SaveSupportModel i_model)
    {
        string phone = NormalizePhone(i_model.Phone);
        if (phone.Length > 0 && (phone.Length < 8 || phone.Length > 12)) throw new LogicException("Số điện thoại hỗ trợ không hợp lệ (8–12 chữ số).");
        if ((i_model.Name ?? "").Trim().Length > TEXT_MAX) throw new LogicException($"Tên người hỗ trợ tối đa {TEXT_MAX} ký tự.");
        if ((i_model.Hours ?? "").Trim().Length > TEXT_MAX) throw new LogicException($"Giờ hỗ trợ tối đa {TEXT_MAX} ký tự.");
    }
}
