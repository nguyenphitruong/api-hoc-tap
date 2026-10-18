using System.Text.Json;
using System.Text.RegularExpressions;
using HocTap.Domain.Aggregates.Feedbacks;

namespace HocTap.Domain.Commons;

/// <summary>
/// 18/10/2026 - QUY TẮC GÓP Ý (feedback)
/// - Loại: NOIDUNG (nội dung bài học) | LOIDE (báo lỗi đề / đáp án) | GIAODIEN | GIA (giá, thanh toán) | TINHNANG (đề xuất) | KHAC.
/// - Nội dung 10–1000 ký tự (báo lỗi đề: 3–1000, vì đã kèm nội dung câu). Sao 1–5 (không bắt buộc).
/// - Báo lỗi đề: question là chuỗi JSON object ≤ HocTapConst.MAX_QUESTION_JSON ký tự.
/// - Trạng thái: MOI -> DANGXL -> XONG (admin đổi tự do).
/// </summary>
public static class FeedbackRules
{
    public const string KIND_CONTENT = "NOIDUNG";
    public const string KIND_ERROR = "LOIDE";
    public const string KIND_UI = "GIAODIEN";
    public const string KIND_PRICE = "GIA";
    public const string KIND_FEATURE = "TINHNANG";
    public const string KIND_OTHER = "KHAC";
    public static readonly string[] KINDS = { KIND_CONTENT, KIND_ERROR, KIND_UI, KIND_PRICE, KIND_FEATURE, KIND_OTHER };

    public const string STATUS_NEW = "MOI";
    public const string STATUS_DOING = "DANGXL";
    public const string STATUS_DONE = "XONG";
    public static readonly string[] STATUSES = { STATUS_NEW, STATUS_DOING, STATUS_DONE };

    public const string SENDER_PARENT = "PHUHUYNH";
    public const string SENDER_STUDENT = "HOCSINH";
    public const string SENDER_GUEST = "KHACH";

    public const int MAX_PER_DAY = 20;                 // 1 tài khoản / 1 mã khách tối đa 20 góp ý / ngày
    public const int MAX_CONTENT = 1000;
    public const int MAX_NOTE = 1000;

    private static readonly Regex TOPIC_RE = new(@"^[a-z0-9_.:\-]{1,80}$", RegexOptions.Compiled);

    /// <summary>Kiểm tra + chuẩn hoá thành Model (chưa có id, người gửi, thời gian). Sai -> LogicException 400 kèm thông báo tiếng Việt</summary>
    public static FeedbackModel ToModel(SubmitFeedbackModel i_model)
    {
        if (i_model == null) throw new LogicException("Thiếu nội dung góp ý.");
        string kind = (i_model.Kind ?? "").Trim().ToUpperInvariant();
        if (!KINDS.Contains(kind)) throw new LogicException("Vui lòng chọn loại góp ý.");
        string content = (i_model.Content ?? "").Trim();
        int min = kind == KIND_ERROR ? 3 : 10;
        if (content.Length < min) throw new LogicException(kind == KIND_ERROR ? "Vui lòng ghi ngắn gọn chỗ sai (ít nhất 3 ký tự)." : "Nội dung góp ý ít nhất 10 ký tự.");
        if (content.Length > MAX_CONTENT) throw new LogicException($"Nội dung góp ý tối đa {MAX_CONTENT} ký tự.");
        if (i_model.Rating.HasValue && (i_model.Rating < 1 || i_model.Rating > 5)) throw new LogicException("Số sao từ 1 đến 5.");

        FeedbackModel _Result = new FeedbackModel();
        _Result.kind = kind;
        _Result.content = content;
        _Result.rating = i_model.Rating;
        _Result.grade = i_model.Grade.HasValue && HocTapConst.GRADES.Contains(i_model.Grade.Value) ? i_model.Grade : null;
        string subject = (i_model.Subject ?? "").Trim().ToLowerInvariant();
        _Result.subject = HocTapConst.SUBJECTS.Contains(subject) ? subject : null;
        string topic = (i_model.TopicId ?? "").Trim().ToLowerInvariant();
        _Result.topicid = TOPIC_RE.IsMatch(topic) ? topic : null;
        _Result.question = NormalizeQuestion(i_model.Question);
        if (kind == KIND_ERROR && _Result.question == null && _Result.topicid == null)
            throw new LogicException("Báo lỗi câu hỏi cần kèm câu hỏi bị lỗi.");
        _Result.contactok = i_model.ContactOk;
        string contact = (i_model.Contact ?? "").Trim();
        _Result.contact = i_model.ContactOk && contact.Length > 0 ? contact[..Math.Min(100, contact.Length)] : null;
        string page = (i_model.Page ?? "").Trim();
        _Result.page = page.Length > 0 ? page[..Math.Min(200, page.Length)] : null;
        _Result.status = STATUS_NEW;
        return _Result;
    }

    /// <summary>Chuỗi JSON object hợp lệ, không quá dài -> giữ nguyên; trống / sai -> null (không làm hỏng việc gửi góp ý)</summary>
    public static string? NormalizeQuestion(string? i_json)
    {
        if (string.IsNullOrWhiteSpace(i_json) || i_json.Length > HocTapConst.MAX_QUESTION_JSON) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(i_json);
            return doc.RootElement.ValueKind == JsonValueKind.Object ? i_json : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Admin cập nhật: trạng thái hợp lệ (trống = giữ nguyên), ghi chú ≤ 1000 ký tự</summary>
    public static void ValidateUpdate(UpdateFeedbackModel i_model)
    {
        if (i_model == null) throw new LogicException("Thiếu dữ liệu cập nhật.");
        string status = (i_model.Status ?? "").Trim().ToUpperInvariant();
        if (status.Length > 0 && !STATUSES.Contains(status)) throw new LogicException("Trạng thái không hợp lệ.");
        if ((i_model.AdminNote ?? "").Length > MAX_NOTE) throw new LogicException($"Ghi chú tối đa {MAX_NOTE} ký tự.");
    }
}
