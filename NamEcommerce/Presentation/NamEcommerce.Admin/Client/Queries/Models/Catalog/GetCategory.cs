using MediatR;
using NamEcommerce.Admin.Client.Models.Catalog;
using NamEcommerce.Admin.Client.Models.Common;

namespace NamEcommerce.Admin.Client.Queries.Models.Catalog;

[Serializable]
public sealed record GetCategory(Guid Id) : IRequest<ResponseModel<CategoryModel?>>;
