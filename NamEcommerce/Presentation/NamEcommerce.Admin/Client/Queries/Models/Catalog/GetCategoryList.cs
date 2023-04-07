using MediatR;
using NamEcommerce.Admin.Client.Models.Catalog;

namespace NamEcommerce.Admin.Client.Queries.Models.Catalog;

[Serializable]
public sealed record GetCategoryList(int PageNumer, int PageSize) : IRequest<CategoryListModel>;