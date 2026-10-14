namespace HocTap.Domain.Aggregates.Settings;

/// <summary>14/10/2026 - Repository cài đặt hệ thống: bảng giá (plan_price) + tham số khoá – giá trị (sys_setting)</summary>
public interface ISettingRepository
{
    /// <summary>Mọi gói (kể cả đã ngưng bán), theo sortorder rồi code</summary>
    List<PlanPriceModel> GetListPlanPrice();
    /// <summary>Thêm mới khi code chưa có, ngược lại cập nhật; Service gọi unitOfWork.Save()</summary>
    void UpsertPlanPrice(PlanPriceModel i_plan);
    /// <summary>Mọi tham số hệ thống</summary>
    List<SysSettingModel> GetListSetting();
    /// <summary>Thêm mới khi key chưa có, ngược lại cập nhật; Service gọi unitOfWork.Save()</summary>
    void UpsertSetting(SysSettingModel i_setting);
}
