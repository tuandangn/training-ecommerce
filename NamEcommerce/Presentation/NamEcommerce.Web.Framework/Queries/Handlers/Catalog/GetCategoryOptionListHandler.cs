using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetCategoryOptionListHandler : IRequestHandler<GetCategoryOptionListQuery, EntityOptionListModel>
{
    private readonly IMediator _mediator;

    public GetCategoryOptionListHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<EntityOptionListModel> Handle(GetCategoryOptionListQuery request, CancellationToken cancellationToken)
    {
        var categoryData = await _mediator.Send(new GetCategoryListQuery
        {
            Keywords = null,
            PageIndex = 0,
            PageSize = int.MaxValue
        }, cancellationToken);

        var optionsData = categoryData.Data
            .Where(category => !request.ExcludedCategoryId.HasValue || category.Id != request.ExcludedCategoryId)
            .Select(category => new EntityOptionListModel.EntityOptionModel
            {
                Id = category.Id,
                Name = category.Breadcrumb ?? category.Name
            }).ToList();

        return new EntityOptionListModel
        {
            Options = optionsData
        };
    }
}
