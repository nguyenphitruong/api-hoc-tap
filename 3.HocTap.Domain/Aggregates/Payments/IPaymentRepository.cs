namespace HocTap.Domain.Aggregates.Payments;

/// <summary>12/10/2026 - Repository đơn gia hạn (payment_order)</summary>
public interface IPaymentRepository
{
    void InsertOrder(PaymentOrderModel i_order);
    void UpdateOrder(PaymentOrderModel i_order);
    PaymentOrderModel? GetOrderById(Guid i_id);
    PaymentOrderModel? GetOrderByCode(string i_code);
    /// <summary>Đơn đã ghi nhận giao dịch này (chống cộng hạn 2 lần khi cổng gửi lại webhook)</summary>
    PaymentOrderModel? GetOrderByTxnRef(string i_gateway, string i_txnRef);
    /// <summary>Đơn chờ thanh toán mới nhất của tài khoản cho 1 gói (dùng lại mã QR thay vì tạo đơn mới)</summary>
    PaymentOrderModel? GetLastPendingOrder(Guid i_userId, string i_planCode);
    /// <summary>Danh sách đơn (mới nhất trước) kèm thông tin phụ huynh; i_status rỗng = tất cả</summary>
    List<PaymentOrderRow> GetListOrder(string? i_status, int i_max);

    // ===== 13/10/2026 - Hạn học theo hồ sơ con × môn (student_plan) =====
    /// <summary>Hạn học của mọi hồ sơ con CÒN HOẠT ĐỘNG thuộc tài khoản</summary>
    List<StudentPlanModel> GetListStudentPlanByUser(Guid i_userId);
    /// <summary>Hạn học của 1 hồ sơ con (0–2 dòng)</summary>
    List<StudentPlanModel> GetListStudentPlan(Guid i_studentId);
    /// <summary>Thêm mới hoặc cập nhật hạn (studentid + subject); Service gọi unitOfWork.Save()</summary>
    void UpsertStudentPlan(StudentPlanModel i_plan);
}
