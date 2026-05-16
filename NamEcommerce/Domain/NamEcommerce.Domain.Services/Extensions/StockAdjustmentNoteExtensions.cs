using NamEcommerce.Domain.Entities.StockAdjustment;
using NamEcommerce.Domain.Shared.Dtos.StockAdjustment;

namespace NamEcommerce.Domain.Services.Extensions;

public static class StockAdjustmentNoteExtensions
{
    public static StockAdjustmentNoteDto ToDto(this StockAdjustmentNote note) =>
        new()
        {
            Id = note.Id,
            Code = note.Code,
            WarehouseId = note.WarehouseId,
            WarehouseName = note.WarehouseName,
            Note = note.Note,
            Status = note.Status,
            ApprovedOnUtc = note.ApprovedOnUtc,
            CreatedByUserId = note.CreatedByUserId,
            CreatedOnUtc = note.CreatedOnUtc,
            UpdatedOnUtc = note.UpdatedOnUtc,
            Items = note.Items.Select(i => new StockAdjustmentNoteItemDto
            {
                Id = i.Id,
                NoteId = i.NoteId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                SystemQuantity = i.SystemQuantity,
                PhysicalQuantity = i.PhysicalQuantity
            }).ToList()
        };
}
