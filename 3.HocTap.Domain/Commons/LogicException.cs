namespace HocTap.Domain.Commons;

/// <summary>
/// 08/10/2026 - Lỗi nghiệp vụ: middleware trả về HTTP ErrorCode (mặc định 400) kèm Message tiếng Việt cho người dùng.
/// 403: không có quyền với dữ liệu (vd hồ sơ con của tài khoản khác); 404: không tìm thấy.
/// </summary>
public class LogicException : Exception
{
    public LogicException(string message, int errorCode = 400) : base(message)
    {
        ErrorCode = errorCode;
    }

    public int ErrorCode { get; set; }
}
