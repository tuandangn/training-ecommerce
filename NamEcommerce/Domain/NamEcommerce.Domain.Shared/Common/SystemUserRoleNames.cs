namespace NamEcommerce.Domain.Shared.Common;

public static class SystemUserRoleNames
{
    public const string Admin = "Admin";
    public const string SalesStaff = "SalesStaff";
    public const string WarehouseManager = "WarehouseManager";
    public const string DeliveryStaff = "DeliveryStaff";
    public const string Cashier = "Cashier";

    public static readonly IReadOnlyList<string> All =
    [
        Admin,
        SalesStaff,
        WarehouseManager,
        DeliveryStaff,
        Cashier
    ];

    public static string Normalize(string roleName)
        => roleName.Trim().ToUpperInvariant();
}
