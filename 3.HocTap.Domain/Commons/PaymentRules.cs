using System.Text.RegularExpressions;

namespace HocTap.Domain.Commons;

/// <summary>
/// 12/10/2026 - Quy tắc thanh toán gia hạn (hàm thuần, có xUnit):
/// - Mã đơn: HT + 8 ký tự (chữ hoa + số, bỏ ký tự dễ nhầm O/0/I/1) — chính là nội dung chuyển khoản.
/// - Tìm mã đơn trong nội dung giao dịch ngân hàng: ngân hàng có thể thêm tiền tố / số tham chiếu, bỏ dấu cách, viết thường.
/// - Hạn gói mới: gói đang còn hạn thì cộng nối tiếp từ planend, ngược lại tính từ bây giờ (giống admin kích hoạt).
/// - Trạng thái hiệu lực đơn: chờ thanh toán quá PAY_ORDER_HOURS giờ -> HETHAN (vẫn nhận nếu sau đó tiền về đúng mã).
/// - Ảnh QR: VietQR Quick Link (img.vietqr.io) — app ngân hàng quét được, điền sẵn số tiền + nội dung.
/// </summary>
public static class PaymentRules
{
    private const string CODE_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private static readonly Regex CODE_RE = new(HocTapConst.PAY_CODE_PREFIX + "[" + CODE_CHARS + "]{" + HocTapConst.PAY_CODE_LENGTH + "}", RegexOptions.Compiled);

    public static string NewCode(Random? i_random = null)
    {
        Random random = i_random ?? Random.Shared;
        char[] chars = new char[HocTapConst.PAY_CODE_LENGTH];
        for (int i = 0; i < chars.Length; i++) chars[i] = CODE_CHARS[random.Next(CODE_CHARS.Length)];
        return HocTapConst.PAY_CODE_PREFIX + new string(chars);
    }

    /// <summary>Các mã đơn có trong nội dung chuyển khoản (đã bỏ dấu cách / ký tự lạ, viết hoa)</summary>
    public static List<string> FindCodes(string? i_content)
    {
        List<string> result = new List<string>();
        if (string.IsNullOrWhiteSpace(i_content)) return result;
        string upper = i_content.ToUpperInvariant();
        string compact = new string(upper.Where(char.IsLetterOrDigit).ToArray());
        foreach (string source in new[] { upper, compact })
        {
            foreach (Match m in CODE_RE.Matches(source))
            {
                if (!result.Contains(m.Value)) result.Add(m.Value);
            }
        }
        return result;
    }

    /// <summary>Gói còn hạn (HOATDONG, planend > now) -> cộng từ planend; còn lại cộng từ bây giờ</summary>
    public static DateTime PlanFrom(string i_status, DateTime? i_planEnd, DateTime i_nowUtc)
    {
        return i_status == HocTapConst.STATUS_ACTIVE && i_planEnd.HasValue && i_planEnd.Value > i_nowUtc ? i_planEnd.Value : i_nowUtc;
    }

    public static string EffectiveOrderStatus(string i_status, DateTime i_timeCr, DateTime i_nowUtc)
    {
        if (i_status == HocTapConst.PAY_PENDING && i_timeCr.AddHours(HocTapConst.PAY_ORDER_HOURS) < i_nowUtc) return HocTapConst.PAY_EXPIRED;
        return i_status;
    }

    /// <summary>Ảnh QR VietQR: https://img.vietqr.io/image/{bin}-{stk}-compact2.png?amount=&addInfo=&accountName=</summary>
    public static string VietQrUrl(string i_bankBin, string i_accountNo, decimal i_amount, string i_content, string i_accountName)
    {
        if (string.IsNullOrWhiteSpace(i_bankBin) || string.IsNullOrWhiteSpace(i_accountNo)) return "";
        return $"https://img.vietqr.io/image/{Uri.EscapeDataString(i_bankBin.Trim())}-{Uri.EscapeDataString(i_accountNo.Trim())}-compact2.png"
             + $"?amount={decimal.ToInt64(decimal.Round(i_amount))}"
             + $"&addInfo={Uri.EscapeDataString(i_content)}"
             + $"&accountName={Uri.EscapeDataString(i_accountName ?? "")}";
    }
}
