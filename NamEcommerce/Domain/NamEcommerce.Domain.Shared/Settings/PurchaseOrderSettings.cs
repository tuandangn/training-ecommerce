namespace NamEcommerce.Domain.Shared.Settings;

[Serializable]
public sealed class PurchaseOrderSettings
{
    public bool CreateFromShortageAutoApproved { get; init; }
}
