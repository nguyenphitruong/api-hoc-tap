using HocTap.Domain.Aggregates.Feedbacks;
using HocTap.Infrastructure.Entities;
using HocTap.Infrastructure.MappingRepos;
using HocTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HocTap.Infrastructure.Repositories;

/// <summary>
/// 18/10/2026 - Repository góp ý (feedback). Đọc AsNoTracking -> Model; ghi qua DbContext (Service gọi unitOfWork.Save()).
/// </summary>
public class FeedbackRepository : IFeedbackRepository
{
    private readonly HocTapDbContext dbContext;

    public FeedbackRepository(HocTapDbContext i_dbContext)
    {
        dbContext = i_dbContext;
    }

    public void InsertFeedback(FeedbackModel i_feedback)
    {
        dbContext.feedback.Add(HocTapEntityMapper.ToEntity(i_feedback));
    }

    public int CountFeedbackSince(Guid? i_userId, string? i_guestId, DateTime i_since)
    {
        IQueryable<feedback> q = dbContext.feedback.AsNoTracking().Where(x => x.timecr >= i_since);
        if (i_userId.HasValue) return q.Count(x => x.userid == i_userId);
        if (!string.IsNullOrEmpty(i_guestId)) return q.Count(x => x.guestid == i_guestId);
        return 0;
    }

    public List<FeedbackModel> GetListFeedback()
    {
        return dbContext.feedback.AsNoTracking().OrderByDescending(x => x.timecr).ToList().Select(HocTapEntityMapper.ToModel).ToList();
    }

    public FeedbackModel? GetFeedbackById(Guid i_id)
    {
        feedback? entity = dbContext.feedback.AsNoTracking().FirstOrDefault(x => x.id == i_id);
        return entity == null ? null : HocTapEntityMapper.ToModel(entity);
    }

    public void UpdateFeedback(FeedbackModel i_feedback)
    {
        feedback? entity = dbContext.feedback.Find(i_feedback.id);
        if (entity == null) return;
        entity.status = i_feedback.status;
        entity.adminnote = i_feedback.adminnote;
        entity.timeup = i_feedback.timeup;
        entity.userup = i_feedback.userup;
    }
}
