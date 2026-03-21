using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;

namespace NamEcommerce.Application.Contracts.Queries.Catalog;

[Serializable]
public sealed record GetAllCategories() : IRequest<IEnumerable<CategoryAppDto>>;
