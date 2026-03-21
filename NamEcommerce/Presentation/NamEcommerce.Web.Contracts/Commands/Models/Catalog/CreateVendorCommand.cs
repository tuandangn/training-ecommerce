using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Commands.Models.Catalog;

[Serializable]
public sealed class CreateVendorCommand : IRequest<CreateVendorResultModel>
{
    public required string Name { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Address { get; set; }
    public int DisplayOrder { get; set; }
}
