using FluentValidation;
using NamEcommerce.Web.Contracts.Models.StockTransfer;

namespace NamEcommerce.Web.Models.StockTransfer;

public sealed class CreateStockTransferNoteModel
{
    public Guid? FromWarehouseId { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public string? Note { get; set; }
    public List<StockTransferNoteItemInputModel> Items { get; set; } = [];

    public List<TransferWarehouseSelectItem> AvailableWarehouses { get; set; } = [];
}

public sealed class TransferWarehouseSelectItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class CreateStockTransferNoteModelValidator : AbstractValidator<CreateStockTransferNoteModel>
{
    public CreateStockTransferNoteModelValidator()
    {
        RuleFor(x => x.FromWarehouseId).NotEmpty().WithMessage("Vui lòng chọn kho nguồn.");
        RuleFor(x => x.ToWarehouseId).NotEmpty().WithMessage("Vui lòng chọn kho đích.");
    }
}
