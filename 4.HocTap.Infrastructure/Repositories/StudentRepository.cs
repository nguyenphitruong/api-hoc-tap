using HocTap.Domain.Aggregates.Students;
using HocTap.Infrastructure.Entities;
using HocTap.Infrastructure.MappingRepos;
using HocTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HocTap.Infrastructure.Repositories;

/// <summary>08/10/2026 - Repository hồ sơ con (student_profile)</summary>
public class StudentRepository : IStudentRepository
{
    private readonly HocTapDbContext dbContext;

    public StudentRepository(HocTapDbContext i_context)
    {
        dbContext = i_context;
    }

    public List<StudentProfileModel> GetListStudentByUser(Guid i_userId)
    {
        List<student_profile> lstEntity = dbContext.student_profile.AsNoTracking()
            .Where(x => x.userid == i_userId && x.active)
            .OrderBy(x => x.timecr)
            .ToList();
        List<StudentProfileModel> lstResult = new List<StudentProfileModel>();
        foreach (student_profile entity in lstEntity)
        {
            lstResult.Add(HocTapEntityMapper.ToModel(entity));
        }
        return lstResult;
    }

    public StudentProfileModel? GetStudentById(Guid i_id)
    {
        student_profile? entity = dbContext.student_profile.AsNoTracking().FirstOrDefault(x => x.id == i_id);
        return entity == null ? null : HocTapEntityMapper.ToModel(entity);
    }

    public int CountActiveStudent(Guid i_userId)
    {
        return dbContext.student_profile.Count(x => x.userid == i_userId && x.active);
    }

    public void InsertStudent(StudentProfileModel i_student)
    {
        dbContext.student_profile.Add(HocTapEntityMapper.ToEntity(i_student));
    }

    public void UpdateStudent(StudentProfileModel i_student)
    {
        dbContext.student_profile.Update(HocTapEntityMapper.ToEntity(i_student));
    }
}
