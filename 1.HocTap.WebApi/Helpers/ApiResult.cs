namespace HocTap.WebApi.Helpers;

/// <summary>08/10/2026 - Dạng trả về chung { code, message, data } (giống ServiceResponseResult bên Web-MultiClinic)</summary>
public class ApiResult
{
    public int Code { get; set; }
    public string Message { get; set; } = "";
    public object? Data { get; set; }

    public static ApiResult Ok(object? data = null) => new() { Code = 200, Message = "Success", Data = data };
    public static ApiResult Fail(int code, string message) => new() { Code = code, Message = message };
}
