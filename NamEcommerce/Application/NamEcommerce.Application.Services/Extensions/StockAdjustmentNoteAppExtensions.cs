using NamEcommerce.Application.Contracts.Dtos.StockAdjustment;
using NamEcommerce.Domain.Shared.Dtos.StockAdjustment;

namespace NamEcommerce.Application.Services.Extensions;

public static class StockAdjustmentNoteAppExtensions
{
    public static StockAdjustmentNoteAppDto ToAppDto(this StockAdjustmentNoteDto dto) =>
        new()
        {
            Id = dto.Id,
            Code = dto.Code,
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            Note = dto.Note,
            Status = dto.Status,
            ApprovedOnUtc = dto.ApprovedOnUtc,
            CreatedOnUtc = dto.CreatedOnUtc,
            UpdatedOnUtc = dto.UpdatedOnUtc,
            Items = dto.Items.Select(i => new StockAdjustmentNoteItemAppDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                SystemQuantity = i.SystemQuantity,
                PhysicalQuantity = i.PhysicalQuantity
            }).ToList()
        };

    public static StockAdjustmentNoteListAppDto ToListAppDto(this StockAdjustmentNoteDto dto) =>
        new()
        {
            Id = dto.Id,
            Code = dto.Code,
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            Status = dto.Status,
            ItemCount = dto.Items.Count,
            CreatedOnUtc = dto.CreatedOnUtc
        };
}
