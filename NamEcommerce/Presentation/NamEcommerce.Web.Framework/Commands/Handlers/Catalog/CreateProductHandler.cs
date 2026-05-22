using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.StockAdjustment;
using NamEcommerce.Application.Contracts.StockAdjustment;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class CreateProductHandler(
    IProductAppService productAppService,
    IStockAdjustmentNoteAppService stockAdjustmentNoteAppService)
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
            ImageFile = request.ImageFile is not null ? new FileInfoAppDto
            {
                Data = request.ImageFile.Data,
                MimeType = request.ImageFile.MimeType,
                Extension = request.ImageFile.Extension,
                FileName = request.ImageFile.FileName
            } : null
        };

        var result = await productAppService.CreateProductAsync(dto).ConfigureAwait(false);
        if (!result.Success)
        {
            return new CreateProductResultModel
            {
                Success = false,
                ErrorMessage = result.ErrorMessage
            };
        }

        // C5: Nếu có tồn kho ban đầu, tạo + duyệt StockAdjustmentNote cho từng kho
        var initialStocks = request.ProductStocks?.ToList();
        if (initialStocks is { Count: > 0 } && result.CreatedId != Guid.Empty)
        {
            foreach (var stock in initialStocks)
            {
                var noteDto = new CreateStockAdjustmentNoteAppDto
                {
                    WarehouseId = stock.WarehouseId,
                    Note = $"Tồn kho ban đầu khi tạo hàng hóa mới",
                    Items =
                    [
                        new CreateStockAdjustmentNoteItemAppDto
                        {
                            ProductId = result.CreatedId ?? default,
                            ProductName = request.Name,
                            SystemQuantity = 0,
                            PhysicalQuantity = stock.Quantity
                        }
                    ]
                };

                var noteResult = await stockAdjustmentNoteAppService
                    .CreateAsync(noteDto).ConfigureAwait(false);

                if (noteResult.Success && noteResult.CreatedId.HasValue)
                {
                    await stockAdjustmentNoteAppService
                        .ApproveAsync(noteResult.CreatedId.Value).ConfigureAwait(false);
                }
            }
        }

        return new CreateProductResultModel
        {
            Success = true,
            ErrorMessage = null,
            CreatedId = result.CreatedId ?? Guid.Empty
        };
    }
}
