namespace HocTap.Domain.Commons;

/// <summary>
/// 08/10/2026 - Hằng số dùng chung: mã chữ tiếng Việt không dấu (giống Web-MultiClinic) cho vai trò, trạng thái, hình thức học.
/// </summary>
public static class HocTapConst
{
    // ===== Vai trò (app_user.role; HOCSINH chỉ có trong token phiên học sinh) =====
    public const string ROLE_PARENT = "PHUHUYNH";
    public const string ROLE_ADMIN = "ADMIN";
    public const string ROLE_STUDENT = "HOCSINH";

    // ===== Trạng thái tài khoản (app_user.status) =====
    public const string STATUS_TRIAL = "DUNGTHU";      // dùng thử, hết hạn theo trialend
    public const string STATUS_ACTIVE = "HOATDONG";    // đã kích hoạt gói, hết hạn theo planend
    public const string STATUS_EXPIRED = "HETHAN";     // tính khi đọc (không cần tác vụ nền)
    public const string STATUS_LOCKED = "KHOA";        // admin khoá: không đăng nhập được

    // ===== Hình thức lượt học (attempt.mode) =====
    public const string MODE_PRACTICE = "LUYENTAP";
    public const string MODE_TEST = "KIEMTRA";

    // ===== Nguồn lượt học (attempt.source) =====
    public const string SOURCE_APP = "APP";
    public const string SOURCE_IMPORT = "NHAP";        // nhập từ file JSON / dữ liệu trên máy

    // ===== Giới hạn =====
    public const int TRIAL_DAYS = 14;
    public const int MAX_PROFILES = 4;
    public const int MAX_ITEMS = 60;                   // số câu tối đa 1 lượt gửi kèm
    public const int MAX_IMPORT = 5000;                // số lượt tối đa 1 lần nhập
    public const int MAX_QUESTION_JSON = 20000;        // độ dài tối đa nội dung 1 câu (ký tự)
    public static readonly short[] GRADES = { 2, 6 };
    public static readonly string[] SUBJECTS = { "toan", "anh" };

    // ===== Claim trong JWT =====
    public const string CLAIM_STUDENT = "sid";

    // ===== 08/10/2026 - Giai đoạn 4: giao bài tập =====
    public const string ASG_OPEN = "DANGGIAO";          // assignment.status: đang giao, con làm / nộp được
    public const string ASG_CLOSED = "DADONG";          // phụ huynh đóng: con không thấy / không nộp được
    public const string AS_TODO = "CHUALAM";            // assignment_student.status
    public const string AS_DOING = "DANGLAM";           // dự phòng (chưa dùng: không theo dõi lúc mở bài)
    public const string AS_DONE = "DANOP";
    public const string AS_OVERDUE = "QUAHAN";          // tính khi đọc: quá hạn mà chưa nộp
    public const int ASG_MIN_Q = 5;
    public const int ASG_MAX_Q = 20;
    public const int ASG_MAX_TOPICS = 3;
    public const int ASG_MAX_MINUTES = 120;
    public const string TOPIC_ASSIGN = "as";            // tiền tố topicid lượt làm bài được giao: as:g6.toan.<id>

    // ===== 08/10/2026 - Giai đoạn 5: quản trị (plan_log.action) =====
    public const string PLAN_ACTIVATE = "KICHHOAT";     // kích hoạt / gia hạn gói theo tháng
    public const string PLAN_TRIAL = "GIAHANTHU";       // gia hạn dùng thử theo ngày
    public const string PLAN_LOCK = "KHOA";
    public const string PLAN_UNLOCK = "MOKHOA";
    public const int PLAN_MAX_MONTHS = 36;
    public const int PLAN_MAX_TRIAL_DAYS = 90;
    public const int STATS_DAYS = 30;
}
