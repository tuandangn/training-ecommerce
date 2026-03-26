using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class DeleteProductHandler : IRequestHandler<DeleteProductCommand, DeleteProductResultModel>
{
    private readonly IProductAppService _productAppService;

    public DeleteProductHandler(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    public async Task<DeleteProductResultModel> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var deleteResultDto = await _productAppService.DeleteProductAsync(
            new DeleteProductAppDto(request.Id)).ConfigureAwait(false);

        return new DeleteProductResultModel
        {
            Success = deleteResultDto.Success,
            ErrorMessage = deleteResultDto.ErrorMessage
        };
    }
}
