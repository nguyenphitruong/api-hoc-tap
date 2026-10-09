using HocTap.Domain.Aggregates.Admin;
using HocTap.Domain.Aggregates.Assignments;
using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Aggregates.Auth;
using HocTap.Domain.Aggregates.Students;
using HocTap.Infrastructure.Entities;

namespace HocTap.Infrastructure.MappingRepos;

/// <summary>
/// 08/10/2026 - Chuyển đổi entity (bảng CSDL) <-> Model (tầng Domain) — viết tường minh từng cột, không dùng AutoMapper.
/// Repository đọc entity -> ToModel trả cho Service; nhận Model từ Service -> ToEntity để thêm / sửa.
/// Thêm cột mới vào bảng: thêm thuộc tính ở entity + Model và thêm 1 dòng gán ở 2 hàm tương ứng.
/// </summary>
public static class HocTapEntityMapper
{
    /// <summary>app_user -> AppUserModel (gán từng cột)</summary>
    public static AppUserModel ToModel(app_user i_source)
    {
        AppUserModel _Result = new AppUserModel();
        _Result.id = i_source.id;
        _Result.email = i_source.email;
        _Result.phone = i_source.phone;
        _Result.passwordhash = i_source.passwordhash;
        _Result.fullname = i_source.fullname;
        _Result.role = i_source.role;
        _Result.status = i_source.status;
        _Result.trialend = i_source.trialend;
        _Result.planend = i_source.planend;
        _Result.lastlogin = i_source.lastlogin;
        _Result.timecr = i_source.timecr;
        _Result.timeup = i_source.timeup;
        return _Result;
    }

    /// <summary>AppUserModel -> app_user (gán từng cột)</summary>
    public static app_user ToEntity(AppUserModel i_source)
    {
        app_user _Result = new app_user();
        _Result.id = i_source.id;
        _Result.email = i_source.email;
        _Result.phone = i_source.phone;
        _Result.passwordhash = i_source.passwordhash;
        _Result.fullname = i_source.fullname;
        _Result.role = i_source.role;
        _Result.status = i_source.status;
        _Result.trialend = i_source.trialend;
        _Result.planend = i_source.planend;
        _Result.lastlogin = i_source.lastlogin;
        _Result.timecr = i_source.timecr;
        _Result.timeup = i_source.timeup;
        return _Result;
    }

    /// <summary>refresh_token -> RefreshTokenModel (gán từng cột)</summary>
    public static RefreshTokenModel ToModel(refresh_token i_source)
    {
        RefreshTokenModel _Result = new RefreshTokenModel();
        _Result.id = i_source.id;
        _Result.userid = i_source.userid;
        _Result.studentid = i_source.studentid;
        _Result.tokenhash = i_source.tokenhash;
        _Result.expires = i_source.expires;
        _Result.revoked = i_source.revoked;
        _Result.device = i_source.device;
        _Result.timecr = i_source.timecr;
        return _Result;
    }

    /// <summary>RefreshTokenModel -> refresh_token (gán từng cột)</summary>
    public static refresh_token ToEntity(RefreshTokenModel i_source)
    {
        refresh_token _Result = new refresh_token();
        _Result.id = i_source.id;
        _Result.userid = i_source.userid;
        _Result.studentid = i_source.studentid;
        _Result.tokenhash = i_source.tokenhash;
        _Result.expires = i_source.expires;
        _Result.revoked = i_source.revoked;
        _Result.device = i_source.device;
        _Result.timecr = i_source.timecr;
        return _Result;
    }

    /// <summary>student_profile -> StudentProfileModel (gán từng cột)</summary>
    public static StudentProfileModel ToModel(student_profile i_source)
    {
        StudentProfileModel _Result = new StudentProfileModel();
        _Result.id = i_source.id;
        _Result.userid = i_source.userid;
        _Result.nickname = i_source.nickname;
        _Result.grade = i_source.grade;
        _Result.avatar = i_source.avatar;
        _Result.pinhash = i_source.pinhash;
        _Result.active = i_source.active;
        _Result.timecr = i_source.timecr;
        _Result.timeup = i_source.timeup;
        return _Result;
    }

    /// <summary>StudentProfileModel -> student_profile (gán từng cột)</summary>
    public static student_profile ToEntity(StudentProfileModel i_source)
    {
        student_profile _Result = new student_profile();
        _Result.id = i_source.id;
        _Result.userid = i_source.userid;
        _Result.nickname = i_source.nickname;
        _Result.grade = i_source.grade;
        _Result.avatar = i_source.avatar;
        _Result.pinhash = i_source.pinhash;
        _Result.active = i_source.active;
        _Result.timecr = i_source.timecr;
        _Result.timeup = i_source.timeup;
        return _Result;
    }

    /// <summary>attempt -> AttemptModel (gán từng cột)</summary>
    public static AttemptModel ToModel(attempt i_source)
    {
        AttemptModel _Result = new AttemptModel();
        _Result.id = i_source.id;
        _Result.studentid = i_source.studentid;
        _Result.assignmentid = i_source.assignmentid;
        _Result.grade = i_source.grade;
        _Result.subject = i_source.subject;
        _Result.topicid = i_source.topicid;
        _Result.mode = i_source.mode;
        _Result.level = i_source.level;
        _Result.seed = i_source.seed;
        _Result.total = i_source.total;
        _Result.correct = i_source.correct;
        _Result.score10 = i_source.score10;
        _Result.durationsec = i_source.durationsec;
        _Result.startedat = i_source.startedat;
        _Result.finishedat = i_source.finishedat;
        _Result.source = i_source.source;
        _Result.timecr = i_source.timecr;
        _Result.items = new List<AttemptLineModel>();
        foreach (var item in i_source.items)
        {
            _Result.items.Add(ToModel(item));
        }
        return _Result;
    }

    /// <summary>AttemptModel -> attempt (gán từng cột)</summary>
    public static attempt ToEntity(AttemptModel i_source)
    {
        attempt _Result = new attempt();
        _Result.id = i_source.id;
        _Result.studentid = i_source.studentid;
        _Result.assignmentid = i_source.assignmentid;
        _Result.grade = i_source.grade;
        _Result.subject = i_source.subject;
        _Result.topicid = i_source.topicid;
        _Result.mode = i_source.mode;
        _Result.level = i_source.level;
        _Result.seed = i_source.seed;
        _Result.total = i_source.total;
        _Result.correct = i_source.correct;
        _Result.score10 = i_source.score10;
        _Result.durationsec = i_source.durationsec;
        _Result.startedat = i_source.startedat;
        _Result.finishedat = i_source.finishedat;
        _Result.source = i_source.source;
        _Result.timecr = i_source.timecr;
        _Result.items = new List<attempt_item>();
        foreach (var item in i_source.items)
        {
            _Result.items.Add(ToEntity(item));
        }
        return _Result;
    }

    /// <summary>attempt_item -> AttemptLineModel (gán từng cột)</summary>
    public static AttemptLineModel ToModel(attempt_item i_source)
    {
        AttemptLineModel _Result = new AttemptLineModel();
        _Result.id = i_source.id;
        _Result.attemptid = i_source.attemptid;
        _Result.idx = i_source.idx;
        _Result.question = i_source.question;
        _Result.answer = i_source.answer;
        _Result.iscorrect = i_source.iscorrect;
        return _Result;
    }

    /// <summary>AttemptLineModel -> attempt_item (gán từng cột)</summary>
    public static attempt_item ToEntity(AttemptLineModel i_source)
    {
        attempt_item _Result = new attempt_item();
        _Result.id = i_source.id;
        _Result.attemptid = i_source.attemptid;
        _Result.idx = i_source.idx;
        _Result.question = i_source.question;
        _Result.answer = i_source.answer;
        _Result.iscorrect = i_source.iscorrect;
        return _Result;
    }

    /// <summary>assignment -> AssignmentModel (gán từng cột)</summary>
    public static AssignmentModel ToModel(assignment i_source)
    {
        AssignmentModel _Result = new AssignmentModel();
        _Result.id = i_source.id;
        _Result.parentid = i_source.parentid;
        _Result.title = i_source.title;
        _Result.grade = i_source.grade;
        _Result.subject = i_source.subject;
        _Result.topicids = i_source.topicids;
        _Result.level = i_source.level;
        _Result.questioncount = i_source.questioncount;
        _Result.timelimitmin = i_source.timelimitmin;
        _Result.seed = i_source.seed;
        _Result.duedate = i_source.duedate;
        _Result.note = i_source.note;
        _Result.status = i_source.status;
        _Result.questions = i_source.questions;
        _Result.sourceid = i_source.sourceid;
        _Result.timecr = i_source.timecr;
        _Result.timeup = i_source.timeup;
        return _Result;
    }

    /// <summary>AssignmentModel -> assignment (gán từng cột)</summary>
    public static assignment ToEntity(AssignmentModel i_source)
    {
        assignment _Result = new assignment();
        _Result.id = i_source.id;
        _Result.parentid = i_source.parentid;
        _Result.title = i_source.title;
        _Result.grade = i_source.grade;
        _Result.subject = i_source.subject;
        _Result.topicids = i_source.topicids;
        _Result.level = i_source.level;
        _Result.questioncount = i_source.questioncount;
        _Result.timelimitmin = i_source.timelimitmin;
        _Result.seed = i_source.seed;
        _Result.duedate = i_source.duedate;
        _Result.note = i_source.note;
        _Result.status = i_source.status;
        _Result.questions = i_source.questions;
        _Result.sourceid = i_source.sourceid;
        _Result.timecr = i_source.timecr;
        _Result.timeup = i_source.timeup;
        return _Result;
    }

    /// <summary>assignment_student -> AssignmentStudentModel (gán từng cột)</summary>
    public static AssignmentStudentModel ToModel(assignment_student i_source)
    {
        AssignmentStudentModel _Result = new AssignmentStudentModel();
        _Result.assignmentid = i_source.assignmentid;
        _Result.studentid = i_source.studentid;
        _Result.status = i_source.status;
        _Result.bestscore = i_source.bestscore;
        _Result.bestattemptid = i_source.bestattemptid;
        _Result.attemptcount = i_source.attemptcount;
        _Result.submittedat = i_source.submittedat;
        _Result.lastattemptat = i_source.lastattemptat;
        _Result.islate = i_source.islate;
        return _Result;
    }

    /// <summary>AssignmentStudentModel -> assignment_student (gán từng cột)</summary>
    public static assignment_student ToEntity(AssignmentStudentModel i_source)
    {
        assignment_student _Result = new assignment_student();
        _Result.assignmentid = i_source.assignmentid;
        _Result.studentid = i_source.studentid;
        _Result.status = i_source.status;
        _Result.bestscore = i_source.bestscore;
        _Result.bestattemptid = i_source.bestattemptid;
        _Result.attemptcount = i_source.attemptcount;
        _Result.submittedat = i_source.submittedat;
        _Result.lastattemptat = i_source.lastattemptat;
        _Result.islate = i_source.islate;
        return _Result;
    }

    /// <summary>plan_log -> PlanLogModel (gán từng cột)</summary>
    public static PlanLogModel ToModel(plan_log i_source)
    {
        PlanLogModel _Result = new PlanLogModel();
        _Result.id = i_source.id;
        _Result.userid = i_source.userid;
        _Result.action = i_source.action;
        _Result.fromdate = i_source.fromdate;
        _Result.todate = i_source.todate;
        _Result.note = i_source.note;
        _Result.adminid = i_source.adminid;
        _Result.timecr = i_source.timecr;
        return _Result;
    }

    /// <summary>PlanLogModel -> plan_log (gán từng cột)</summary>
    public static plan_log ToEntity(PlanLogModel i_source)
    {
        plan_log _Result = new plan_log();
        _Result.id = i_source.id;
        _Result.userid = i_source.userid;
        _Result.action = i_source.action;
        _Result.fromdate = i_source.fromdate;
        _Result.todate = i_source.todate;
        _Result.note = i_source.note;
        _Result.adminid = i_source.adminid;
        _Result.timecr = i_source.timecr;
        return _Result;
    }
}
