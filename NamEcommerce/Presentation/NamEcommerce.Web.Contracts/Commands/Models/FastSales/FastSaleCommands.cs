using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.FastSales;

namespace NamEcommerce.Web.Contracts.Commands.Models.FastSales;

[Serializable]
public sealed class QuickCreateOrderCommand : ICommand<QuickCreateOrderResultModel>
{
    public required Guid CustomerId { get; init; }
    public required IList<QuickCreateOrderItemModel> Items { get; init; }
    public required bool DeliveryNow { get; init; }
    public decimal OrderDiscount { get; set; }

    public string? ShippingAddress { get; set; }
    public string? ShippingPhoneNumber { get; set; }

    public string? Note { get; set; }

    [Serializable]
    public sealed class QuickCreateOrderItemModel
    {
        public required Guid ProductId { get; init; }
        public Guid? WarehouseId { get; init; }
        public decimal Quantity { get; init; }
        public decimal UnitPrice { get; init; }
    }
}

[Serializable]
public sealed class QuickSaleItemCommand
{
    public required Guid ProductId { get; init; }
    public Guid WarehouseId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

[Serializable]
public sealed class CreateCashQuickSaleCommand : ICommand<QuickSaleResultModel>
{
    public required Guid CustomerId { get; init; }
    public IList<QuickSaleItemCommand> Items { get; init; } = [];
    public decimal? OrderDiscount { get; init; }
    public string? Note { get; init; }
    public int FulfillmentMode { get; init; } = 10;
    public int PaymentTiming { get; init; } = 10;
    public decimal PaidAmount { get; init; }
    public string? ShippingAddress { get; set; }
    public string? ShippingPhoneNumber { get; set; }
}

[Serializable]
public sealed class CompleteQuickCreateOrderPaymentCommand : ICommand<CommonActionResultModel>
{
    public required Guid OrderId { get; init; }
    public required decimal PaidAmount { get; init; }
    public required Guid? PaymentIntentId { get; init; }
}

[Serializable]
public sealed class CreateBankTransferQuickSaleCommand : ICommand<QuickSaleResultModel>
{
    public required Guid PaymentIntentId { get; init; }
    public required Guid CustomerId { get; init; }
    public IList<QuickSaleItemCommand> Items { get; init; } = [];
    public decimal? OrderDiscount { get; init; }
    public string? Note { get; init; }
    public int FulfillmentMode { get; init; } = 10;
    public int PaymentTiming { get; init; } = 10;
    public decimal PaidAmount { get; init; }
    public string? ShippingAddress { get; set; }
    public string? ShippingPhoneNumber { get; set; }
}

[Serializable]
public sealed class CreateUnpaidQuickSaleCommand : ICommand<QuickSaleResultModel>
{
    public required Guid CustomerId { get; init; }
    public IList<QuickSaleItemCommand> Items { get; init; } = [];
    public decimal? OrderDiscount { get; init; }
    public string? Note { get; init; }
    public int FulfillmentMode { get; init; } = 10;
    public int PaymentTiming { get; init; } = 20;
    public string? ShippingAddress { get; set; }
    public string? ShippingPhoneNumber { get; set; }
}

[Serializable]
public sealed class CreateBankTransferPaymentIntentCommand : ICommand<BankTransferPaymentIntentResultModel>
{
    public required decimal Amount { get; init; }
    public Guid CustomerId { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed class GetBankTransferPaymentIntentStatusCommand : ICommand<BankTransferPaymentIntentResultModel>
{
    public required Guid IntentId { get; init; }
}

[Serializable]
public sealed class ManualConfirmBankTransferPaymentIntentCommand : ICommand<BankTransferPaymentIntentResultModel>
{
    public required Guid IntentId { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed class ProcessBankTransferProviderTransactionCommand : ICommand<BankTransferPaymentIntentResultModel>
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
