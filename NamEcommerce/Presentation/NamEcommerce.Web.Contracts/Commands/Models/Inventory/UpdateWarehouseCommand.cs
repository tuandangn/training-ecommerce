using MediatR;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Commands.Models.Inventory;

[Serializable]
public sealed class UpdateWarehouseCommand : IRequest<UpdateWarehouseResultModel>
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required int WarehouseType { get; init; }
    public string? Address { get; init; }
    public string? PhoneNumber { get; init; }
    public Guid? ManagerUserId { get; set; }
    public bool IsActive { get; set; }
}

