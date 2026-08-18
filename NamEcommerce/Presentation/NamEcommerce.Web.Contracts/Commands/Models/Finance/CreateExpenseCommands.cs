using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Finance;

public abstract class BaseCreateExpenseCommand
{
    public required string Title { get; init; }
    public required decimal AmountWithoutTax { get; init; }
    public required int ExpenseType { get; init; }
    public required DateTime IncurredDate { get; init; }
    public decimal? TaxRate { get; set; }
    public string? Description { get; set; }
}

[Serializable]
public sealed class CreateGeneralExpenseCommand() : BaseCreateExpenseCommand, ICommand<CommonActionResultModel>;

[Serializable]
public sealed class CreateOrderExpenseCommand : BaseCreateExpenseCommand, ICommand<CommonActionResultModel>
{
    public required Guid OrderId { get; set; }
}
[Serializable]
public sealed class CreatePurchaseOrderExpenseCommand : BaseCreateExpenseCommand, ICommand<CommonActionResultModel>
{
    public required Guid PurchaseOrderId { get; set; }
}

[Serializable]
public sealed class CreateCustomerReturnExpenseCommand : BaseCreateExpenseCommand, ICommand<CommonActionResultModel>
{
    public required Guid CustomerReturnId { get; set; }
}

[Serializable]
public sealed class CreateVendorReturnExpenseCommand : BaseCreateExpenseCommand, ICommand<CommonActionResultModel>
{
    public required Guid VendorReturnId { get; set; }
}

