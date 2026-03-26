using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.UnitMeasurements;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class UpdateProductHandler : IRequestHandler<UpdateProductCommand, UpdateProductResultModel>
{
    private readonly IProductAppService _productAppService;

    public UpdateProductHandler(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    public async Task<UpdateProductResultModel> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var updateResult = await _productAppService.UpdateProductAsync(new UpdateProductAppDto(request.Id)
        {
            ShortDesc = request.ShortDesc,
            Name = request.Name,
            Categories = request.CategoryId.HasValue
                ? [new ProductCategoryAppDto(request.CategoryId.Value, request.DisplayOrder)]
                : [],
            ImageFile = request.ImageFile is not null ? new FileInfoAppDto
            {
                Data = request.ImageFile.Data,
                MimeType = request.ImageFile.MimeType,
                Extension = request.ImageFile.Extension,
                FileName = request.ImageFile.FileName
            } : null
        });

        return new UpdateProductResultModel
        {
            Success = updateResult.Success,
            ErrorMessage = updateResult.ErrorMessage,
            UpdatedId = updateResult.UpdatedId
        };
    }
}
