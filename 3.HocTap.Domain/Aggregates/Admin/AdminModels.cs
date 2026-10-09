namespace HocTap.Domain.Aggregates.Admin;

/// <summary>08/10/2026 - Lọc danh sách tài khoản: Q tìm theo tên / email / SĐT; Status theo trạng thái hiệu lực</summary>
public class AdminUserQuery
{
    public string? Q { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>08/10/2026 - Kích hoạt / gia hạn: Action = KICHHOAT (Months) hoặc GIAHANTHU (Days)</summary>
public class AdminPlanModel
{
    public string? Action { get; set; }
    public int Months { get; set; }
    public int Days { get; set; }
    public string? Note { get; set; }
}

public class AdminLockModel
{
    public bool Lock { get; set; }
    public string? Note { get; set; }
}

/// <summary>08/10/2026 - 1 dòng tài khoản ở trang quản trị</summary>
public class AdminUserReadModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = "";
    public string Status { get; set; } = "";          // trạng thái hiệu lực
    public DateTime TrialEnd { get; set; }
    public DateTime? PlanEnd { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime TimeCr { get; set; }
    public int Students { get; set; }
    public int Attempts { get; set; }
    public DateTime? LastAttempt { get; set; }
}

public class AdminUserPageReadModel
{
    public int Total { get; set; }
    public List<AdminUserReadModel> Items { get; set; } = new();
}

public class PlanLogReadModel
{
    public Guid Id { get; set; }
    public string Action { get; set; } = "";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Note { get; set; }
    public string AdminName { get; set; } = "";
    public DateTime TimeCr { get; set; }
}

/// <summary>08/10/2026 - Thống kê: số tài khoản theo trạng thái, hồ sơ con, lượt học theo ngày (30 ngày)</summary>
public class AdminStatsReadModel
{
    public int Users { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public int Students { get; set; }
    public int ActiveStudents7d { get; set; }
    public int Attempts { get; set; }
    public int Assignments { get; set; }
    public List<DayCountReadModel> AttemptsByDay { get; set; } = new();
    public List<DayCountReadModel> NewUsersByDay { get; set; } = new();
}

public class DayCountReadModel
{
    public string Day { get; set; } = "";             // yyyy-MM-dd theo giờ Việt Nam
    public int Count { get; set; }
}

/// <summary>08/10/2026 - Số liệu thô 1 tài khoản (repository trả về, service tính trạng thái hiệu lực)</summary>
public class AdminUserRow
{
    public Auth.AppUserModel User { get; set; } = new();
    public int Students { get; set; }
    public int Attempts { get; set; }
    public DateTime? LastAttempt { get; set; }
}
