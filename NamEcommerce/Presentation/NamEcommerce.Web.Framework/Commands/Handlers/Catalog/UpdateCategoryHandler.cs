using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.UnitMeasurements;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, UpdateCategoryResultModel>
{
    private readonly ICategoryAppService _categoryAppService;

    public UpdateCategoryHandler(ICategoryAppService categoryAppService)
    {
        _categoryAppService = categoryAppService;
    }

    public async Task<UpdateCategoryResultModel> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var updateResult = await _categoryAppService.UpdateCategoryAsync(new UpdateCategoryAppDto(request.Id) {
            Name = request.Name,
            DisplayOrder = request.DisplayOrder,
            ParentId = request.ParentId
        });

        return new UpdateCategoryResultModel
        {
            Success = updateResult.Success,
            ErrorMessage = updateResult.ErrorMessage,
            UpdatedId = updateResult.UpdatedId
        };
    }
}
