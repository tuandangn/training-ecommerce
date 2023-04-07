using MediatR;
using NamEcommerce.Admin.Client.Models.Catalog;

namespace NamEcommerce.Admin.Client.Queries.Models.Catalog;

[Serializable]
public sealed record GetCategory(Guid Id) : IRequest<CategoryModel>;
