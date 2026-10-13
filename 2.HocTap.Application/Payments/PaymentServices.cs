using System.Security.Cryptography;
using System.Text;
using HocTap.Application.Commons;
using HocTap.Domain.Aggregates.Admin;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Payments;
using HocTap.Domain.Aggregates.Students;   // 13/10/2026
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;

namespace HocTap.Application.Payments;

/// <summary>
/// 12/10/2026 - Cấu hình thanh toán (appsettings "Payment"):
///   BankBin (mã BIN ngân hàng theo VietQR, vd 970436 = Vietcombank), BankName, AccountNo, AccountName,
///   WebhookApiKey (khoá SePay gửi kèm header "Authorization: Apikey ..."; trống = tắt tự xác nhận),
///   Plans: [{ Code, Name, Months, Subjects, Price, Note }] — 13/10/2026: giá theo SỐ MÔN (1 | 2), không phân biệt lớp
/// </summary>
public class PaymentOptions
{
    public string BankBin { get; set; } = "";
    public string BankName { get; set; } = "";
    public string AccountNo { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string WebhookApiKey { get; set; } = "";
    public List<PlanPackageReadModel> Plans { get; set; } = new();
}

public interface IPaymentServices
{
    PaymentConfigReadModel GetConfig();
    PaymentOrderReadModel CreateOrder(CurrentUser i_caller, CreatePaymentOrderModel i_model);
    PaymentOrderReadModel GetOrder(CurrentUser i_caller, Guid i_orderId);
    BankTransactionResultReadModel HandleBankTransaction(string? i_apiKey, BankTransactionModel i_txn);
    List<AdminPaymentReadModel> GetListOrderForAdmin(CurrentUser i_caller, string? i_status);
    AdminPaymentReadModel ConfirmOrderByAdmin(CurrentUser i_caller, Guid i_orderId, AdminConfirmPaymentModel i_model);
}

/// <summary>
/// 12/10/2026 - GIA HẠN GÓI BẰNG CHUYỂN KHOẢN QR (viết tường minh theo mẫu Web-MultiClinic)
/// Luồng:
///   1. Phụ huynh chọn gói -> CreateOrder: tạo payment_order (mã HTxxxxxxxx = nội dung chuyển khoản, số tiền theo gói)
///      -> trả ảnh QR VietQR; đơn chờ thanh toán cùng gói còn hạn thì dùng lại (không sinh nhiều mã).
///   2. Phụ huynh quét QR chuyển khoản; client hỏi GetOrder mỗi vài giây.
///   3. Ngân hàng báo có tiền về qua cổng (SePay webhook) -> HandleBankTransaction:
///      kiểm tra khoá webhook, chỉ tiền vào, tìm mã đơn trong nội dung, số tiền >= giá gói, giao dịch chưa xử lý
///      -> cộng hạn gói (nối tiếp nếu còn hạn), trạng thái HOATDONG, đơn DATT, ghi plan_log THANHTOAN — 1 lần Save.
///   4. Admin xác nhận tay (ConfirmOrderByAdmin) khi chưa cấu hình webhook hoặc khách ghi sai nội dung.
/// Tài khoản đang khoá: vẫn ghi nhận tiền + cộng hạn nhưng giữ trạng thái KHOA (admin mở khoá sau).
/// Update 13/10/2026: mỗi đơn gia hạn cho 1 hồ sơ con × môn đã chọn (student_plan, PlanHelper.Extend);
///   đơn cũ không có studentid (tạo trước phase8) -> cộng cho mọi con đang hoạt động, cả 2 môn.
/// </summary>
public class PaymentServices : IPaymentServices
{
    private const int ADMIN_MAX_ROWS = 300;

    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly PaymentOptions options;

    public PaymentServices(IUnitOfWork i_UnitOfWork, IClock i_Clock, PaymentOptions i_Options)
    {
        unitOfWork = i_UnitOfWork;
        clock = i_Clock;
        options = i_Options;
    }

    /// <summary>Bảng giá + tài khoản nhận tiền (công khai)</summary>
    public PaymentConfigReadModel GetConfig()
    {
        PaymentConfigReadModel _Result = new PaymentConfigReadModel();
        _Result.Enabled = IsBankConfigured();
        _Result.AutoConfirm = !string.IsNullOrWhiteSpace(options.WebhookApiKey);
        _Result.BankName = options.BankName;
        _Result.AccountNo = options.AccountNo;
        _Result.AccountName = options.AccountName;
        _Result.Plans = options.Plans.Where(x => x.Months > 0 && x.Price > 0).ToList();
        return _Result;
    }

    /// <summary>Tạo (hoặc dùng lại) đơn gia hạn cho gói đã chọn</summary>
    public PaymentOrderReadModel CreateOrder(CurrentUser i_caller, CreatePaymentOrderModel i_model)
    {
        // 1. Kiểm tra
        ServiceGuard.EnsureParent(i_caller);
        if (!IsBankConfigured()) throw new LogicException("Chưa cấu hình tài khoản nhận tiền, vui lòng liên hệ quản trị.");
        PlanPackageReadModel plan = FindPlan(i_model.PlanCode);
        AppUserModel user = unitOfWork.UserRepo.GetUserById(i_caller.UserId) ?? throw new LogicException("Không tìm thấy tài khoản.", 404);
        if (user.status == HocTapConst.STATUS_LOCKED) throw new LogicException("Tài khoản đang bị khoá, vui lòng liên hệ quản trị.", 403);

        // 13/10/2026: con + môn được gia hạn (số môn phải đúng số môn của gói)
        if (!i_model.StudentId.HasValue) throw new LogicException("Chọn hồ sơ con cần gia hạn.");
        StudentProfileModel student = ServiceGuard.GetOwnedStudent(unitOfWork, i_caller, i_model.StudentId.Value);
        List<string> lstSubject = PlanRules.NormalizeSubjects(i_model.Subjects);
        if (lstSubject.Count != plan.Subjects) throw new LogicException($"Gói này dành cho {plan.Subjects} môn, hãy chọn đúng {plan.Subjects} môn.");
        string subjects = PlanRules.JoinSubjects(lstSubject);

        // 2. Đơn chờ thanh toán cùng gói, cùng con, cùng môn còn hạn -> dùng lại mã QR
        DateTime now = clock.UtcNow;
        PaymentOrderModel? pending = unitOfWork.PaymentRepo.GetLastPendingOrder(user.id, plan.Code);
        if (pending != null && pending.amount == plan.Price && pending.studentid == student.id && pending.subjects == subjects
            && PaymentRules.EffectiveOrderStatus(pending.status, pending.timecr, now) == HocTapConst.PAY_PENDING)
        {
            return ToReadModel(pending, now);
        }

        // 3. Đơn mới (mã không trùng)
        PaymentOrderModel order = new PaymentOrderModel();
        order.id = Guid.NewGuid();
        order.code = NewUniqueCode();
        order.userid = user.id;
        order.plancode = plan.Code;
        order.months = plan.Months;
        order.amount = plan.Price;
        order.status = HocTapConst.PAY_PENDING;
        order.timecr = now;
        order.studentid = student.id;   // 13/10/2026
        order.subjects = subjects;
        try
        {
            unitOfWork.InitTransaction();
            unitOfWork.PaymentRepo.InsertOrder(order);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
        return ToReadModel(order, now);
    }

    /// <summary>Phụ huynh xem trạng thái đơn của mình (client hỏi định kỳ khi đang hiện QR)</summary>
    public PaymentOrderReadModel GetOrder(CurrentUser i_caller, Guid i_orderId)
    {
        ServiceGuard.EnsureParent(i_caller);
        PaymentOrderModel? order = unitOfWork.PaymentRepo.GetOrderById(i_orderId);
        if (order == null || order.userid != i_caller.UserId) throw new LogicException("Không tìm thấy đơn gia hạn.", 404);
        return ToReadModel(order, clock.UtcNow);
    }

    /// <summary>
    /// Webhook ngân hàng (SePay): luôn trả kết quả (không ném lỗi nghiệp vụ) để cổng không gửi lại vô hạn,
    /// trừ sai khoá (401). Giao dịch đã xử lý -> trả "đã ghi nhận" (idempotent).
    /// </summary>
    public BankTransactionResultReadModel HandleBankTransaction(string? i_apiKey, BankTransactionModel i_txn)
    {
        BankTransactionResultReadModel _Result = new BankTransactionResultReadModel();

        // 1. Khoá webhook
        if (string.IsNullOrWhiteSpace(options.WebhookApiKey) || !FixedEquals(ExtractApiKey(i_apiKey), options.WebhookApiKey.Trim()))
        {
            throw new LogicException("Sai khoá webhook.", 401);
        }

        // 2. Chỉ xử lý tiền vào, có mã giao dịch
        if (!string.Equals(i_txn.TransferType ?? "in", "in", StringComparison.OrdinalIgnoreCase))
        {
            _Result.Message = "Bỏ qua giao dịch tiền ra.";
            return _Result;
        }
        if (string.IsNullOrWhiteSpace(i_txn.TransactionId))
        {
            _Result.Message = "Thiếu mã giao dịch.";
            return _Result;
        }
        string gateway = string.IsNullOrWhiteSpace(i_txn.Gateway) ? "SEPAY" : i_txn.Gateway.Trim().ToUpperInvariant();
        PaymentOrderModel? done = unitOfWork.PaymentRepo.GetOrderByTxnRef(gateway, i_txn.TransactionId);
        if (done != null)
        {
            _Result.Matched = true;
            _Result.OrderCode = done.code;
            _Result.Message = "Giao dịch đã được ghi nhận trước đó.";
            return _Result;
        }

        // 3. Tìm đơn theo mã trong nội dung (ưu tiên mã cổng đã nhận diện)
        List<string> codes = PaymentRules.FindCodes(i_txn.Code);
        foreach (string c in PaymentRules.FindCodes(i_txn.Content)) if (!codes.Contains(c)) codes.Add(c);
        PaymentOrderModel? order = null;
        foreach (string code in codes)
        {
            order = unitOfWork.PaymentRepo.GetOrderByCode(code);
            if (order != null) break;
        }
        if (order == null)
        {
            _Result.Message = "Không tìm thấy mã đơn trong nội dung chuyển khoản.";
            return _Result;
        }
        _Result.OrderCode = order.code;
        if (order.status == HocTapConst.PAY_PAID)
        {
            _Result.Matched = true;
            _Result.Message = "Đơn đã thanh toán trước đó (giao dịch trùng mã).";
            return _Result;
        }
        if (i_txn.Amount < order.amount)
        {
            _Result.Message = $"Số tiền {i_txn.Amount:0} nhỏ hơn giá gói {order.amount:0}, chờ admin xử lý.";
            return _Result;
        }

        // 4. Cộng hạn
        string note = $"Thanh toán tự động {gateway} #{i_txn.TransactionId}: {i_txn.Amount:#,0}đ" + (string.IsNullOrWhiteSpace(i_txn.ReferenceCode) ? "" : $" (ref {i_txn.ReferenceCode})");
        ApplyPaid(order, i_txn.Amount, gateway, i_txn.TransactionId, i_txn.Content, null, note);
        _Result.Matched = true;
        _Result.Message = "Đã gia hạn.";
        return _Result;
    }

    public List<AdminPaymentReadModel> GetListOrderForAdmin(CurrentUser i_caller, string? i_status)
    {
        ServiceGuard.EnsureAdmin(i_caller);
        DateTime now = clock.UtcNow;
        // HETHAN là trạng thái tính khi đọc -> đọc CHOTT rồi lọc
        string? stored = i_status == HocTapConst.PAY_EXPIRED ? HocTapConst.PAY_PENDING : i_status;
        List<AdminPaymentReadModel> lstResult = new List<AdminPaymentReadModel>();
        foreach (PaymentOrderRow row in unitOfWork.PaymentRepo.GetListOrder(string.IsNullOrEmpty(stored) ? null : stored, ADMIN_MAX_ROWS))
        {
            AdminPaymentReadModel read = ToAdminReadModel(row, now);
            if (string.IsNullOrEmpty(i_status) || read.Status == i_status) lstResult.Add(read);
        }
        return lstResult;
    }

    /// <summary>Admin xác nhận đã nhận tiền (kiểm tra sao kê) -> cộng hạn như webhook</summary>
    public AdminPaymentReadModel ConfirmOrderByAdmin(CurrentUser i_caller, Guid i_orderId, AdminConfirmPaymentModel i_model)
    {
        ServiceGuard.EnsureAdmin(i_caller);
        PaymentOrderModel order = unitOfWork.PaymentRepo.GetOrderById(i_orderId) ?? throw new LogicException("Không tìm thấy đơn gia hạn.", 404);
        if (order.status == HocTapConst.PAY_PAID) throw new LogicException("Đơn đã thanh toán.");
        if (order.status == HocTapConst.PAY_CANCELLED) throw new LogicException("Đơn đã huỷ.");
        decimal paid = i_model.PaidAmount ?? order.amount;
        if (paid <= 0) throw new LogicException("Số tiền không hợp lệ.");

        string note = $"Admin xác nhận đã nhận {paid:#,0}đ" + (string.IsNullOrWhiteSpace(i_model.Note) ? "" : ": " + i_model.Note.Trim());
        if (note.Length > 300) note = note[..300];
        ApplyPaid(order, paid, HocTapConst.PAY_GATEWAY_ADMIN, "ADMIN-" + order.code, null, i_caller.UserId, note);

        AppUserModel? user = unitOfWork.UserRepo.GetUserById(order.userid);
        string studentName = order.studentid.HasValue ? unitOfWork.StudentRepo.GetStudentById(order.studentid.Value)?.nickname ?? "" : "";   // 13/10/2026
        return ToAdminReadModel(new PaymentOrderRow { Order = order, FullName = user?.fullname ?? "", Email = user?.email, Phone = user?.phone, StudentName = studentName }, clock.UtcNow);
    }

    // ===================== Hàm phụ =====================

    /// <summary>Đơn -> đã thanh toán; tài khoản cộng hạn gói; ghi plan_log (người thực hiện = admin hoặc chính phụ huynh khi tự động)</summary>
    private void ApplyPaid(PaymentOrderModel i_order, decimal i_paidAmount, string i_gateway, string i_txnRef, string? i_content, Guid? i_adminId, string i_note)
    {
        AppUserModel user = unitOfWork.UserRepo.GetUserById(i_order.userid) ?? throw new LogicException("Không tìm thấy tài khoản của đơn.", 404);
        DateTime now = clock.UtcNow;

        // 13/10/2026: con × môn được cộng hạn (đơn cũ không ghi con -> mọi con đang hoạt động, cả 2 môn)
        List<Guid> lstStudentId;
        List<string> lstSubject;
        string target;
        if (i_order.studentid.HasValue)
        {
            StudentProfileModel? student = unitOfWork.StudentRepo.GetStudentById(i_order.studentid.Value);
            lstStudentId = new List<Guid> { i_order.studentid.Value };
            lstSubject = PlanRules.SplitSubjects(i_order.subjects);
            target = $"{student?.nickname ?? "con"} – {PlanRules.SubjectNames(lstSubject)}";
        }
        else
        {
            lstStudentId = unitOfWork.StudentRepo.GetListStudentByUser(user.id).Select(x => x.id).ToList();
            lstSubject = HocTapConst.SUBJECTS.ToList();
            target = "mọi con – cả 2 môn (đơn cũ)";
        }

        i_order.status = HocTapConst.PAY_PAID;
        i_order.paidamount = i_paidAmount;
        i_order.gateway = i_gateway;
        i_order.txnref = i_txnRef.Length > 100 ? i_txnRef[..100] : i_txnRef;
        i_order.txncontent = i_content != null && i_content.Length > 300 ? i_content[..300] : i_content;
        i_order.paidat = now;
        i_order.timeup = now;

        PlanLogModel log = new PlanLogModel();
        log.id = Guid.NewGuid();
        log.userid = user.id;
        log.adminid = i_adminId ?? user.id;
        log.action = HocTapConst.PLAN_PAYMENT;
        log.note = $"Đơn {i_order.code} - {i_order.months} tháng ({target}). {i_note}";
        if (log.note.Length > 300) log.note = log.note[..300];
        log.timecr = now;

        try
        {
            unitOfWork.InitTransaction();
            // Cộng hạn sau InitTransaction (InitTransaction xoá thay đổi chưa lưu); PlanHelper không Save
            (DateTime from, DateTime to) = PlanHelper.Extend(unitOfWork, user, lstStudentId, lstSubject, i_order.months, now);
            i_order.planfrom = from;
            i_order.planto = to;
            log.fromdate = from;
            log.todate = to;
            unitOfWork.UserRepo.UpdateUser(user);
            unitOfWork.PaymentRepo.UpdateOrder(i_order);
            unitOfWork.AdminRepo.InsertPlanLog(log);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
    }

    private bool IsBankConfigured() => !string.IsNullOrWhiteSpace(options.BankBin) && !string.IsNullOrWhiteSpace(options.AccountNo);

    private PlanPackageReadModel FindPlan(string? i_code)
    {
        PlanPackageReadModel? plan = options.Plans.FirstOrDefault(x => string.Equals(x.Code, (i_code ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
        if (plan == null || plan.Months < 1 || plan.Price <= 0) throw new LogicException("Gói gia hạn không hợp lệ.");
        return plan;
    }

    private string NewUniqueCode()
    {
        for (int i = 0; i < 10; i++)
        {
            string code = PaymentRules.NewCode();
            if (unitOfWork.PaymentRepo.GetOrderByCode(code) == null) return code;
        }
        throw new LogicException("Không tạo được mã đơn, vui lòng thử lại.");
    }

    private PaymentOrderReadModel ToReadModel(PaymentOrderModel i_order, DateTime i_now)
    {
        PlanPackageReadModel? plan = options.Plans.FirstOrDefault(x => x.Code == i_order.plancode);
        PaymentOrderReadModel read = new PaymentOrderReadModel();
        read.Id = i_order.id;
        read.Code = i_order.code;
        read.PlanCode = i_order.plancode;
        read.PlanName = plan?.Name ?? $"{i_order.months} tháng";
        read.Months = i_order.months;
        read.Amount = i_order.amount;
        read.Status = PaymentRules.EffectiveOrderStatus(i_order.status, i_order.timecr, i_now);
        read.BankName = options.BankName;
        read.AccountNo = options.AccountNo;
        read.AccountName = options.AccountName;
        read.TransferContent = i_order.code;
        read.QrUrl = PaymentRules.VietQrUrl(options.BankBin, options.AccountNo, i_order.amount, i_order.code, options.AccountName);
        read.TimeCr = i_order.timecr;
        read.ExpiresAt = i_order.timecr.AddHours(HocTapConst.PAY_ORDER_HOURS);
        read.PaidAt = i_order.paidat;
        read.PlanTo = i_order.planto;
        read.StudentId = i_order.studentid;   // 13/10/2026
        read.StudentName = i_order.studentid.HasValue ? unitOfWork.StudentRepo.GetStudentById(i_order.studentid.Value)?.nickname ?? "" : "";
        read.Subjects = PlanRules.SplitSubjects(i_order.subjects);
        return read;
    }

    private static AdminPaymentReadModel ToAdminReadModel(PaymentOrderRow i_row, DateTime i_now)
    {
        PaymentOrderModel o = i_row.Order;
        AdminPaymentReadModel read = new AdminPaymentReadModel();
        read.Id = o.id;
        read.Code = o.code;
        read.UserId = o.userid;
        read.FullName = i_row.FullName;
        read.Email = i_row.Email;
        read.Phone = i_row.Phone;
        read.PlanCode = o.plancode;
        read.Months = o.months;
        read.Amount = o.amount;
        read.PaidAmount = o.paidamount;
        read.Status = PaymentRules.EffectiveOrderStatus(o.status, o.timecr, i_now);
        read.Gateway = o.gateway;
        read.TxnRef = o.txnref;
        read.TxnContent = o.txncontent;
        read.TimeCr = o.timecr;
        read.PaidAt = o.paidat;
        read.PlanTo = o.planto;
        read.StudentName = i_row.StudentName;   // 13/10/2026
        read.Subjects = PlanRules.SplitSubjects(o.subjects);
        return read;
    }

    /// <summary>Header "Apikey xxx" (SePay) hoặc chỉ "xxx"</summary>
    private static string ExtractApiKey(string? i_header)
    {
        string s = (i_header ?? "").Trim();
        if (s.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase)) s = s[7..].Trim();
        else if (s.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) s = s[7..].Trim();
        return s;
    }

    private static bool FixedEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
