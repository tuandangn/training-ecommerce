namespace NamEcommerce.Web.Contracts.Commands;

/// <summary>
/// Opt-in: command result implement interface này cam kết "Success = false nghĩa là không có gì cần persist".
/// UnitOfWorkBehavior sẽ bỏ qua CommitAsync khi Success = false. KHÔNG implement trên các result
/// mà flow thất bại vẫn phải ghi dữ liệu (vd: Casso reconciliation FailAsync, RegisterUser tạo user xong
/// nhưng auto-login fail, payment verification log bị reject).
/// </summary>
public interface ICommandResult
{
    bool Success { get; }
}
