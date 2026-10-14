using HocTap.Domain.Aggregates.Settings;
using HocTap.Infrastructure.Entities;
using HocTap.Infrastructure.MappingRepos;
using HocTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HocTap.Infrastructure.Repositories;

/// <summary>
/// 14/10/2026 - Repository cài đặt hệ thống: bảng giá (plan_price) + tham số (sys_setting).
/// Đọc AsNoTracking -> Model; Upsert: có dòng thì sửa, chưa có thì thêm (Service gọi unitOfWork.Save()).
/// </summary>
public class SettingRepository : ISettingRepository
{
    private readonly HocTapDbContext dbContext;

    public SettingRepository(HocTapDbContext i_dbContext)
    {
        dbContext = i_dbContext;
    }

    public List<PlanPriceModel> GetListPlanPrice()
    {
        return dbContext.plan_price.AsNoTracking().OrderBy(x => x.sortorder).ThenBy(x => x.code).ToList()
            .Select(HocTapEntityMapper.ToModel).ToList();
    }

    public void UpsertPlanPrice(PlanPriceModel i_plan)
    {
        plan_price? entity = dbContext.plan_price.Find(i_plan.code);
        if (entity == null)
        {
            dbContext.plan_price.Add(new plan_price
            {
                code = i_plan.code, name = i_plan.name, months = i_plan.months, subjects = i_plan.subjects, price = i_plan.price,
                note = i_plan.note, sortorder = i_plan.sortorder, isactive = i_plan.isactive, timecr = i_plan.timecr,
                timeup = i_plan.timeup, userup = i_plan.userup,
            });
            return;
        }
        entity.name = i_plan.name;
        entity.months = i_plan.months;
        entity.subjects = i_plan.subjects;
        entity.price = i_plan.price;
        entity.note = i_plan.note;
        entity.sortorder = i_plan.sortorder;
        entity.isactive = i_plan.isactive;
        entity.timeup = i_plan.timeup;
        entity.userup = i_plan.userup;
    }

    public List<SysSettingModel> GetListSetting()
    {
        return dbContext.sys_setting.AsNoTracking().ToList().Select(HocTapEntityMapper.ToModel).ToList();
    }

    public void UpsertSetting(SysSettingModel i_setting)
    {
        sys_setting? entity = dbContext.sys_setting.Find(i_setting.key);
        if (entity == null)
        {
            dbContext.sys_setting.Add(new sys_setting
            {
                key = i_setting.key, value = i_setting.value, note = i_setting.note, timeup = i_setting.timeup, userup = i_setting.userup,
            });
            return;
        }
        entity.value = i_setting.value;
        entity.timeup = i_setting.timeup;
        entity.userup = i_setting.userup;
        if (i_setting.note != null) entity.note = i_setting.note;
    }
}
