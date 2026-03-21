using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;

namespace NamEcommerce.Application.Contracts.Queries.Catalog;

[Serializable]
public sealed record GetCategoriesByIds(IEnumerable<Guid> Ids) : IRequest<IEnumerable<CategoryAppDto>>;