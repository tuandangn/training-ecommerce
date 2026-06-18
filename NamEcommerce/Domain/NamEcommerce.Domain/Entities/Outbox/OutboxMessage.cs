using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Outbox;

/// <summary>
/// Bản ghi message của Transactional Outbox Pattern.
/// <para>
/// Mỗi <c>IIntegrationEvent</c> được serialize và lưu vào bảng <c>OutboxMessages</c>
/// trong cùng transaction với business operation. Sau đó <c>OutboxProcessor</c> background service
/// đọc các message chưa processed (<see cref="ProcessedOnUtc"/> = null), publish qua MediatR
/// và đánh dấu đã processed.
/// </para>
/// <para>
/// Đảm bảo: business state và integration event luôn nhất quán
/// (atomic — cùng commit hoặc cùng rollback).
/// </para>
/// </summary>
[Serializable]
public sealed record OutboxMessage : AppEntity
{
    internal OutboxMessage(string type, string payload, DateTime occurredOnUtc) : base(Guid.Empty)
    {
        ArgumentException.ThrowIfNullOrEmpty(type);
        ArgumentException.ThrowIfNullOrEmpty(payload);

        Type = type;
        Payload = payload;
        OccurredOnUtc = occurredOnUtc;
        RetryCount = 0;
    }

    /// <summary>
    /// CLR <c>AssemblyQualifiedName</c> của integration event — dùng để deserialize ngược lại
    /// đúng concrete type khi đọc message từ DB.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// Nội dung event đã serialize sang JSON (System.Text.Json hoặc Newtonsoft).
    /// </summary>
    public string Payload { get; private set; }

    /// <summary>
    /// Thời điểm event xảy ra (UTC) — copy từ <c>IIntegrationEvent.OccurredOnUtc</c>.
    /// </summary>
    public DateTime OccurredOnUtc { get; private set; }

    /// <summary>
    /// Thời điểm message được dispatch thành công.
    /// <c>null</c> = chưa processed → Background service sẽ pick up.
    /// </summary>
    public DateTime? ProcessedOnUtc { get; private set; }

    /// <summary>
    /// Thông tin lỗi của lần dispatch gần nhất (nếu có).
    /// Reset về <c>null</c> khi dispatch thành công.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Số lần đã retry — dùng để giới hạn retry và tính exponential backoff.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Factory method tạo OutboxMessage mới — chỉ <c>IOutbox</c> implementation gọi method này.
    /// </summary>
    internal static OutboxMessage Create(string type, string payload, DateTime occurredOnUtc)
        => new(type, payload, occurredOnUtc);

    /// <summary>
    /// Đánh dấu message đã được dispatch thành công.
    /// </summary>
    internal void MarkAsProcessed()
    {
        ProcessedOnUtc = DateTime.UtcNow;
        Error = null;
    }

    /// <summary>
    /// Ghi nhận lỗi dispatch — tăng RetryCount và lưu thông điệp lỗi.
    /// </summary>
    internal void MarkAsFailed(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrEmpty(errorMessage);
        Error = errorMessage;
        RetryCount += 1;
    }
}
