using System.Text.Json;
using HocTap.Application.Commons;
using HocTap.Domain.Aggregates.Assignments;
using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Students;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;

namespace HocTap.Application.Attempts;

/// <summary>
/// 08/10/2026 - LƯỢT HỌC (viết tường minh theo mẫu Web-MultiClinic)
/// - Nộp lượt: chỉ phiên học sinh; tài khoản phải còn hạn (DUNGTHU / HOATDONG); bài phải đúng lớp của hồ sơ.
///   Lượt làm bài được giao: kiểm tra bài + cập nhật dòng assignment_student trong cùng 1 lần Save.
/// - Tiến độ: trả danh sách lượt (tối đa 5000, mới nhất trước) — client tự tính sao, %, chủ đề cần ôn.
/// - Xem lại: kèm từng câu (question / answer jsonb).
/// - Nhập: phụ huynh đưa lịch sử cũ vào 1 hồ sơ; bỏ qua dòng sai, khác lớp hoặc đã có (topicid|mode|finishedat).
/// </summary>
public class AttemptServices : IAttemptServices
{
    private static readonly DateTime MIN_TIME = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly string[] MODES = { HocTapConst.MODE_PRACTICE, HocTapConst.MODE_TEST };

    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public AttemptServices(IUnitOfWork i_UnitOfWork, IClock i_Clock)
    {
        unitOfWork = i_UnitOfWork;
        clock = i_Clock;
    }

    public AttemptReadModel InsertAttempt(CurrentUser i_caller, AttemptSaveModel i_model)
    {
        AttemptReadModel _Result = new AttemptReadModel();

        // 1. Phiên học sinh + hồ sơ hợp lệ
        if (i_caller.Role != HocTapConst.ROLE_STUDENT || !i_caller.StudentId.HasValue)
        {
            throw new LogicException("Hãy chọn hồ sơ học sinh trước khi làm bài.", 403);
        }
        StudentProfileModel student = ServiceGuard.GetOwnedStudent(unitOfWork, i_caller, i_caller.StudentId.Value);

        // 2. Tài khoản còn hạn
        AppUserModel? user = unitOfWork.UserRepo.GetUserById(i_caller.UserId);
        if (user == null) throw new LogicException("Không tìm thấy tài khoản.", 401);
        string status = AccountRules.EffectiveStatus(user.status, user.trialend, user.planend, clock.UtcNow);
        if (!AccountRules.CanStudy(status))
        {
            throw new LogicException("Tài khoản đã hết hạn sử dụng. Phụ huynh vui lòng gia hạn để tiếp tục lưu bài.", 403);
        }

        // 3. Dựng lượt học + kiểm tra bài được giao (nếu có)
        AttemptModel attempt = BuildAttempt(i_model, student, HocTapConst.SOURCE_APP, true);
        AssignmentModel? assignment = null;
        AssignmentStudentModel? assignmentStudent = null;
        if (i_model.AssignmentId.HasValue)
        {
            assignment = CheckAssignment(i_model.AssignmentId.Value, student, attempt);
            assignmentStudent = unitOfWork.AssignmentRepo.GetAssignmentStudent(assignment.id, student.id);
            if (assignmentStudent == null) throw new LogicException("Bài này không giao cho con.", 403);
            AssignmentRules.ApplySubmission(assignmentStudent, attempt.id, attempt.score10 ?? 0, attempt.finishedat, assignment.duedate);
        }

        // 4. Lưu
        try
        {
            unitOfWork.InitTransaction();
            unitOfWork.AttemptRepo.InsertAttempt(attempt);
            if (assignmentStudent != null)
            {
                unitOfWork.AssignmentRepo.UpdateAssignmentStudent(assignmentStudent);
            }
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }

        FillReadModel(_Result, attempt);
        _Result.HasItems = attempt.items.Count > 0;
        return _Result;
    }

    public List<AttemptReadModel> GetListProgress(CurrentUser i_caller, Guid i_studentId)
    {
        ServiceGuard.GetOwnedStudent(unitOfWork, i_caller, i_studentId);
        List<AttemptReadModel> lstResult = unitOfWork.AttemptRepo.GetListAttemptByStudent(i_studentId, HocTapConst.MAX_IMPORT);
        return lstResult;
    }

    public AttemptDetailReadModel GetAttemptDetail(CurrentUser i_caller, Guid i_id)
    {
        AttemptDetailReadModel _Result = new AttemptDetailReadModel();

        AttemptModel? attempt = unitOfWork.AttemptRepo.GetAttemptWithItems(i_id);
        if (attempt == null) throw new LogicException("Không tìm thấy lượt học.", 404);
        ServiceGuard.GetOwnedStudent(unitOfWork, i_caller, attempt.studentid);

        FillReadModel(_Result, attempt);
        _Result.Seed = attempt.seed;
        _Result.HasItems = attempt.items.Count > 0;
        foreach (AttemptLineModel line in attempt.items.OrderBy(x => x.idx))
        {
            AttemptItemModel item = new AttemptItemModel();
            item.Idx = line.idx;
            item.Question = JsonDocument.Parse(line.question).RootElement.Clone();
            item.Answer = line.answer == null ? null : JsonDocument.Parse(line.answer).RootElement.Clone();
            item.IsCorrect = line.iscorrect;
            _Result.Items.Add(item);
        }
        return _Result;
    }

    public ImportResultReadModel ImportAttempt(CurrentUser i_caller, Guid i_studentId, ImportModel i_model)
    {
        ImportResultReadModel _Result = new ImportResultReadModel();

        // 1. Kiểm tra quyền + số lượng
        ServiceGuard.EnsureParent(i_caller);
        StudentProfileModel student = ServiceGuard.GetOwnedStudent(unitOfWork, i_caller, i_studentId);
        List<AttemptSaveModel> lstInput = i_model.Attempts ?? new List<AttemptSaveModel>();
        if (lstInput.Count == 0) throw new LogicException("Không có lượt học nào để nhập.");
        if (lstInput.Count > HocTapConst.MAX_IMPORT) throw new LogicException($"Mỗi lần nhập tối đa {HocTapConst.MAX_IMPORT} lượt.");

        // 2. Dựng từng dòng: bỏ qua dòng sai hoặc trùng khoá
        HashSet<string> keys = unitOfWork.AttemptRepo.GetAttemptKeys(i_studentId);
        List<AttemptModel> lstAttempt = new List<AttemptModel>();
        int skipped = 0;
        foreach (AttemptSaveModel input in lstInput)
        {
            AttemptModel attempt;
            try
            {
                attempt = BuildAttempt(input, student, HocTapConst.SOURCE_IMPORT, false);
            }
            catch (LogicException)
            {
                skipped++;
                continue;
            }
            if (!keys.Add(AttemptRules.Key(attempt.topicid, attempt.mode, attempt.finishedat)))
            {
                skipped++;
                continue;
            }
            lstAttempt.Add(attempt);
        }

        _Result.Added = lstAttempt.Count;
        _Result.Skipped = skipped;
        if (lstAttempt.Count == 0) return _Result;

        // 3. Lưu
        try
        {
            unitOfWork.InitTransaction();
            unitOfWork.AttemptRepo.InsertListAttempt(lstAttempt);
            unitOfWork.Save();
            unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            unitOfWork.RollbackTransaction();
            throw;
        }
        return _Result;
    }

    // ===== Nội bộ =====

    /// <summary>
    /// Kiểm tra + dựng lượt: mã chủ đề hợp lệ, cùng lớp với hồ sơ, môn khớp mã, hình thức LUYENTAP/KIEMTRA,
    /// 0 ≤ đúng ≤ tổng ≤ 1000, điểm 0–10 (kiểm tra thiếu điểm thì tính theo số câu đúng), thời điểm hợp lý, câu hỏi giới hạn kích thước.
    /// Mã "as:" chỉ dùng cho lượt làm bài được giao (có AssignmentId), không nhập từ file.
    /// </summary>
    private AttemptModel BuildAttempt(AttemptSaveModel i_model, StudentProfileModel i_student, string i_source, bool i_withItems)
    {
        var parsed = AttemptRules.ParseTopic(i_model.TopicId);
        if (parsed == null) throw new LogicException("Mã chủ đề không hợp lệ.");
        short topicGrade = parsed.Value.Grade;
        string topicSubject = parsed.Value.Subject;
        if (parsed.Value.Kind == HocTapConst.TOPIC_ASSIGN && (!i_withItems || !i_model.AssignmentId.HasValue)) throw new LogicException("Mã bài giao không hợp lệ.");
        if (topicGrade != i_student.grade) throw new LogicException($"Bài này thuộc lớp {topicGrade}, hồ sơ của con là lớp {i_student.grade}.");
        if (i_model.Grade != 0 && i_model.Grade != topicGrade) throw new LogicException("Lớp không khớp mã chủ đề.");
        if (!string.IsNullOrEmpty(i_model.Subject) && i_model.Subject != topicSubject) throw new LogicException("Môn không khớp mã chủ đề.");
        if (!MODES.Contains(i_model.Mode)) throw new LogicException("Hình thức học không hợp lệ.");
        if (i_model.Level.HasValue && (i_model.Level < 1 || i_model.Level > 3)) throw new LogicException("Mức độ không hợp lệ.");
        if (i_model.Total < 0 || i_model.Total > 1000 || i_model.Correct < 0 || i_model.Correct > i_model.Total) throw new LogicException("Số câu không hợp lệ.");
        if (i_model.Score10.HasValue && (i_model.Score10 < 0 || i_model.Score10 > 10)) throw new LogicException("Điểm không hợp lệ.");
        if (i_model.DurationSec < 0 || i_model.DurationSec > 86400) throw new LogicException("Thời gian làm bài không hợp lệ.");

        DateTime now = clock.UtcNow;
        DateTime finished = AttemptRules.FromUnixMs(i_model.FinishedAt);
        if (finished < MIN_TIME || finished > now.AddDays(1)) throw new LogicException("Thời điểm làm bài không hợp lệ.");
        DateTime? started = i_model.StartedAt.HasValue ? AttemptRules.FromUnixMs(i_model.StartedAt.Value) : null;
        if (started.HasValue && (started > finished || started < MIN_TIME)) started = null;

        decimal? score = i_model.Score10;
        if (i_model.Mode == HocTapConst.MODE_TEST && score == null)
        {
            score = i_model.Total > 0 ? Math.Round(10m * i_model.Correct / i_model.Total, 2) : 0;
        }
        if (i_model.Mode == HocTapConst.MODE_PRACTICE) score = null;

        AttemptModel attempt = new AttemptModel();
        attempt.id = Guid.NewGuid();
        attempt.studentid = i_student.id;
        attempt.assignmentid = null;
        attempt.grade = topicGrade;
        attempt.subject = topicSubject;
        attempt.topicid = i_model.TopicId!;
        attempt.mode = i_model.Mode!;
        attempt.level = i_model.Level;
        attempt.seed = i_model.Seed;
        attempt.total = i_model.Total;
        attempt.correct = i_model.Correct;
        attempt.score10 = score.HasValue ? Math.Round(score.Value, 2) : null;
        attempt.durationsec = i_model.DurationSec;
        attempt.startedat = started;
        attempt.finishedat = finished;
        attempt.source = i_source;
        attempt.timecr = now;

        if (i_withItems && i_model.Items != null && i_model.Items.Count > 0)
        {
            if (i_model.Items.Count > HocTapConst.MAX_ITEMS) throw new LogicException("Số câu gửi kèm quá nhiều.");
            foreach (AttemptItemModel item in i_model.Items)
            {
                if (item.Question.ValueKind != JsonValueKind.Object) throw new LogicException("Nội dung câu hỏi không hợp lệ.");
                string question = item.Question.GetRawText();
                string? answer = item.Answer.HasValue && item.Answer.Value.ValueKind != JsonValueKind.Undefined ? item.Answer.Value.GetRawText() : null;
                if (question.Length > HocTapConst.MAX_QUESTION_JSON || (answer?.Length ?? 0) > 2000) throw new LogicException("Nội dung câu hỏi quá dài.");

                AttemptLineModel line = new AttemptLineModel();
                line.id = Guid.NewGuid();
                line.attemptid = attempt.id;
                line.idx = item.Idx;
                line.question = question;
                line.answer = answer;
                line.iscorrect = item.IsCorrect;
                attempt.items.Add(line);
            }
        }
        return attempt;
    }

    /// <summary>
    /// Lượt làm bài được giao: bài tồn tại, cùng tài khoản, đang giao (chưa đóng), đúng mã as:, là bài kiểm tra, đủ số câu.
    /// Hợp lệ thì gán attempt.assignmentid.
    /// </summary>
    private AssignmentModel CheckAssignment(Guid i_assignmentId, StudentProfileModel i_student, AttemptModel i_attempt)
    {
        AssignmentModel? assignment = unitOfWork.AssignmentRepo.GetAssignmentById(i_assignmentId);
        if (assignment == null || assignment.parentid != i_student.userid) throw new LogicException("Không tìm thấy bài được giao.", 404);
        if (assignment.status != HocTapConst.ASG_OPEN) throw new LogicException("Bài này đã đóng, không nộp được nữa.");
        if (unitOfWork.AssignmentRepo.GetAssignmentStudent(assignment.id, i_student.id) == null) throw new LogicException("Bài này không giao cho con.", 403);
        if (i_attempt.topicid != AssignmentRules.TopicKey(assignment.grade, assignment.subject, assignment.id)) throw new LogicException("Mã bài giao không khớp.");
        if (i_attempt.mode != HocTapConst.MODE_TEST) throw new LogicException("Bài được giao phải nộp dạng bài kiểm tra.");
        if (i_attempt.total != assignment.questioncount) throw new LogicException("Số câu không khớp với bài được giao.");
        i_attempt.assignmentid = assignment.id;
        return assignment;
    }

    private static void FillReadModel(AttemptReadModel o_read, AttemptModel i_attempt)
    {
        o_read.Id = i_attempt.id;
        o_read.StudentId = i_attempt.studentid;
        o_read.AssignmentId = i_attempt.assignmentid;
        o_read.Grade = i_attempt.grade;
        o_read.Subject = i_attempt.subject;
        o_read.TopicId = i_attempt.topicid;
        o_read.Mode = i_attempt.mode;
        o_read.Level = i_attempt.level;
        o_read.Total = i_attempt.total;
        o_read.Correct = i_attempt.correct;
        o_read.Score10 = i_attempt.score10;
        o_read.DurationSec = i_attempt.durationsec;
        o_read.FinishedAt = AttemptRules.ToUnixMs(i_attempt.finishedat);
    }
}
