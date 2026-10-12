using HocTap.Application.Payments;
using HocTap.Domain.Aggregates.Payments;
using HocTap.Domain.Bases;
using HocTap.Domain.Commons;
using HocTap.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HocTap.WebApi.Controllers;

/// <summary>
/// 12/10/2026 - GIA HẠN GÓI BẰNG CHUYỂN KHOẢN QR
///   GET  api/v1/payments/config            (công khai)   bảng giá + tài khoản nhận tiền
///   POST api/v1/payments/orders            (phụ huynh)   tạo / dùng lại đơn, trả ảnh QR { planCode }
///   GET  api/v1/payments/orders/{id}       (phụ huynh)   trạng thái đơn (client hỏi định kỳ)
///   POST api/v1/payments/webhook/sepay     (SePay)       báo có tiền về -> tự gia hạn; header "Authorization: Apikey ..."
///   GET  api/v1/payments/admin/orders?status=      (ADMIN) danh sách đơn
///   POST api/v1/payments/admin/orders/{id}/confirm (ADMIN) xác nhận tay đã nhận tiền
/// Mẫu action giống các controller khác: LogicException -> ErrorCode + thông báo; Exception -> 500 (ghi log).
/// </summary>
[Route("api/v1/payments")]
public class PaymentsController : BaseApiController
{
    private readonly IPaymentServices paymentServices;
    private readonly ILogger<PaymentsController> logger;

    public PaymentsController(IPaymentServices i_PaymentServices, ILogger<PaymentsController> i_Logger)
    {
        paymentServices = i_PaymentServices;
        logger = i_Logger;
    }

    [AllowAnonymous]
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        try
        {
            return Ok(ApiResult.Ok(paymentServices.GetConfig()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PaymentsController.GetConfig");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
    }

    [Authorize]
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreatePaymentOrderModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            PaymentOrderReadModel retObj = await Task.Run(() => paymentServices.CreateOrder(caller, i_model));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PaymentsController.CreateOrder");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    [Authorize]
    [HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            PaymentOrderReadModel retObj = await Task.Run(() => paymentServices.GetOrder(caller, id));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PaymentsController.GetOrder");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    /// <summary>
    /// Webhook SePay: trả { success: true } (HTTP 200) khi đã nhận — kể cả giao dịch không khớp đơn (ghi log để admin xử lý),
    /// để SePay không gửi lại. Sai khoá -> 401.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook/sepay")]
    public async Task<IActionResult> SepayWebhook([FromBody] SepayWebhookModel i_model)
    {
        try
        {
            string? apiKey = Request.Headers.Authorization.FirstOrDefault();
            BankTransactionModel txn = new BankTransactionModel
            {
                Gateway = "SEPAY",
                TransactionId = i_model.id.ToString(),
                Content = string.IsNullOrWhiteSpace(i_model.content) ? i_model.description : i_model.content,
                Code = i_model.code,
                Amount = i_model.transferAmount,
                TransferType = i_model.transferType,
                AccountNumber = i_model.accountNumber,
                ReferenceCode = i_model.referenceCode,
            };
            BankTransactionResultReadModel result = await Task.Run(() => paymentServices.HandleBankTransaction(apiKey, txn));
            logger.LogInformation("SePay #{Id} {Amount} '{Content}' -> {Matched} {Code}: {Message}",
                i_model.id, i_model.transferAmount, txn.Content, result.Matched, result.OrderCode, result.Message);
            return Ok(new { success = true, matched = result.Matched, message = result.Message });
        }
        catch (LogicException ex)
        {
            logger.LogWarning("SePay webhook bị từ chối: {Message}", ex.Message);
            return StatusCode(ex.ErrorCode, new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PaymentsController.SepayWebhook");
            return StatusCode(500, new { success = false, message = SYSTEM_ERROR });
        }
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet("admin/orders")]
    public async Task<IActionResult> GetListOrderForAdmin([FromQuery] string? status)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            List<AdminPaymentReadModel> retObj = await Task.Run(() => paymentServices.GetListOrderForAdmin(caller, status));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PaymentsController.GetListOrderForAdmin");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost("admin/orders/{id:guid}/confirm")]
    public async Task<IActionResult> ConfirmOrderByAdmin(Guid id, [FromBody] AdminConfirmPaymentModel i_model)
    {
        ApiResult sr;
        try
        {
            CurrentUser caller = Caller;
            AdminPaymentReadModel retObj = await Task.Run(() => paymentServices.ConfirmOrderByAdmin(caller, id, i_model ?? new AdminConfirmPaymentModel()));
            sr = ApiResult.Ok(retObj);
        }
        catch (LogicException ex)
        {
            return StatusCode(ex.ErrorCode, ApiResult.Fail(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PaymentsController.ConfirmOrderByAdmin");
            return StatusCode(500, ApiResult.Fail(500, SYSTEM_ERROR));
        }
        return Ok(sr);
    }
}
