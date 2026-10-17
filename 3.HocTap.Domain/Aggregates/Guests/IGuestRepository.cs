namespace HocTap.Domain.Aggregates.Guests;

/// <summary>17/10/2026 - Repository hoạt động khách học thử (guest_activity)</summary>
public interface IGuestRepository
{
    /// <summary>Thêm 1 hoạt động; Service gọi unitOfWork.Save()</summary>
    void InsertActivity(GuestActivityModel i_activity);
    /// <summary>Số hoạt động của 1 mã khách từ thời điểm i_since (chặn gửi quá nhiều)</summary>
    int CountActivitySince(string i_guestId, DateTime i_since);
    /// <summary>Gắn tài khoản cho mọi hoạt động CHƯA gắn của mã khách (từ i_since); trả số dòng đã gắn. Service gọi unitOfWork.Save()</summary>
    int LinkGuest(string i_guestId, Guid i_userId, string i_via, DateTime i_now, DateTime i_since);
    /// <summary>Mọi hoạt động của các mã khách có hoạt động từ i_since (kể cả hoạt động cũ hơn của chính các mã đó), tối đa i_max dòng mới nhất</summary>
    List<GuestActivityModel> GetListActivitySince(DateTime i_since, int i_max);
}
