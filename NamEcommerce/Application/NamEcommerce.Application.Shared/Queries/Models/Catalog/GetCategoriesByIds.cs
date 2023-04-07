using MediatR;
using NamEcommerce.Application.Shared.Dtos.Catalog;

namespace NamEcommerce.Application.Shared.Queries.Models.Catalog;

[Serializable]
public sealed record GetCategoriesByIds(IEnumerable<Guid> Ids) : IRequest<IEnumerable<CategoryDto>>;