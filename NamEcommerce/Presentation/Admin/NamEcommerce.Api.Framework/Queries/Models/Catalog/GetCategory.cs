using MediatR;
using NamEcommerce.Admin.Contracts.Models.Catalog;
using NamEcommerce.Admin.Contracts.Models.Common;

namespace NamEcommerce.Admin.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed record GetCategory(Guid Id) : IRequest<ResponseModel<CategoryModel?>>;
