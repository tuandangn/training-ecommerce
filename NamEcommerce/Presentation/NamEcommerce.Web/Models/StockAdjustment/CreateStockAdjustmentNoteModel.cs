using FluentValidation;
using NamEcommerce.Web.Contracts.Models.StockAdjustment;

namespace NamEcommerce.Web.Models.StockAdjustment;

public sealed class CreateStockAdjustmentNoteModel
{
    public Guid? WarehouseId { get; set; }
    public string? WarehouseDisplayName { get; set; }
    public string? Note { get; set; }
    public List<StockAdjustmentNoteItemInputModel> Items { get; set; } = [];

    // Danh sách kho hàng để dropdown
    public List<WarehouseSelectItem> AvailableWarehouses { get; set; } = [];
}

public sealed class WarehouseSelectItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class CreateStockAdjustmentNoteModelValidator : AbstractValidator<CreateStockAdjustmentNoteModel>
{
    public CreateStockAdjustmentNoteModelValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("Vui lòng chọn kho hàng.");
    }
}
