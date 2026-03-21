using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;

namespace NamEcommerce.Application.Contracts.Queries.Catalog;

[Serializable]
public record GetCategoryById(Guid Id) : IRequest<CategoryAppDto?>;