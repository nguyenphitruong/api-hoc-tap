using System.Security.Cryptography;
using System.Text.Json;
using HocTap.Application.Commons;
using HocTap.Domain.Aggregates.Assignments;
using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;

namespace HocTap.Application.Assignments;

/// <summary>
/// 08/10/2026 - GIAO BÀI TẬP (viết tường minh theo mẫu Web-MultiClinic)
/// - Phụ huynh giao: con cùng lớp, 1 môn, 1–3 chủ đề, mức, 5–20 câu, thời gian (tuỳ chọn), hạn nộp, lời nhắn. Server tạo seed;
///   trình duyệt của con và của phụ huynh sinh cùng 1 đề từ (chủ đề, mức, số câu, seed).
/// - Con thấy bài đang giao (DANGGIAO), làm lại nhiều lần; server giữ điểm cao nhất + mọi lượt (AttemptServices cập nhật).
/// - Quá hạn chưa nộp → QUAHAN (tính khi đọc); nộp sau hạn vẫn lưu, đánh dấu nộp trễ. Bài đóng (DADONG) không nộp được.
/// - "Giao lại câu sai": bài mới cho 1 con, nội dung là các câu sai ở lượt gần nhất (lưu sẵn trong questions).
/// Luồng ghi: kiểm tra -> InitTransaction -> unitOfWork.AssignmentRepo.Insert/Update -> Save -> Commit; lỗi -> Rollback.
/// </summary>
public class AssignmentServices : IAssignmentServices
{
    private const int MAX_LIST = 200;

    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public AssignmentServices(IUnitOfWork i_UnitOfWork, IClock i_Clock)
    {
        unitOfWork = i_UnitOfWork;
        clock = i_Clock;
    }

    public List<AssignmentReadModel> GetListAssignment(CurrentUser i_caller)
    {
        List<AssignmentReadModel> lstResult = new List<AssignmentReadModel>();
        ServiceGuard.EnsureParent(i_caller);

        List<AssignmentModel> lstAssignment = unitOfWork.AssignmentRepo.GetListAssignmentByParent(i_caller.UserId, MAX_LIST);
        List<Guid> lstAssignmentId = lstAssignment.Select(x => x.id).ToList();
        List<AssignmentStudentRow> lstRow = unitOfWork.AssignmentRepo.GetListAssignmentStudentRow(lstAssignmentId);
        foreach (AssignmentModel assignment in lstAssignment)
        {
            List<AssignmentStudentRow> lstRowOfAssignment = lstRow.Where(r => r.Row.assignmentid == assignment.id).ToList();
            lstResult.Add(ToReadModel(assignment, lstRowOfAssignment));
        }
        return lstResult;
    }

    public AssignmentReadModel InsertAssignment(CurrentUser i_caller, AssignmentCreateModel i_model)
    {
        // 1. Kiểm tra dữ liệu
        ServiceGuard.EnsureParent(i_caller);
        DateTime now = clock.UtcNow;
        string subject = i_model.Subject ?? "";
        if (!HocTapConst.GRADES.Contains(i_model.Grade)) throw new LogicException("Lớp chỉ được chọn 2 hoặc 6.");
        if (!HocTapConst.SUBJECTS.Contains(subject)) throw new LogicException("Môn không hợp lệ.");

        List<string> lstTopic = (i_model.TopicIds ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (lstTopic.Count < 1 || lstTopic.Count > HocTapConst.ASG_MAX_TOPICS) throw new LogicException($"Chọn từ 1 đến {HocTapConst.ASG_MAX_TOPICS} chủ đề.");
        foreach (string topicId in lstTopic)
        {
            var parsed = AttemptRules.ParseTopic(topicId);
            if (parsed == null || parsed.Value.Kind != "topic" || parsed.Value.Grade != i_model.Grade || parsed.Value.Subject != subject)
            {
                throw new LogicException("Chủ đề không thuộc lớp / môn đã chọn.");
            }
        }
        if (i_model.Level < 1 || i_model.Level > 3) throw new LogicException("Mức độ không hợp lệ.");
        if (i_model.QuestionCount < HocTapConst.ASG_MIN_Q || i_model.QuestionCount > HocTapConst.ASG_MAX_Q)
        {
            throw new LogicException($"Số câu từ {HocTapConst.ASG_MIN_Q} đến {HocTapConst.ASG_MAX_Q}.");
        }
        ValidateTimeLimit(i_model.TimeLimitMin);
        DateTime dueDate = ToUtc(i_model.DueDate);
        if (dueDate <= now) throw new LogicException("Hạn nộp phải sau thời điểm hiện tại.");

        List<Guid> lstStudentId = (i_model.StudentIds ?? new List<Guid>()).Distinct().ToList();
        if (lstStudentId.Count == 0) throw new LogicException("Chọn ít nhất 1 con để giao bài.");
        foreach (Guid studentId in lstStudentId)
        {
            StudentProfileModel student = ServiceGuard.GetOwnedStudent(unitOfWork, i_caller, studentId);
            if (student.grade != i_model.Grade)
            {
                throw new LogicException($"{student.nickname} học lớp {student.grade}, không giao được bài lớp {i_model.Grade}.");
            }
        }

        // 2. Tạo bài
        AssignmentModel assignment = new AssignmentModel();
        assignment.id = Guid.NewGuid();
        assignment.parentid = i_caller.UserId;
        assignment.title = CleanTitle(i_model.Title);
        assignment.grade = i_model.Grade;
        assignment.subject = subject;
        assignment.topicids = lstTopic.ToArray();
        assignment.level = i_model.Level;
        assignment.questioncount = i_model.QuestionCount;
        assignment.timelimitmin = i_model.TimeLimitMin;
        assignment.seed = RandomNumberGenerator.GetInt32(1, int.MaxValue);
        assignment.duedate = dueDate;
        assignment.note = CleanNote(i_model.Note);
        assignment.status = HocTapConst.ASG_OPEN;
        assignment.timecr = now;

        // 3. Lưu bài + dòng từng con
        SaveNewAssignment(assignment, lstStudentId);
        return GetReadModel(assignment);
    }

    public AssignmentReadModel UpdateAssignment(CurrentUser i_caller, Guid i_id, AssignmentUpdateModel i_model)
    {
        // 1. Kiểm tra
        AssignmentModel assignment = GetOwnedAssignment(i_caller, i_id);
        if (i_model.Status != null && i_model.Status != HocTapConst.ASG_OPEN && i_model.Status != HocTapConst.ASG_CLOSED)
        {
            throw new LogicException("Trạng thái không hợp lệ.");
        }
        if (!i_model.ClearTimeLimit) ValidateTimeLimit(i_model.TimeLimitMin);

        // 2. Gán giá trị (trường null = giữ nguyên)
        if (i_model.Title != null) assignment.title = CleanTitle(i_model.Title);
        if (i_model.Note != null) assignment.note = CleanNote(i_model.Note);
        if (i_model.DueDate.HasValue) assignment.duedate = ToUtc(i_model.DueDate.Value);
        if (i_model.ClearTimeLimit) assignment.timelimitmin = null;
        else if (i_model.TimeLimitMin.HasValue) assignment.timelimitmin = i_model.TimeLimitMin;
        if (i_model.Status != null) assignment.status = i_model.Status;
        assignment.timeup = clock.UtcNow;

        // 3. Lưu
        try
        {
            unitOfWork.InitTransaction();
            unitOfWork.AssignmentRepo.UpdateAssignment(assignment);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
        return GetReadModel(assignment);
    }

    public AssignmentReadModel InsertRetryWrong(CurrentUser i_caller, Guid i_id, AssignmentRetryModel i_model)
    {
        // 1. Kiểm tra bài gốc + lượt gần nhất của con
        AssignmentModel source = GetOwnedAssignment(i_caller, i_id);
        StudentProfileModel student = ServiceGuard.GetOwnedStudent(unitOfWork, i_caller, i_model.StudentId);
        if (unitOfWork.AssignmentRepo.GetAssignmentStudent(source.id, student.id) == null) throw new LogicException("Bài này chưa giao cho con.");
        AttemptModel? lastAttempt = unitOfWork.AssignmentRepo.GetLatestAttemptOfStudent(source.id, student.id);
        if (lastAttempt == null) throw new LogicException($"{student.nickname} chưa nộp bài này.");

        List<string> lstWrongQuestion = lastAttempt.items.Where(x => !x.iscorrect).OrderBy(x => x.idx).Select(x => x.question).ToList();
        if (lstWrongQuestion.Count == 0) throw new LogicException($"{student.nickname} không làm sai câu nào ở lần nộp gần nhất. 🎉");

        DateTime now = clock.UtcNow;
        DateTime dueDate = i_model.DueDate.HasValue ? ToUtc(i_model.DueDate.Value) : now.AddDays(3);
        if (dueDate <= now) throw new LogicException("Hạn nộp phải sau thời điểm hiện tại.");

        // 2. Bài mới: lưu sẵn các câu sai
        AssignmentModel assignment = new AssignmentModel();
        assignment.id = Guid.NewGuid();
        assignment.parentid = i_caller.UserId;
        assignment.title = CleanTitle("Làm lại câu sai: " + source.title);
        assignment.grade = source.grade;
        assignment.subject = source.subject;
        assignment.topicids = source.topicids;
        assignment.level = source.level;
        assignment.questioncount = lstWrongQuestion.Count;
        assignment.timelimitmin = source.timelimitmin;
        assignment.seed = source.seed;
        assignment.duedate = dueDate;
        assignment.note = source.note;
        assignment.status = HocTapConst.ASG_OPEN;
        assignment.questions = "[" + string.Join(",", lstWrongQuestion) + "]";
        assignment.sourceid = source.id;
        assignment.timecr = now;

        // 3. Lưu
        SaveNewAssignment(assignment, new List<Guid> { student.id });
        return GetReadModel(assignment);
    }

    public AssignmentResultReadModel GetAssignmentResult(CurrentUser i_caller, Guid i_id)
    {
        AssignmentResultReadModel _Result = new AssignmentResultReadModel();
        AssignmentModel assignment = GetOwnedAssignment(i_caller, i_id);

        _Result.Assignment = GetReadModel(assignment);
        List<AttemptModel> lstAttempt = unitOfWork.AssignmentRepo.GetListAttemptOfAssignment(assignment.id);
        foreach (AttemptModel attempt in lstAttempt)
        {
            AssignmentAttemptReadModel item = new AssignmentAttemptReadModel();
            item.Id = attempt.id;
            item.StudentId = attempt.studentid;
            item.Total = attempt.total;
            item.Correct = attempt.correct;
            item.Score10 = attempt.score10;
            item.DurationSec = attempt.durationsec;
            item.FinishedAt = attempt.finishedat;
            item.IsLate = attempt.finishedat > assignment.duedate;
            _Result.Attempts.Add(item);
        }
        return _Result;
    }

    /// <summary>Chi tiết (kèm câu hỏi nếu bài lưu sẵn câu): phụ huynh chủ bài hoặc học sinh được giao (trả thêm Mine)</summary>
    public AssignmentReadModel GetAssignmentDetail(CurrentUser i_caller, Guid i_id)
    {
        AssignmentModel? assignment = unitOfWork.AssignmentRepo.GetAssignmentById(i_id);
        if (assignment == null) throw new LogicException("Không tìm thấy bài giao.", 404);

        if (i_caller.Role == HocTapConst.ROLE_STUDENT)
        {
            AssignmentStudentModel? row = i_caller.StudentId.HasValue ? unitOfWork.AssignmentRepo.GetAssignmentStudent(assignment.id, i_caller.StudentId.Value) : null;
            if (row == null || assignment.parentid != i_caller.UserId) throw new LogicException("Không tìm thấy bài giao.", 404);
            AssignmentReadModel _Result = ToReadModel(assignment, new List<AssignmentStudentRow>());
            _Result.Mine = ToStudentReadModel(row, "", "", assignment.duedate);
            return _Result;
        }

        if (assignment.parentid != i_caller.UserId) throw new LogicException("Không tìm thấy bài giao.", 404);
        return GetReadModel(assignment);
    }

    public List<AssignmentReadModel> GetListAssignmentOfStudent(CurrentUser i_caller, Guid i_studentId)
    {
        List<AssignmentReadModel> lstResult = new List<AssignmentReadModel>();
        ServiceGuard.GetOwnedStudent(unitOfWork, i_caller, i_studentId);

        List<AssignmentOfStudentRow> lstRow = unitOfWork.AssignmentRepo.GetListOpenAssignmentOfStudent(i_studentId);
        foreach (AssignmentOfStudentRow item in lstRow)
        {
            AssignmentReadModel read = ToReadModel(item.Assignment, new List<AssignmentStudentRow>());
            read.Mine = ToStudentReadModel(item.Row, "", "", item.Assignment.duedate);
            lstResult.Add(read);
        }
        return lstResult;
    }

    // ===== Nội bộ =====

    /// <summary>Lưu bài mới + 1 dòng assignment_student (CHUALAM) cho mỗi con</summary>
    private void SaveNewAssignment(AssignmentModel i_assignment, List<Guid> i_lstStudentId)
    {
        List<AssignmentStudentModel> lstRow = new List<AssignmentStudentModel>();
        foreach (Guid studentId in i_lstStudentId)
        {
            AssignmentStudentModel row = new AssignmentStudentModel();
            row.assignmentid = i_assignment.id;
            row.studentid = studentId;
            row.status = HocTapConst.AS_TODO;
            row.attemptcount = 0;
            row.islate = false;
            lstRow.Add(row);
        }

        try
        {
            unitOfWork.InitTransaction();
            unitOfWork.AssignmentRepo.InsertAssignment(i_assignment, lstRow);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
    }

    private AssignmentReadModel GetReadModel(AssignmentModel i_assignment)
    {
        List<AssignmentStudentRow> lstRow = unitOfWork.AssignmentRepo.GetListAssignmentStudentRow(new List<Guid> { i_assignment.id });
        return ToReadModel(i_assignment, lstRow);
    }

    private AssignmentModel GetOwnedAssignment(CurrentUser i_caller, Guid i_id)
    {
        ServiceGuard.EnsureParent(i_caller);
        AssignmentModel? assignment = unitOfWork.AssignmentRepo.GetAssignmentById(i_id);
        if (assignment == null || assignment.parentid != i_caller.UserId) throw new LogicException("Không tìm thấy bài giao.", 404);
        return assignment;
    }

    private AssignmentReadModel ToReadModel(AssignmentModel i_assignment, List<AssignmentStudentRow> i_lstRow)
    {
        AssignmentReadModel read = new AssignmentReadModel();
        read.Id = i_assignment.id;
        read.Title = i_assignment.title;
        read.Grade = i_assignment.grade;
        read.Subject = i_assignment.subject;
        read.TopicIds = i_assignment.topicids.ToList();
        read.Level = i_assignment.level;
        read.QuestionCount = i_assignment.questioncount;
        read.TimeLimitMin = i_assignment.timelimitmin;
        read.Seed = i_assignment.seed;
        read.DueDate = i_assignment.duedate;
        read.Note = i_assignment.note;
        read.Status = i_assignment.status;
        read.HasQuestions = i_assignment.questions != null;
        read.SourceId = i_assignment.sourceid;
        read.TimeCr = i_assignment.timecr;
        read.TopicKey = AssignmentRules.TopicKey(i_assignment.grade, i_assignment.subject, i_assignment.id);
        read.Questions = string.IsNullOrEmpty(i_assignment.questions) ? null : JsonDocument.Parse(i_assignment.questions).RootElement.Clone();
        foreach (AssignmentStudentRow item in i_lstRow)
        {
            read.Students.Add(ToStudentReadModel(item.Row, item.NickName, item.Avatar, i_assignment.duedate));
        }
        return read;
    }

    private AssignmentStudentReadModel ToStudentReadModel(AssignmentStudentModel i_row, string i_nickName, string i_avatar, DateTime i_dueDate)
    {
        AssignmentStudentReadModel read = new AssignmentStudentReadModel();
        read.StudentId = i_row.studentid;
        read.NickName = i_nickName;
        read.Avatar = i_avatar;
        read.Status = AssignmentRules.EffectiveStatus(i_row.status, i_dueDate, clock.UtcNow);
        read.BestScore = i_row.bestscore;
        read.BestAttemptId = i_row.bestattemptid;
        read.AttemptCount = i_row.attemptcount;
        read.SubmittedAt = i_row.submittedat;
        read.LastAttemptAt = i_row.lastattemptat;
        read.IsLate = i_row.islate;
        return read;
    }

    private static void ValidateTimeLimit(int? i_minutes)
    {
        if (i_minutes.HasValue && (i_minutes < 1 || i_minutes > HocTapConst.ASG_MAX_MINUTES))
        {
            throw new LogicException($"Thời gian làm bài từ 1 đến {HocTapConst.ASG_MAX_MINUTES} phút.");
        }
    }

    private static string CleanTitle(string? i_title)
    {
        string title = (i_title ?? "").Trim();
        if (title.Length == 0) title = "Bài tập";
        return title.Length > 100 ? title[..100] : title;
    }

    private static string? CleanNote(string? i_note)
    {
        string note = (i_note ?? "").Trim();
        if (note.Length == 0) return null;
        return note.Length > 500 ? note[..500] : note;
    }

    private static DateTime ToUtc(DateTime i_date)
    {
        if (i_date.Kind == DateTimeKind.Utc) return i_date;
        if (i_date.Kind == DateTimeKind.Local) return i_date.ToUniversalTime();
        return DateTime.SpecifyKind(i_date, DateTimeKind.Utc);
    }
}
