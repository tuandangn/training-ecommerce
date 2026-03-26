using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Commands.Models.Catalog;

[Serializable]
public sealed class CreateCategoryCommand : IRequest<CreateCategoryResultModel>
{
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }
    public required Guid? ParentId { get; init; }
}
