namespace NamEcommerce.Web.Models.Common;

public sealed record MenuNavigationModel
{
    public required bool CanManageUserRoles { get; init; }
    public required bool CanViewDeliveryRuns { get; init; }
    public required bool CanUseDeliveryMobile { get; init; }
}
