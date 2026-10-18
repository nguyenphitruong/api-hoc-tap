using HocTap.Application.Commons;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Feedbacks;
using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;

namespace HocTap.Application.Feedbacks;

/// <summary>18/10/2026 - Service góp ý: gửi góp ý / báo lỗi câu hỏi (không bắt buộc đăng nhập), admin xem và xử lý</summary>
public interface IFeedbackServices
{
    /// <summary>Gửi góp ý; i_caller = null khi chưa đăng nhập. Trả id góp ý</summary>
    Guid Submit(CurrentUser? i_caller, SubmitFeedbackModel i_model);
    AdminFeedbackPageReadModel GetListFeedback(CurrentUser i_caller, AdminFeedbackQuery i_query);
    AdminFeedbackReadModel UpdateFeedback(CurrentUser i_caller, Guid i_id, UpdateFeedbackModel i_model);
}

/// <summary>
/// 18/10/2026 - GÓP Ý (viết tường minh theo mẫu Web-MultiClinic)
/// - Submit: FeedbackRules.ToModel kiểm tra + chuẩn hoá. Người gửi:
///     đã đăng nhập -> userid (phiên học sinh: tài khoản bố mẹ + studentid); liên hệ lấy từ tài khoản lúc admin xem;
///     khách -> guestid (mã học thử của trình duyệt, có thể trống) + liên hệ tự điền.
///   Mỗi tài khoản / mã khách tối đa FeedbackRules.MAX_PER_DAY góp ý / 24 giờ (thêm giới hạn theo IP ở controller).
/// - Admin: danh sách lọc theo trạng thái / loại / từ khoá, phân trang, kèm số tổng hợp; đổi trạng thái + ghi chú nội bộ.
/// Mỗi thao tác ghi: InitTransaction -> Insert / Update -> Save (1 lần) -> CommitTransaction; lỗi -> RollbackTransaction.
/// </summary>
public class FeedbackServices : IFeedbackServices
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public FeedbackServices(IUnitOfWork i_UnitOfWork, IClock i_Clock)
    {
        unitOfWork = i_UnitOfWork;
        clock = i_Clock;
    }

    public Guid Submit(CurrentUser? i_caller, SubmitFeedbackModel i_model)
    {
        // 1. Kiểm tra + xác định người gửi
        FeedbackModel feedback = FeedbackRules.ToModel(i_model);
        DateTime now = clock.UtcNow;
        if (i_caller != null)
        {
            feedback.userid = i_caller.UserId;
            feedback.studentid = i_caller.Role == HocTapConst.ROLE_STUDENT ? i_caller.StudentId : null;
            feedback.contact = null;                      // dùng email / SĐT của tài khoản
        }
        else
        {
            feedback.guestid = GuestRules.NormalizeGuestId(i_model.GuestId);
        }
        if (unitOfWork.FeedbackRepo.CountFeedbackSince(feedback.userid, feedback.guestid, now.AddDays(-1)) >= FeedbackRules.MAX_PER_DAY)
        {
            throw new LogicException("Hôm nay bạn đã gửi nhiều góp ý, vui lòng thử lại vào ngày mai.", 429);
        }

        // 2. Ghi
        feedback.id = Guid.NewGuid();
        feedback.timecr = now;
        try
        {
            unitOfWork.InitTransaction();
            unitOfWork.FeedbackRepo.InsertFeedback(feedback);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
        return feedback.id;
    }

    public AdminFeedbackPageReadModel GetListFeedback(CurrentUser i_caller, AdminFeedbackQuery i_query)
    {
        ServiceGuard.EnsureAdmin(i_caller);
        AdminFeedbackPageReadModel _Result = new AdminFeedbackPageReadModel();
        AdminFeedbackQuery query = i_query ?? new AdminFeedbackQuery();
        List<FeedbackModel> lstAll = unitOfWork.FeedbackRepo.GetListFeedback();

        // 1. Số tổng hợp (trên toàn bộ góp ý)
        _Result.Summary = Summarize(lstAll, clock.UtcNow);

        // 2. Lọc trạng thái / loại / từ khoá (nội dung, mã chủ đề, ghi chú)
        string status = (query.Status ?? "").Trim().ToUpperInvariant();
        string kind = (query.Kind ?? "").Trim().ToUpperInvariant();
        string q = (query.Q ?? "").Trim().ToLowerInvariant();
        List<FeedbackModel> lstRow = lstAll.Where(x =>
            (status.Length == 0 || x.status == status) && (kind.Length == 0 || x.kind == kind) &&
            (q.Length == 0 || x.content.ToLowerInvariant().Contains(q) || (x.topicid ?? "").Contains(q) || (x.adminnote ?? "").ToLowerInvariant().Contains(q))).ToList();

        // 3. Phân trang (10–100 dòng / trang) + đọc tên người gửi
        int pageSize = Math.Clamp(query.PageSize, 10, 100);
        int page = Math.Max(1, query.Page);
        _Result.Total = lstRow.Count;
        Dictionary<Guid, AppUserModel?> users = new Dictionary<Guid, AppUserModel?>();
        foreach (FeedbackModel f in lstRow.Skip((page - 1) * pageSize).Take(pageSize)) _Result.Items.Add(ToReadModel(f, users));
        return _Result;
    }

    public AdminFeedbackReadModel UpdateFeedback(CurrentUser i_caller, Guid i_id, UpdateFeedbackModel i_model)
    {
        // 1. Kiểm tra
        ServiceGuard.EnsureAdmin(i_caller);
        FeedbackRules.ValidateUpdate(i_model);
        FeedbackModel feedback = unitOfWork.FeedbackRepo.GetFeedbackById(i_id) ?? throw new LogicException("Không tìm thấy góp ý.", 404);

        // 2. Cập nhật
        string status = (i_model.Status ?? "").Trim().ToUpperInvariant();
        if (status.Length > 0) feedback.status = status;
        string note = (i_model.AdminNote ?? "").Trim();
        feedback.adminnote = note.Length > 0 ? note : null;
        feedback.timeup = clock.UtcNow;
        feedback.userup = i_caller.UserId;
        try
        {
            unitOfWork.InitTransaction();
            unitOfWork.FeedbackRepo.UpdateFeedback(feedback);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
        return ToReadModel(feedback, new Dictionary<Guid, AppUserModel?>());
    }

    /// <summary>Tổng số, theo trạng thái, báo lỗi đề chưa xong, trung bình sao, số góp ý 7 ngày gần nhất</summary>
    private static AdminFeedbackSummaryReadModel Summarize(List<FeedbackModel> i_rows, DateTime i_now)
    {
        AdminFeedbackSummaryReadModel _Result = new AdminFeedbackSummaryReadModel();
        _Result.Total = i_rows.Count;
        _Result.New = i_rows.Count(x => x.status == FeedbackRules.STATUS_NEW);
        _Result.InProgress = i_rows.Count(x => x.status == FeedbackRules.STATUS_DOING);
        _Result.Done = i_rows.Count(x => x.status == FeedbackRules.STATUS_DONE);
        _Result.OpenErrors = i_rows.Count(x => x.kind == FeedbackRules.KIND_ERROR && x.status != FeedbackRules.STATUS_DONE);
        List<short> ratings = i_rows.Where(x => x.rating.HasValue).Select(x => x.rating!.Value).ToList();
        _Result.AvgRating = ratings.Count > 0 ? Math.Round((decimal)ratings.Average(x => x), 1) : null;
        _Result.Last7Days = i_rows.Count(x => x.timecr >= i_now.AddDays(-7));
        return _Result;
    }

    /// <summary>Model -> dòng hiển thị; liên hệ chỉ trả khi phụ huynh đồng ý (tài khoản: email / SĐT của tài khoản)</summary>
    private AdminFeedbackReadModel ToReadModel(FeedbackModel i_source, Dictionary<Guid, AppUserModel?> io_users)
    {
        AdminFeedbackReadModel _Result = new AdminFeedbackReadModel
        {
            Id = i_source.id, Kind = i_source.kind, Rating = i_source.rating, Grade = i_source.grade, Subject = i_source.subject,
            TopicId = i_source.topicid, Question = i_source.question, Content = i_source.content, ContactOk = i_source.contactok,
            Page = i_source.page, Status = i_source.status, AdminNote = i_source.adminnote, TimeCr = i_source.timecr, TimeUp = i_source.timeup,
            Sender = i_source.userid == null ? FeedbackRules.SENDER_GUEST : i_source.studentid != null ? FeedbackRules.SENDER_STUDENT : FeedbackRules.SENDER_PARENT,
        };
        if (i_source.userid.HasValue)
        {
            if (!io_users.TryGetValue(i_source.userid.Value, out AppUserModel? user))
            {
                user = unitOfWork.UserRepo.GetUserById(i_source.userid.Value);
                io_users[i_source.userid.Value] = user;
            }
            _Result.UserName = user?.fullname;
            if (i_source.contactok) _Result.Contact = user?.phone ?? user?.email;
            if (i_source.studentid.HasValue) _Result.StudentName = unitOfWork.StudentRepo.GetStudentById(i_source.studentid.Value)?.nickname;
        }
        else if (i_source.contactok)
        {
            _Result.Contact = i_source.contact;
        }
        return _Result;
    }
}
