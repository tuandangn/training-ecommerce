using MediatR;
using NamEcommerce.Web.Contracts.Models.FastSales;

namespace NamEcommerce.Web.Contracts.Commands.Models.FastSales;

[Serializable]
public sealed class QuickSaleItemCommand
{
    public required Guid ProductId { get; init; }
    public Guid WarehouseId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

[Serializable]
public sealed class CreateCashQuickSaleCommand : IRequest<QuickSaleResultModel>
{
    public required Guid CustomerId { get; init; }
    public required Guid WarehouseId { get; init; }
    public IList<QuickSaleItemCommand> Items { get; init; } = [];
    public decimal? OrderDiscount { get; init; }
    public string? Note { get; init; }
    public int FulfillmentMode { get; init; } = 10;
    public int PaymentTiming { get; init; } = 10;
    public decimal PaidAmount { get; init; }
}

[Serializable]
public sealed class CreateBankTransferQuickSaleCommand : IRequest<QuickSaleResultModel>
{
    public required Guid PaymentIntentId { get; init; }
    public required Guid CustomerId { get; init; }
    public required Guid WarehouseId { get; init; }
    public IList<QuickSaleItemCommand> Items { get; init; } = [];
    public decimal? OrderDiscount { get; init; }
    public string? Note { get; init; }
    public int FulfillmentMode { get; init; } = 10;
    public int PaymentTiming { get; init; } = 10;
    public decimal PaidAmount { get; init; }
}

[Serializable]
public sealed class CreateUnpaidQuickSaleCommand : IRequest<QuickSaleResultModel>
{
    public required Guid CustomerId { get; init; }
    public required Guid WarehouseId { get; init; }
    public IList<QuickSaleItemCommand> Items { get; init; } = [];
    public decimal? OrderDiscount { get; init; }
    public string? Note { get; init; }
    public int FulfillmentMode { get; init; } = 10;
    public int PaymentTiming { get; init; } = 20;
}

[Serializable]
public sealed class CreateBankTransferPaymentIntentCommand : IRequest<BankTransferPaymentIntentResultModel>
{
    public required decimal Amount { get; init; }
    public Guid? CustomerId { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed class GetBankTransferPaymentIntentStatusCommand : IRequest<BankTransferPaymentIntentResultModel>
{
    public required Guid IntentId { get; init; }
}

[Serializable]
public sealed class ManualConfirmBankTransferPaymentIntentCommand : IRequest<BankTransferPaymentIntentResultModel>
{
    public required Guid IntentId { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed class ProcessBankTransferProviderTransactionCommand : IRequest<BankTransferPaymentIntentResultModel>
{
    public required string ReferenceCode { get; init; }
    public required decimal Amount { get; init; }
    public required string BankId { get; init; }
    public required string AccountNo { get; init; }
    public required string ProviderTransactionId { get; init; }
    public required int Source { get; init; }
    public string? RawPayload { get; init; }
    public DateTime ConfirmedAtUtc { get; init; } = DateTime.UtcNow;
}
