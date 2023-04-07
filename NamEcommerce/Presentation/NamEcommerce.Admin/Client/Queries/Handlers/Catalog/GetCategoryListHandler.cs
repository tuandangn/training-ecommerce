using MediatR;
using NamEcommerce.Admin.Client.Models.Catalog;
using NamEcommerce.Admin.Client.Queries.Models.Catalog;

namespace NamEcommerce.Admin.Client.Queries.Handlers.Catalog;

public sealed class GetCategoryListHandler : IRequestHandler<GetCategoryList, CategoryListModel>
{
    public async Task<CategoryListModel> Handle(GetCategoryList request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
