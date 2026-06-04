using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class CreateProductHandler(IProductAppService productAppService)
    : IRequestHandler<CreateProductCommand, CreateProductResultModel>
{
    public async Task<CreateProductResultModel> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var dto = new CreateProductAppDto
        {
            Name = request.Name,
            ShortDesc = request.ShortDesc,
            UnitMeasurementId = request.UnitMeasurementId,
            UnitPrice = request.UnitPrice ?? 0,
            Categories = request.CategoryId.HasValue
                ? [new ProductCategoryAppDto(request.CategoryId.Value, request.DisplayOrder)]
                : [],
            Vendors = request.VendorIds?.Select(id => new ProductVendorAppDto(id, 0)) ?? [],
            Pictures = request.PictureId.HasValue ? [request.PictureId.Value] : [],
            InitialStocks = request.ProductStocks?
                .Where(s => s.Quantity > 0)
                .Select(s => new InitialStockAppDto
                {
                    WarehouseId = s.WarehouseId,
                    Quantity = s.Quantity,
                    UnitCost = s.UnitCost
                }).ToList() ?? []
        };

        var result = await productAppService.CreateProductAsync(dto).ConfigureAwait(false);

        return new CreateProductResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId ?? Guid.Empty
        };
    }
}
