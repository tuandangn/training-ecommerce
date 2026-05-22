using NamEcommerce.Domain.Entities.StockTransfer;
using NamEcommerce.Domain.Shared.Dtos.StockTransfer;

namespace NamEcommerce.Domain.Services.Extensions;

public static class StockTransferNoteExtensions
{
    public static StockTransferNoteDto ToDto(this StockTransferNote note) =>
        new()
        {
            Id = note.Id,
            Code = note.Code,
            FromWarehouseId = note.FromWarehouseId,
            FromWarehouseName = note.FromWarehouseName,
            ToWarehouseId = note.ToWarehouseId,
            ToWarehouseName = note.ToWarehouseName,
            Note = note.Note,
            Status = note.Status,
            ApprovedOnUtc = note.ApprovedOnUtc,
            CreatedByUserId = note.CreatedByUserId,
            CreatedOnUtc = note.CreatedOnUtc,
            UpdatedOnUtc = note.UpdatedOnUtc,
            Items = note.Items.Select(i => new StockTransferNoteItemDto
            {
                Id = i.Id,
                NoteId = i.NoteId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            }).ToList()
        };
}
