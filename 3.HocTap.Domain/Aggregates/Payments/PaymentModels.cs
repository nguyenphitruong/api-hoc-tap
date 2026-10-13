namespace HocTap.Domain.Aggregates.Payments;

/// <summary>
/// 12/10/2026 - Model bảng payment_order: 1 đơn gia hạn gói bằng chuyển khoản QR.
/// code = nội dung chuyển khoản (HT + 8 ký tự) để đối soát giao dịch ngân hàng (webhook SePay / admin xác nhận tay).
/// </summary>
public class PaymentOrderModel
{
    public Guid id { get; set; }
    public string code { get; set; } = "";
    public Guid userid { get; set; }
    public string plancode { get; set; } = "";
    public int months { get; set; }
    public decimal amount { get; set; }
    public string status { get; set; } = "";            // CHOTT | DATT | HUY (HETHAN tính khi đọc)
    public decimal? paidamount { get; set; }
    public string? gateway { get; set; }                // SEPAY | ADMIN
    public string? txnref { get; set; }                 // mã giao dịch của cổng (chống cộng hạn 2 lần)
    public string? txncontent { get; set; }             // nội dung chuyển khoản thực nhận
    public DateTime? planfrom { get; set; }             // hạn gói trước / sau khi cộng (đối chiếu)
    public DateTime? planto { get; set; }
    public DateTime timecr { get; set; }
    public DateTime? paidat { get; set; }
    public DateTime? timeup { get; set; }
    public Guid? studentid { get; set; }                // 13/10/2026 - hồ sơ con được gia hạn (null = đơn cũ: áp dụng mọi con, mọi môn)
    public string? subjects { get; set; }               // 13/10/2026 - môn được gia hạn, nối bằng dấu phẩy: "toan" | "anh" | "anh,toan"
}

/// <summary>13/10/2026 - Model bảng student_plan: hạn học của 1 hồ sơ con cho 1 môn (khoá chính studentid + subject)</summary>
public class StudentPlanModel
{
    public Guid studentid { get; set; }
    public string subject { get; set; } = "";           // toan | anh
    public DateTime planend { get; set; }
    public DateTime timecr { get; set; }
    public DateTime? timeup { get; set; }
}

/// <summary>13/10/2026 - Hạn học 1 con × 1 môn trả client (trang phụ huynh, khoá môn ở phiên học sinh)</summary>
public class StudentPlanReadModel
{
    public Guid StudentId { get; set; }
    public string Subject { get; set; } = "";
    public DateTime PlanEnd { get; set; }
    public bool Active { get; set; }
}

/// <summary>12/10/2026 - 1 gói gia hạn (cấu hình Payment:Plans trong appsettings)</summary>
public class PlanPackageReadModel
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Months { get; set; }
    public decimal Price { get; set; }
    public string? Note { get; set; }
    public int Subjects { get; set; } = 2;              // 13/10/2026 - số môn của gói: 1 (chọn Toán hoặc Anh) | 2 (cả 2 môn)
}

/// <summary>12/10/2026 - Bảng giá + thông tin nhận tiền (công khai: trang giới thiệu, hộp thoại gia hạn)</summary>
public class PaymentConfigReadModel
{
    public bool Enabled { get; set; }                   // đã cấu hình tài khoản nhận tiền
    public bool AutoConfirm { get; set; }               // có webhook tự xác nhận
    public string BankName { get; set; } = "";
    public string AccountNo { get; set; } = "";
    public string AccountName { get; set; } = "";
    public List<PlanPackageReadModel> Plans { get; set; } = new();
}

/// <summary>12/10/2026 - Phụ huynh tạo đơn gia hạn</summary>
public class CreatePaymentOrderModel
{
    public string? PlanCode { get; set; }
    public Guid? StudentId { get; set; }                // 13/10/2026 - con được gia hạn (bắt buộc)
    public List<string>? Subjects { get; set; }         // 13/10/2026 - môn: đúng bằng số môn của gói
}

/// <summary>12/10/2026 - Đơn gia hạn trả về client (kèm ảnh QR VietQR để quét chuyển khoản)</summary>
public class PaymentOrderReadModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string PlanCode { get; set; } = "";
    public string PlanName { get; set; } = "";
    public int Months { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";            // trạng thái hiệu lực (CHOTT / DATT / HETHAN / HUY)
    public string QrUrl { get; set; } = "";
    public string BankName { get; set; } = "";
    public string AccountNo { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string TransferContent { get; set; } = "";
    public DateTime TimeCr { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? PlanTo { get; set; }               // hạn gói mới sau khi thanh toán
    public Guid? StudentId { get; set; }                // 13/10/2026
    public string StudentName { get; set; } = "";
    public List<string> Subjects { get; set; } = new();
}

/// <summary>
/// 12/10/2026 - Giao dịch ngân hàng nhận được (đã chuẩn hoá từ webhook cổng, vd SePay):
/// chỉ xử lý tiền vào (TransferType = in); Content chứa mã đơn HTxxxxxxxx.
/// </summary>
public class BankTransactionModel
{
    public string Gateway { get; set; } = "";
    public string TransactionId { get; set; } = "";
    public string? Content { get; set; }
    public string? Code { get; set; }                   // mã thanh toán cổng tự nhận diện (nếu có cấu hình)
    public decimal Amount { get; set; }
    public string? TransferType { get; set; }
    public string? AccountNumber { get; set; }
    public string? ReferenceCode { get; set; }
}

/// <summary>12/10/2026 - Kết quả xử lý 1 giao dịch webhook (ghi log, trả cổng thanh toán)</summary>
public class BankTransactionResultReadModel
{
    public bool Matched { get; set; }
    public string? OrderCode { get; set; }
    public string Message { get; set; } = "";
}

/// <summary>
/// 12/10/2026 - Payload webhook SePay (https://docs.sepay.vn - "Tích hợp WebHooks").
/// SePay gửi header "Authorization: Apikey {API_KEY}"; trả HTTP 200 + { success: true } để SePay không gửi lại.
/// </summary>
public class SepayWebhookModel
{
    public long id { get; set; }
    public string? gateway { get; set; }
    public string? transactionDate { get; set; }
    public string? accountNumber { get; set; }
    public string? code { get; set; }
    public string? content { get; set; }
    public string? transferType { get; set; }
    public decimal transferAmount { get; set; }
    public decimal accumulated { get; set; }
    public string? subAccount { get; set; }
    public string? referenceCode { get; set; }
    public string? description { get; set; }
}

/// <summary>12/10/2026 - Trang quản trị: 1 đơn gia hạn kèm thông tin phụ huynh</summary>
public class AdminPaymentReadModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public Guid UserId { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string PlanCode { get; set; } = "";
    public int Months { get; set; }
    public decimal Amount { get; set; }
    public decimal? PaidAmount { get; set; }
    public string Status { get; set; } = "";
    public string? Gateway { get; set; }
    public string? TxnRef { get; set; }
    public string? TxnContent { get; set; }
    public DateTime TimeCr { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? PlanTo { get; set; }
    public string StudentName { get; set; } = "";       // 13/10/2026 - con được gia hạn ("" = đơn cũ, mọi con)
    public List<string> Subjects { get; set; } = new();
}

/// <summary>12/10/2026 - Admin xác nhận tay đơn đã nhận tiền (webhook chưa cấu hình / khách ghi sai nội dung)</summary>
public class AdminConfirmPaymentModel
{
    public decimal? PaidAmount { get; set; }
    public string? Note { get; set; }
}

/// <summary>12/10/2026 - Số liệu thô 1 đơn (repository trả về, service tính trạng thái hiệu lực)</summary>
public class PaymentOrderRow
{
    public PaymentOrderModel Order { get; set; } = new();
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string StudentName { get; set; } = "";       // 13/10/2026
}
