using HocTap.Application.Settings;
using HocTap.Domain.Aggregates.Settings;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;
using HocTap.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HocTap.WebApi.Controllers;

/// <summary>
/// 14/10/2026 - CÀI ĐẶT HỆ THỐNG (chỉ ADMIN) — tab "Cài đặt" trang quản trị
///   GET api/v1/admin/settings          bảng giá (kể cả gói ngưng bán) + liên hệ hỗ trợ
///   PUT api/v1/admin/settings/plans    lưu danh sách gói [{ code, name, months, subjects, price, note, sortOrder, isActive }]
///   PUT api/v1/admin/settings/support  lưu liên hệ { name, phone, zalo, hours }
/// Dữ liệu công khai (gói đang bán + liên hệ) trả qua GET api/v1/payments/config.
/// </summary>
[Authorize(Roles = "ADMIN")]
[Route("api/v1/admin/settings")]
public class SettingsController : BaseApiController
{
    private readonly ISettingServices settingServices;
    private readonly ILogger<SettingsController> logger;

    public SettingsController(ISettingServices i_SettingServices, ILogger<SettingsController> i_Logger)
    {
        settingServices = i_SettingServices;
        logger = i_Logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAdminSettings()
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AdminSettingsReadModel retObj = await Task.Run(() => settingServices.GetAdminSettings(caller));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SettingsController.GetAdminSettings");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    [HttpPut("plans")]
    public async Task<IActionResult> SavePlans([FromBody] List<SavePlanPriceModel> i_plans)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AdminSettingsReadModel retObj = await Task.Run(() => settingServices.SavePlans(caller, i_plans));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SettingsController.SavePlans");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    [HttpPut("support")]
    public async Task<IActionResult> SaveSupport([FromBody] SaveSupportModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AdminSettingsReadModel retObj = await Task.Run(() => settingServices.SaveSupport(caller, i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SettingsController.SaveSupport");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }
}
