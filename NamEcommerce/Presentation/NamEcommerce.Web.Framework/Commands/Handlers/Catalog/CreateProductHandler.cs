using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class CreateProductHandler : IRequestHandler<CreateProductCommand, CreateProductResultModel>
{
    private readonly IProductAppService _productAppService;

    public CreateProductHandler(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    public async Task<CreateProductResultModel> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var dto = new CreateProductAppDto
        {
            Name = request.Name,
            ShortDesc = request.ShortDesc,
            UnitMeasurementId = request.UnitMeasurementId,
            Categories = request.CategoryId.HasValue
                ? [new ProductCategoryAppDto(request.CategoryId.Value, request.DisplayOrder)]
                : [],
            ImageFile = request.ImageFile is not null ? new FileInfoAppDto
            {
                Data = request.ImageFile.Data,
                MimeType = request.ImageFile.MimeType,
                Extension = request.ImageFile.Extension,
                FileName = request.ImageFile.FileName
            } : null,
            TrackInventory = request.TrackInventory
        };

        var result = await _productAppService.CreateProductAsync(dto).ConfigureAwait(false);

        return new CreateProductResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId ?? default
        };
    }
}
