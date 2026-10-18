namespace HocTap.Domain.Aggregates.Feedbacks;

/// <summary>
/// 18/10/2026 - Model bảng feedback: 1 góp ý của phụ huynh / khách, hoặc 1 báo lỗi câu hỏi.
/// kind  : NOIDUNG | LOIDE | GIAODIEN | GIA | TINHNANG | KHAC (FeedbackRules)
/// status: MOI | DANGXL | XONG — admin đổi ở tab "Góp ý"; adminnote là ghi chú nội bộ (phụ huynh không thấy).
/// LOIDE kèm topicid + question (jsonb: đề, đáp án, lời giải của câu bị báo) để sửa đúng bộ sinh đề.
/// </summary>
public class FeedbackModel
{
    public Guid id { get; set; }
    public Guid? userid { get; set; }                  // tài khoản phụ huynh (phiên học sinh: tài khoản của bố mẹ)
    public Guid? studentid { get; set; }               // hồ sơ con khi gửi từ phiên học sinh
    public string? guestid { get; set; }               // khách chưa đăng nhập (mã học thử của trình duyệt)
    public string kind { get; set; } = "";
    public short? rating { get; set; }                 // 1–5 sao
    public short? grade { get; set; }
    public string? subject { get; set; }
    public string? topicid { get; set; }
    public string? question { get; set; }              // jsonb
    public string content { get; set; } = "";
    public bool contactok { get; set; }                // đồng ý cho liên hệ lại
    public string? contact { get; set; }               // khách tự điền (tài khoản: lấy email / SĐT của tài khoản)
    public string? page { get; set; }                  // trang đang mở khi góp ý
    public string status { get; set; } = "";
    public string? adminnote { get; set; }
    public DateTime timecr { get; set; }
    public DateTime? timeup { get; set; }
    public Guid? userup { get; set; }
}

/// <summary>18/10/2026 - Web gửi lên POST api/v1/feedback</summary>
public class SubmitFeedbackModel
{
    public string? Kind { get; set; }
    public short? Rating { get; set; }
    public short? Grade { get; set; }
    public string? Subject { get; set; }
    public string? TopicId { get; set; }
    public string? Question { get; set; }              // chuỗi JSON của câu bị báo lỗi
    public string? Content { get; set; }
    public bool ContactOk { get; set; }
    public string? Contact { get; set; }
    public string? Page { get; set; }
    public string? GuestId { get; set; }
}

/// <summary>18/10/2026 - Bộ lọc danh sách góp ý (admin)</summary>
public class AdminFeedbackQuery
{
    public string? Status { get; set; }
    public string? Kind { get; set; }
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}

/// <summary>18/10/2026 - Admin đổi trạng thái / ghi chú nội bộ</summary>
public class UpdateFeedbackModel
{
    public string? Status { get; set; }
    public string? AdminNote { get; set; }
}

public class AdminFeedbackReadModel
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = "";
    public short? Rating { get; set; }
    public short? Grade { get; set; }
    public string? Subject { get; set; }
    public string? TopicId { get; set; }
    public string? Question { get; set; }
    public string Content { get; set; } = "";
    public bool ContactOk { get; set; }
    public string? Page { get; set; }
    public string Status { get; set; } = "";
    public string? AdminNote { get; set; }
    public DateTime TimeCr { get; set; }
    public DateTime? TimeUp { get; set; }
    public string Sender { get; set; } = "";           // PHUHUYNH | HOCSINH | KHACH
    public string? UserName { get; set; }
    public string? StudentName { get; set; }
    public string? Contact { get; set; }               // chỉ hiện khi contactok = true
}

public class AdminFeedbackSummaryReadModel
{
    public int Total { get; set; }
    public int New { get; set; }
    public int InProgress { get; set; }
    public int Done { get; set; }
    public int OpenErrors { get; set; }                // báo lỗi đề chưa xong
    public decimal? AvgRating { get; set; }            // trung bình sao (góp ý có chấm sao)
    public int Last7Days { get; set; }
}

public class AdminFeedbackPageReadModel
{
    public int Total { get; set; }                     // số dòng sau khi lọc
    public List<AdminFeedbackReadModel> Items { get; set; } = new();
    public AdminFeedbackSummaryReadModel Summary { get; set; } = new();
}
