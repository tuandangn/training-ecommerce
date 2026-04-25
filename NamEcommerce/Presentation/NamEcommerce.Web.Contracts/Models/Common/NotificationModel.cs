namespace NamEcommerce.Web.Contracts.Models.Common;

[Serializable]
public sealed record NotificationModel
{
    public required NotificationType Type { get; set; }
    public required string Message { get; set; }
    public string? Title { get; set; }
    public int DurationMs { get; set; } = 4000;
}
