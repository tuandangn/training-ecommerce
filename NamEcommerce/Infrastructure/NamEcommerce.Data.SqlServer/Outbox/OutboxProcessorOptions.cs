namespace NamEcommerce.Data.SqlServer.Outbox;

/// <summary>
/// Cấu hình cho <see cref="OutboxProcessor"/> background service.
/// Bind từ section <c>Outbox</c> trong appsettings.json.
/// </summary>
public sealed class OutboxProcessorOptions
{
    /// <summary>Interval (giây) giữa hai lần polling Outbox. Mặc định 10 giây.</summary>
    public int PollingIntervalSeconds { get; set; } = 10;

    /// <summary>Số message tối đa xử lý mỗi lần polling. Mặc định 50.</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>Số lần retry tối đa trước khi message được "giữ lại" (giữ Error nhưng không pickup nữa). Mặc định 5.</summary>
    public int MaxRetryCount { get; set; } = 5;
}
