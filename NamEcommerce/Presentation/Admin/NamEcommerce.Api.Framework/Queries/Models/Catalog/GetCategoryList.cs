using MediatR;
using NamEcommerce.Admin.Contracts.Models.Catalog;
using NamEcommerce.Admin.Contracts.Models.Common;

namespace NamEcommerce.Admin.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed record GetCategoryList(int PageNumer, int PageSize) 
    : IRequest<ResponseModel<CategoryListModel>>;