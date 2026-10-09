using HocTap.Domain.Aggregates.Assignments;
using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Commons;
using HocTap.Infrastructure.Entities;
using HocTap.Infrastructure.MappingRepos;
using HocTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HocTap.Infrastructure.Repositories;

/// <summary>
/// 08/10/2026 - Repository bài giao (giai đoạn 4). Danh sách không đọc nội dung câu (jsonb questions):
/// questions trả "" nếu bài có lưu sẵn câu, null nếu không (Service chỉ cần biết có / không).
/// </summary>
public class AssignmentRepository : IAssignmentRepository
{
    private readonly HocTapDbContext dbContext;

    public AssignmentRepository(HocTapDbContext i_context)
    {
        dbContext = i_context;
    }

    public void InsertAssignment(AssignmentModel i_assignment, List<AssignmentStudentModel> i_lstStudent)
    {
        dbContext.assignment.Add(HocTapEntityMapper.ToEntity(i_assignment));
        foreach (AssignmentStudentModel row in i_lstStudent)
        {
            dbContext.assignment_student.Add(HocTapEntityMapper.ToEntity(row));
        }
    }

    public void UpdateAssignment(AssignmentModel i_assignment)
    {
        dbContext.assignment.Update(HocTapEntityMapper.ToEntity(i_assignment));
    }

    public AssignmentModel? GetAssignmentById(Guid i_id)
    {
        assignment? entity = dbContext.assignment.AsNoTracking().FirstOrDefault(x => x.id == i_id);
        return entity == null ? null : HocTapEntityMapper.ToModel(entity);
    }

    public List<AssignmentModel> GetListAssignmentByParent(Guid i_parentId, int i_max)
    {
        List<assignment> lstEntity = dbContext.assignment.AsNoTracking()
            .Where(x => x.parentid == i_parentId)
            .OrderByDescending(x => x.timecr)
            .Take(i_max)
            .Select(x => new assignment
            {
                id = x.id, parentid = x.parentid, title = x.title, grade = x.grade, subject = x.subject, topicids = x.topicids,
                level = x.level, questioncount = x.questioncount, timelimitmin = x.timelimitmin, seed = x.seed, duedate = x.duedate,
                note = x.note, status = x.status, questions = x.questions == null ? null : "", sourceid = x.sourceid, timecr = x.timecr,
            })
            .ToList();
        List<AssignmentModel> lstResult = new List<AssignmentModel>();
        foreach (assignment entity in lstEntity)
        {
            lstResult.Add(HocTapEntityMapper.ToModel(entity));
        }
        return lstResult;
    }

    public List<AssignmentStudentRow> GetListAssignmentStudentRow(List<Guid> i_lstAssignmentId)
    {
        var lstRow = (from r in dbContext.assignment_student.AsNoTracking()
                      join s in dbContext.student_profile.AsNoTracking() on r.studentid equals s.id
                      where i_lstAssignmentId.Contains(r.assignmentid)
                      orderby s.timecr
                      select new { r, s.nickname, s.avatar })
                     .ToList();
        List<AssignmentStudentRow> lstResult = new List<AssignmentStudentRow>();
        foreach (var row in lstRow)
        {
            AssignmentStudentRow item = new AssignmentStudentRow();
            item.Row = HocTapEntityMapper.ToModel(row.r);
            item.NickName = row.nickname;
            item.Avatar = row.avatar;
            lstResult.Add(item);
        }
        return lstResult;
    }

    public AssignmentStudentModel? GetAssignmentStudent(Guid i_assignmentId, Guid i_studentId)
    {
        assignment_student? entity = dbContext.assignment_student.AsNoTracking()
            .FirstOrDefault(x => x.assignmentid == i_assignmentId && x.studentid == i_studentId);
        return entity == null ? null : HocTapEntityMapper.ToModel(entity);
    }

    public void UpdateAssignmentStudent(AssignmentStudentModel i_row)
    {
        dbContext.assignment_student.Update(HocTapEntityMapper.ToEntity(i_row));
    }

    public List<AssignmentOfStudentRow> GetListOpenAssignmentOfStudent(Guid i_studentId)
    {
        var lstRow = (from r in dbContext.assignment_student.AsNoTracking()
                      join a in dbContext.assignment.AsNoTracking() on r.assignmentid equals a.id
                      where r.studentid == i_studentId && a.status == HocTapConst.ASG_OPEN
                      orderby a.duedate
                      select new
                      {
                          r,
                          a = new assignment
                          {
                              id = a.id, parentid = a.parentid, title = a.title, grade = a.grade, subject = a.subject, topicids = a.topicids,
                              level = a.level, questioncount = a.questioncount, timelimitmin = a.timelimitmin, seed = a.seed, duedate = a.duedate,
                              note = a.note, status = a.status, questions = a.questions == null ? null : "", sourceid = a.sourceid, timecr = a.timecr,
                          },
                      })
                     .ToList();
        List<AssignmentOfStudentRow> lstResult = new List<AssignmentOfStudentRow>();
        foreach (var row in lstRow)
        {
            AssignmentOfStudentRow item = new AssignmentOfStudentRow();
            item.Assignment = HocTapEntityMapper.ToModel(row.a);
            item.Row = HocTapEntityMapper.ToModel(row.r);
            lstResult.Add(item);
        }
        return lstResult;
    }

    public List<AttemptModel> GetListAttemptOfAssignment(Guid i_assignmentId)
    {
        List<attempt> lstEntity = dbContext.attempt.AsNoTracking()
            .Where(x => x.assignmentid == i_assignmentId)
            .OrderByDescending(x => x.finishedat)
            .ToList();
        List<AttemptModel> lstResult = new List<AttemptModel>();
        foreach (attempt entity in lstEntity)
        {
            lstResult.Add(HocTapEntityMapper.ToModel(entity));
        }
        return lstResult;
    }

    public AttemptModel? GetLatestAttemptOfStudent(Guid i_assignmentId, Guid i_studentId)
    {
        attempt? entity = dbContext.attempt.AsNoTracking().Include(x => x.items)
            .Where(x => x.assignmentid == i_assignmentId && x.studentid == i_studentId)
            .OrderByDescending(x => x.finishedat)
            .FirstOrDefault();
        return entity == null ? null : HocTapEntityMapper.ToModel(entity);
    }
}
