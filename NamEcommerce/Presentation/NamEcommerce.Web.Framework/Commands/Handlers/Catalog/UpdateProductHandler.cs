using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;

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
            Name = request.Name,
            ShortDesc = request.ShortDesc,
            UnitMeasurementId = request.UnitMeasurementId,
            Categories = request.CategoryId.HasValue
                ? [new ProductCategoryAppDto(request.CategoryId.Value, request.DisplayOrder)]
                : [],
            Vendors = request.VendorIds?.Select(id => new ProductVendorAppDto(id, 0)) ?? [],
            Pictures = request.PictureId.HasValue ? [request.PictureId.Value] : [],
            NewUnitPrice = request.NewUnitPrice,
            ChangePriceReason = request.ChangePriceReason
        });

        return new UpdateProductResultModel
        {
            Success = updateResult.Success,
            ErrorMessage = updateResult.ErrorMessage
        };
    }
}
