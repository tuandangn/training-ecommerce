namespace NamEcommerce.Web.Models.Common;

[Serializable]
public sealed record UserHeaderLinksModel
{
    public required bool IsAuthenticated { get; init; }

    public bool CanViewOrderFulfillmentSchedule { get; set; }
}
