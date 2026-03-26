using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, DeleteCategoryResultModel>
{
    private readonly ICategoryAppService _categoryAppService;

    public DeleteCategoryHandler(ICategoryAppService categoryAppService)
    {
        _categoryAppService = categoryAppService;
    }

    public async Task<DeleteCategoryResultModel> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var deleteResultDto = await _categoryAppService.DeleteCategoryAsync(
            new DeleteCategoryAppDto(request.Id)).ConfigureAwait(false);

        return new DeleteCategoryResultModel
        {
            Success = deleteResultDto.Success,
            ErrorMessage = deleteResultDto.ErrorMessage
        };
    }
}
