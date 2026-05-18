using NamEcommerce.Application.Contracts.Dtos.StockTransfer;
using NamEcommerce.Domain.Shared.Dtos.StockTransfer;

namespace NamEcommerce.Application.Services.Extensions;

public static class StockTransferNoteAppExtensions
{
    public static StockTransferNoteAppDto ToAppDto(this StockTransferNoteDto dto) =>
        new()
        {
            Id = dto.Id,
            Code = dto.Code,
            FromWarehouseId = dto.FromWarehouseId,
            FromWarehouseName = dto.FromWarehouseName,
            ToWarehouseId = dto.ToWarehouseId,
            ToWarehouseName = dto.ToWarehouseName,
            Note = dto.Note,
            Status = (int)dto.Status,
            ApprovedOnUtc = dto.ApprovedOnUtc,
            CreatedOnUtc = dto.CreatedOnUtc,
            UpdatedOnUtc = dto.UpdatedOnUtc,
            Items = dto.Items.Select(i => new StockTransferNoteItemAppDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            }).ToList()
        };

    public static StockTransferNoteListAppDto ToListAppDto(this StockTransferNoteDto dto) =>
        new()
        {
            Id = dto.Id,
            Code = dto.Code,
            FromWarehouseId = dto.FromWarehouseId,
            FromWarehouseName = dto.FromWarehouseName,
            ToWarehouseId = dto.ToWarehouseId,
            ToWarehouseName = dto.ToWarehouseName,
            Status = (int)dto.Status,
            ItemCount = dto.Items.Count,
            CreatedOnUtc = dto.CreatedOnUtc
        };
}
