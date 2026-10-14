using HocTap.Application.Commons;
using HocTap.Application.Payments;
using HocTap.Domain.Aggregates.Settings;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;

namespace HocTap.Application.Settings;

/// <summary>14/10/2026 - Service cài đặt hệ thống (chỉ ADMIN): bảng giá gói + liên hệ hỗ trợ</summary>
public interface ISettingServices
{
    AdminSettingsReadModel GetAdminSettings(CurrentUser i_caller);
    /// <summary>Lưu danh sách gói (thêm / sửa theo mã; gói không có trong danh sách giữ nguyên)</summary>
    AdminSettingsReadModel SavePlans(CurrentUser i_caller, List<SavePlanPriceModel> i_plans);
    AdminSettingsReadModel SaveSupport(CurrentUser i_caller, SaveSupportModel i_model);
}

/// <summary>
/// 14/10/2026 - CÀI ĐẶT HỆ THỐNG (viết tường minh theo mẫu Web-MultiClinic) — chỉ tài khoản ADMIN
/// - Bảng giá (plan_price): thêm / sửa gói theo mã; không xoá (đơn cũ tham chiếu plancode) — ngưng bán bằng IsActive = false.
///   Bảng còn trống: màn hình hiện giá appsettings (PlansFromConfig) để admin lưu lần đầu vào CSDL.
///   Đổi giá chỉ áp dụng đơn tạo SAU khi lưu (đơn đã tạo giữ số tiền lúc tạo).
/// - Liên hệ hỗ trợ (sys_setting): SUPPORT_NAME / SUPPORT_PHONE / SUPPORT_ZALO / SUPPORT_HOURS; số điện thoại trống = ẩn trên web.
/// - Lưu xong: SettingCache.Invalidate() -> /payments/config trả dữ liệu mới ngay.
/// Mỗi thao tác ghi: InitTransaction -> Upsert... -> Save (1 lần) -> CommitTransaction; lỗi -> RollbackTransaction.
/// </summary>
public class SettingServices : ISettingServices
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly PaymentOptions options;
    private readonly SettingCache? settingCache;

    public SettingServices(IUnitOfWork i_UnitOfWork, IClock i_Clock, PaymentOptions i_Options, SettingCache? i_SettingCache = null)
    {
        unitOfWork = i_UnitOfWork;
        clock = i_Clock;
        options = i_Options;
        settingCache = i_SettingCache;
    }

    /// <summary>Đọc thẳng CSDL (không qua bộ nhớ đệm) để admin luôn thấy dữ liệu mới nhất</summary>
    public AdminSettingsReadModel GetAdminSettings(CurrentUser i_caller)
    {
        ServiceGuard.EnsureAdmin(i_caller);
        SettingSnapshot settings = SettingReader.Load(unitOfWork, options);
        if (settings.LoadFailed) throw new LogicException("Chưa có bảng cài đặt trong CSDL — hãy chạy script sql-dump/20261014_hoctap_phase9.sql.", 500);

        AdminSettingsReadModel _Result = new AdminSettingsReadModel();
        _Result.PlansFromConfig = settings.PlansFromConfig;
        _Result.Support = settings.Support();
        foreach (PlanPriceModel p in settings.Plans)
        {
            _Result.Plans.Add(new AdminPlanPriceReadModel
            {
                Code = p.code, Name = p.name, Months = p.months, Subjects = p.subjects, Price = p.price, Note = p.note,
                SortOrder = p.sortorder, IsActive = p.isactive, TimeUp = p.timeup ?? (settings.PlansFromConfig ? null : p.timecr),
            });
        }
        return _Result;
    }

    /// <summary>
    /// Kiểm tra: mỗi gói hợp lệ (SettingRules.ValidatePlan), không trùng mã trong danh sách,
    /// sau khi lưu phải còn ≥ 1 gói đang bán (không thì phụ huynh không gia hạn được).
    /// </summary>
    public AdminSettingsReadModel SavePlans(CurrentUser i_caller, List<SavePlanPriceModel> i_plans)
    {
        // 1. Kiểm tra
        ServiceGuard.EnsureAdmin(i_caller);
        List<SavePlanPriceModel> lstInput = i_plans ?? new List<SavePlanPriceModel>();
        if (lstInput.Count == 0) throw new LogicException("Chưa có gói nào để lưu.");
        HashSet<string> codes = new HashSet<string>();
        foreach (SavePlanPriceModel p in lstInput)
        {
            SettingRules.ValidatePlan(p);
            if (!codes.Add(SettingRules.NormalizeCode(p.Code))) throw new LogicException($"Mã gói {SettingRules.NormalizeCode(p.Code)} bị trùng.");
        }
        List<PlanPriceModel> lstExisting = unitOfWork.SettingRepo.GetListPlanPrice();
        bool anyActive = lstInput.Any(x => x.IsActive)
            || lstExisting.Any(x => x.isactive && !codes.Contains(x.code));
        if (!anyActive) throw new LogicException("Phải còn ít nhất 1 gói đang bán.");

        // 2. Ghi
        DateTime now = clock.UtcNow;
        try
        {
            unitOfWork.InitTransaction();
            foreach (SavePlanPriceModel p in lstInput)
            {
                string code = SettingRules.NormalizeCode(p.Code);
                PlanPriceModel? old = lstExisting.FirstOrDefault(x => x.code == code);
                PlanPriceModel plan = new PlanPriceModel();
                plan.code = code;
                plan.name = p.Name!.Trim();
                plan.months = p.Months;
                plan.subjects = p.Subjects;
                plan.price = p.Price;
                plan.note = string.IsNullOrWhiteSpace(p.Note) ? null : p.Note.Trim();
                plan.sortorder = p.SortOrder;
                plan.isactive = p.IsActive;
                plan.timecr = old?.timecr ?? now;
                plan.timeup = now;
                plan.userup = i_caller.UserId;
                unitOfWork.SettingRepo.UpsertPlanPrice(plan);
            }
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch
        {
            unitOfWork.RollbackTransaction();
            throw;
        }

        // 3. Xoá bộ nhớ đệm -> trang giới thiệu / hộp thoại gia hạn thấy giá mới ngay
        settingCache?.Invalidate();
        return GetAdminSettings(i_caller);
    }

    public AdminSettingsReadModel SaveSupport(CurrentUser i_caller, SaveSupportModel i_model)
    {
        // 1. Kiểm tra
        ServiceGuard.EnsureAdmin(i_caller);
        SaveSupportModel model = i_model ?? new SaveSupportModel();
        SettingRules.ValidateSupport(model);

        // 2. Ghi 4 tham số
        DateTime now = clock.UtcNow;
        try
        {
            unitOfWork.InitTransaction();
            UpsertValue(SettingRules.KEY_SUPPORT_NAME, (model.Name ?? "").Trim(), "Tên người hỗ trợ", now, i_caller.UserId);
            UpsertValue(SettingRules.KEY_SUPPORT_PHONE, SettingRules.NormalizePhone(model.Phone), "Số điện thoại hỗ trợ (trống = ẩn liên hệ)", now, i_caller.UserId);
            UpsertValue(SettingRules.KEY_SUPPORT_ZALO, model.Zalo ? "1" : "0", "1 = số điện thoại dùng Zalo", now, i_caller.UserId);
            UpsertValue(SettingRules.KEY_SUPPORT_HOURS, (model.Hours ?? "").Trim(), "Giờ hỗ trợ (tuỳ chọn)", now, i_caller.UserId);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch
        {
            unitOfWork.RollbackTransaction();
            throw;
        }

        // 3. Xoá bộ nhớ đệm
        settingCache?.Invalidate();
        return GetAdminSettings(i_caller);
    }

    private void UpsertValue(string i_key, string i_value, string i_note, DateTime i_now, Guid i_userId)
    {
        SysSettingModel setting = new SysSettingModel();
        setting.key = i_key;
        setting.value = i_value;
        setting.note = i_note;
        setting.timeup = i_now;
        setting.userup = i_userId;
        unitOfWork.SettingRepo.UpsertSetting(setting);
    }
}
