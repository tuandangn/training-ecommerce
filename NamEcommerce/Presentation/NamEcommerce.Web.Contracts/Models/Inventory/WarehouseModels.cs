namespace NamEcommerce.Web.Contracts.Models.Inventory;

[Serializable]
public sealed record WarehouseModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }

    public int WarehouseType { get; set; }
    public string? WarehouseNameKey { get; set; }

    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public Guid? ManagerUserId { get; set; }

    public bool IsActive { get; set; }
}

[Serializable]
public sealed record WarehouseDetailModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required int WarehouseType { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public Guid? ManagerUserId { get; set; }
    public bool IsActive { get; set; }
}
