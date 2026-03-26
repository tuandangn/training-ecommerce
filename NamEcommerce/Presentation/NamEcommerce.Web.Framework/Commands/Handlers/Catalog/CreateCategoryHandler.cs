using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, CreateCategoryResultModel>
{
    private readonly ICategoryAppService _categoryAppService;

    public CreateCategoryHandler(ICategoryAppService categoryAppService)
    {
        _categoryAppService = categoryAppService;
    }

    public async Task<CreateCategoryResultModel> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var result = await _categoryAppService.CreateCategoryAsync(new CreateCategoryAppDto
        {
            Name = request.Name,
            ParentId = request.ParentId,
            DisplayOrder = request.DisplayOrder
        }).ConfigureAwait(false);

        return new CreateCategoryResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId ?? default
        };
    }
}
