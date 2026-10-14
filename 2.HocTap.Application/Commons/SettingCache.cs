using HocTap.Application.Payments;
using HocTap.Domain.Aggregates.Payments;
using HocTap.Domain.Aggregates.Settings;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;

namespace HocTap.Application.Commons;

/// <summary>
/// 14/10/2026 - Ảnh chụp cài đặt hệ thống đã đọc từ CSDL (bảng giá + tham số), dùng chung cho mọi request trong thời gian TTL.
/// </summary>
public class SettingSnapshot
{
    /// <summary>Mọi gói (kể cả đã ngưng bán) — tra tên gói cho đơn cũ</summary>
    public List<PlanPriceModel> Plans { get; set; } = new();
    /// <summary>true = plan_price trống / chưa tạo bảng -> giá lấy tạm từ appsettings Payment:Plans</summary>
    public bool PlansFromConfig { get; set; }
    /// <summary>Tham số sys_setting (khoá không phân biệt hoa thường)</summary>
    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Đọc CSDL lỗi (vd chưa chạy script phase9) -> chỉ giữ trong bộ nhớ đệm ngắn rồi đọc lại</summary>
    public bool LoadFailed { get; set; }
    public DateTime LoadedAt { get; set; }

    /// <summary>Gói đang bán (isactive, đủ tháng + giá) theo thứ tự hiển thị — trang giới thiệu, hộp thoại gia hạn, tạo đơn</summary>
    public List<PlanPackageReadModel> ActivePlans() =>
        Plans.Where(x => x.isactive && x.months > 0 && x.price > 0).Select(SettingReader.ToPackage).ToList();

    public string Get(string i_key) => Values.TryGetValue(i_key, out string? v) ? v : "";

    /// <summary>Liên hệ hỗ trợ (công khai). Số điện thoại trống -> web ẩn khối liên hệ</summary>
    public SupportContactReadModel Support()
    {
        SupportContactReadModel _Result = new SupportContactReadModel();
        _Result.Name = Get(SettingRules.KEY_SUPPORT_NAME);
        _Result.Phone = SettingRules.NormalizePhone(Get(SettingRules.KEY_SUPPORT_PHONE));
        _Result.Zalo = _Result.Phone.Length > 0 && Get(SettingRules.KEY_SUPPORT_ZALO) != "0";
        _Result.Hours = Get(SettingRules.KEY_SUPPORT_HOURS);
        return _Result;
    }
}

/// <summary>
/// 14/10/2026 - BỘ NHỚ ĐỆM CÀI ĐẶT (singleton): trang giới thiệu / hộp thoại gia hạn gọi /payments/config liên tục,
/// không đọc CSDL mỗi lần. Hết hạn sau TTL (5 phút; đọc lỗi: 30 giây); admin lưu cài đặt -> Invalidate() có hiệu lực ngay.
/// Thread-safe bằng lock (dữ liệu nhỏ, đọc lại hiếm).
/// </summary>
public class SettingCache
{
    public static readonly TimeSpan TTL = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan TTL_FAILED = TimeSpan.FromSeconds(30);

    private readonly object _lock = new();
    private SettingSnapshot? _snapshot;

    public SettingSnapshot Get(DateTime i_now, Func<SettingSnapshot> i_loader)
    {
        lock (_lock)
        {
            if (_snapshot != null)
            {
                TimeSpan ttl = _snapshot.LoadFailed ? TTL_FAILED : TTL;
                if (i_now - _snapshot.LoadedAt < ttl) return _snapshot;
            }
            _snapshot = i_loader();
            _snapshot.LoadedAt = i_now;
            return _snapshot;
        }
    }

    public void Invalidate()
    {
        lock (_lock) { _snapshot = null; }
    }
}

/// <summary>
/// 14/10/2026 - Đọc cài đặt từ CSDL (qua unitOfWork.SettingRepo), dự phòng bằng appsettings:
///   - plan_price trống            -> bảng giá Payment:Plans (PlansFromConfig = true)
///   - lỗi CSDL (chưa tạo bảng ...) -> bảng giá Payment:Plans, không có liên hệ (LoadFailed = true)
/// => triển khai code mới trước khi chạy script phase9 vẫn bán được gói như cũ.
/// </summary>
public static class SettingReader
{
    public static SettingSnapshot Get(IUnitOfWork i_uow, PaymentOptions i_options, SettingCache? i_cache, DateTime i_now) =>
        i_cache == null ? Load(i_uow, i_options) : i_cache.Get(i_now, () => Load(i_uow, i_options));

    public static SettingSnapshot Load(IUnitOfWork i_uow, PaymentOptions i_options)
    {
        SettingSnapshot _Result = new SettingSnapshot();
        try
        {
            _Result.Plans = i_uow.SettingRepo.GetListPlanPrice();
            foreach (SysSettingModel s in i_uow.SettingRepo.GetListSetting()) _Result.Values[s.key] = s.value ?? "";
        }
        catch (Exception)
        {
            _Result.LoadFailed = true;
            _Result.Plans = new List<PlanPriceModel>();
            _Result.Values.Clear();
        }
        if (_Result.Plans.Count == 0)
        {
            _Result.Plans = FromOptions(i_options);
            _Result.PlansFromConfig = true;
        }
        return _Result;
    }

    /// <summary>Payment:Plans (appsettings) -> dòng plan_price (thứ tự theo vị trí trong cấu hình)</summary>
    public static List<PlanPriceModel> FromOptions(PaymentOptions i_options)
    {
        List<PlanPriceModel> _Result = new List<PlanPriceModel>();
        int order = 0;
        foreach (PlanPackageReadModel p in i_options.Plans)
        {
            order += 10;
            _Result.Add(new PlanPriceModel
            {
                code = SettingRules.NormalizeCode(p.Code), name = p.Name, months = p.Months, subjects = p.Subjects,
                price = p.Price, note = p.Note, sortorder = order, isactive = true,
            });
        }
        return _Result;
    }

    public static PlanPackageReadModel ToPackage(PlanPriceModel i_plan) => new PlanPackageReadModel
    {
        Code = i_plan.code, Name = i_plan.name, Months = i_plan.months, Subjects = i_plan.subjects, Price = i_plan.price,
        Note = string.IsNullOrWhiteSpace(i_plan.note) ? null : i_plan.note,
    };
}
