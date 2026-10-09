using HocTap.Domain.Aggregates.Attempts;
using HocTap.Domain.Commons;
using HocTap.Infrastructure.Entities;
using HocTap.Infrastructure.MappingRepos;
using HocTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HocTap.Infrastructure.Repositories;

/// <summary>
/// 08/10/2026 - Repository lượt học. Danh sách tiến độ chỉ chiếu các cột cần (không đọc jsonb câu hỏi),
/// dùng chỉ mục attempt(studentid, finishedat).
/// </summary>
public class AttemptRepository : IAttemptRepository
{
    private readonly HocTapDbContext dbContext;

    public AttemptRepository(HocTapDbContext i_context)
    {
        dbContext = i_context;
    }

    public void InsertAttempt(AttemptModel i_attempt)
    {
        dbContext.attempt.Add(HocTapEntityMapper.ToEntity(i_attempt));
    }

    public void InsertListAttempt(List<AttemptModel> i_lstAttempt)
    {
        List<attempt> lstEntity = new List<attempt>();
        foreach (AttemptModel model in i_lstAttempt)
        {
            lstEntity.Add(HocTapEntityMapper.ToEntity(model));
        }
        dbContext.attempt.AddRange(lstEntity);
    }

    public List<AttemptReadModel> GetListAttemptByStudent(Guid i_studentId, int i_max)
    {
        var lstRow = dbContext.attempt.AsNoTracking()
            .Where(x => x.studentid == i_studentId)
            .OrderByDescending(x => x.finishedat)
            .Take(i_max)
            .Select(x => new
            {
                x.id, x.studentid, x.assignmentid, x.grade, x.subject, x.topicid, x.mode, x.level,
                x.total, x.correct, x.score10, x.durationsec, x.finishedat,
                hasitems = dbContext.attempt_item.Any(i => i.attemptid == x.id),
            })
            .ToList();

        List<AttemptReadModel> lstResult = new List<AttemptReadModel>();
        foreach (var row in lstRow)
        {
            AttemptReadModel item = new AttemptReadModel();
            item.Id = row.id;
            item.StudentId = row.studentid;
            item.AssignmentId = row.assignmentid;
            item.Grade = row.grade;
            item.Subject = row.subject;
            item.TopicId = row.topicid;
            item.Mode = row.mode;
            item.Level = row.level;
            item.Total = row.total;
            item.Correct = row.correct;
            item.Score10 = row.score10;
            item.DurationSec = row.durationsec;
            item.FinishedAt = AttemptRules.ToUnixMs(row.finishedat);
            item.HasItems = row.hasitems;
            lstResult.Add(item);
        }
        return lstResult;
    }

    public AttemptModel? GetAttemptWithItems(Guid i_id)
    {
        attempt? entity = dbContext.attempt.AsNoTracking().Include(x => x.items).FirstOrDefault(x => x.id == i_id);
        return entity == null ? null : HocTapEntityMapper.ToModel(entity);
    }

    public HashSet<string> GetAttemptKeys(Guid i_studentId)
    {
        var lstRow = dbContext.attempt.AsNoTracking()
            .Where(x => x.studentid == i_studentId)
            .Select(x => new { x.topicid, x.mode, x.finishedat })
            .ToList();
        HashSet<string> setResult = new HashSet<string>();
        foreach (var row in lstRow)
        {
            setResult.Add(AttemptRules.Key(row.topicid, row.mode, row.finishedat));
        }
        return setResult;
    }
}
