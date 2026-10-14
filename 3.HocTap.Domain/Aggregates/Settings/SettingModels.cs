using HocTap.Domain.Aggregates.Payments;

namespace HocTap.Domain.Aggregates.Settings;

/// <summary>
/// 14/10/2026 - Model bảng plan_price: 1 gói gia hạn (thay cấu hình Payment:Plans trong appsettings).
/// code là khoá (đơn payment_order lưu plancode) nên không đổi / không xoá — ngưng bán thì isactive = false.
/// </summary>
public class PlanPriceModel
{
    public string code { get; set; } = "";
    public string name { get; set; } = "";
    public int months { get; set; }
    public int subjects { get; set; } = 2;              // 1 (chọn Toán hoặc Anh) | 2 (cả 2 môn)
    public decimal price { get; set; }
    public string? note { get; set; }
    public int sortorder { get; set; }
    public bool isactive { get; set; } = true;
    public DateTime timecr { get; set; }
    public DateTime? timeup { get; set; }
    public Guid? userup { get; set; }                   // admin sửa gần nhất
}

/// <summary>14/10/2026 - Model bảng sys_setting: 1 tham số hệ thống dạng khoá – giá trị (vd SUPPORT_PHONE)</summary>
public class SysSettingModel
{
    public string key { get; set; } = "";
    public string value { get; set; } = "";
    public string? note { get; set; }
    public DateTime? timeup { get; set; }
    public Guid? userup { get; set; }
}

/// <summary>14/10/2026 - Thông tin người hỗ trợ (công khai: trang giới thiệu, hộp chat, hộp thoại gia hạn)</summary>
public class SupportContactReadModel
{
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";             // trống = web ẩn khối liên hệ
    public bool Zalo { get; set; }
    public string Hours { get; set; } = "";
}

/// <summary>14/10/2026 - 1 gói trong màn hình quản trị (kể cả gói đã ngưng bán)</summary>
public class AdminPlanPriceReadModel : PlanPackageReadModel
{
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime? TimeUp { get; set; }
}

/// <summary>14/10/2026 - Màn hình quản trị "Cài đặt": bảng giá + liên hệ hỗ trợ</summary>
public class AdminSettingsReadModel
{
    public List<AdminPlanPriceReadModel> Plans { get; set; } = new();
    public SupportContactReadModel Support { get; set; } = new();
    /// <summary>true = bảng plan_price đang trống, giá đang lấy tạm từ appsettings (lưu lần đầu sẽ ghi vào CSDL)</summary>
    public bool PlansFromConfig { get; set; }
}

/// <summary>14/10/2026 - Admin lưu 1 gói (thêm mới khi code chưa có)</summary>
public class SavePlanPriceModel
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int Months { get; set; }
    public int Subjects { get; set; } = 2;
    public decimal Price { get; set; }
    public string? Note { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>14/10/2026 - Admin lưu thông tin liên hệ hỗ trợ</summary>
public class SaveSupportModel
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public bool Zalo { get; set; } = true;
    public string? Hours { get; set; }
}
