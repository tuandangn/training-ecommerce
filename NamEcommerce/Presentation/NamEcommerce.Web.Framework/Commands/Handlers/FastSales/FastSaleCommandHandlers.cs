using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.FastSales;
using NamEcommerce.Web.Contracts.Models.FastSales;

namespace NamEcommerce.Web.Framework.Commands.Handlers.FastSales;

public sealed class QuickCreateOrderHandler(IFastSaleAppService fastSaleAppService)
    : IRequestHandler<QuickCreateOrderCommand, QuickSaleResultModel>
{
    public async Task<QuickSaleResultModel> Handle(QuickCreateOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await fastSaleAppService.QuickCreateOrderAsync(new QuickCreateOrderAppDto2
        {
            CustomerId = request.CustomerId,
            Items = request.Items.Select(item => new QuickCreateOrderAppDto2.QuickCreateOrderItemAppDto2
            {
                ProductId = item.ProductId,
                WarehouseId = item.WarehouseId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList(),
            OrderDiscount = request.OrderDiscount,
            Note = request.Note,
            DeliveryNow = request.DeliveryNow,
            PayNow = request.PayNow,
            PaidAmount = request.PaidAmount,
            ShippingAddress = request.ShippingAddress,
            ShippingPhoneNumber = request.ShippingPhoneNumber,
            PaymentIntentId = request.PaymentIntentId
        }).ConfigureAwait(false);

        return FastSaleCommandHandlerMapper.MapResult(result);
    }
}


public sealed class CreateCashQuickSaleHandler(IFastSaleAppService fastSaleAppService)
    : IRequestHandler<CreateCashQuickSaleCommand, QuickSaleResultModel>
{
    public async Task<QuickSaleResultModel> Handle(CreateCashQuickSaleCommand request, CancellationToken cancellationToken)
    {
        var result = await fastSaleAppService.CreateCashQuickSaleAsync(new QuickCreateOrderAppDto
        {
            CustomerId = request.CustomerId,
            Items = request.Items.Select(FastSaleCommandHandlerMapper.MapItem).ToList(),
            OrderDiscount = request.OrderDiscount,
            Note = request.Note,
            FulfillmentMode = request.FulfillmentMode,
            PaymentTiming = request.PaymentTiming,
            PaymentMethod = 0,
            PaidAmount = request.PaidAmount,
            ShippingAddress = request.ShippingAddress,
            ShippingPhoneNumber = request.ShippingPhoneNumber
        }).ConfigureAwait(false);

        return FastSaleCommandHandlerMapper.MapResult(result);
    }
}

public sealed class CreateBankTransferQuickSaleHandler(IFastSaleAppService fastSaleAppService)
    : IRequestHandler<CreateBankTransferQuickSaleCommand, QuickSaleResultModel>
{
    public async Task<QuickSaleResultModel> Handle(CreateBankTransferQuickSaleCommand request, CancellationToken cancellationToken)
    {
        var result = await fastSaleAppService.CreateBankTransferQuickSaleAsync(new QuickCreateOrderAppDto
        {
            CustomerId = request.CustomerId,
            Items = request.Items.Select(FastSaleCommandHandlerMapper.MapItem).ToList(),
            OrderDiscount = request.OrderDiscount,
            Note = request.Note,
            FulfillmentMode = request.FulfillmentMode,
            PaymentTiming = request.PaymentTiming,
            PaymentMethod = 1,
            PaidAmount = request.PaidAmount,
            ShippingAddress = request.ShippingAddress,
            ShippingPhoneNumber = request.ShippingPhoneNumber
        }, request.PaymentIntentId).ConfigureAwait(false);

        return FastSaleCommandHandlerMapper.MapResult(result);
    }
}

public sealed class CreateUnpaidQuickSaleHandler(IFastSaleAppService fastSaleAppService)
    : IRequestHandler<CreateUnpaidQuickSaleCommand, QuickSaleResultModel>
{
    public async Task<QuickSaleResultModel> Handle(CreateUnpaidQuickSaleCommand request, CancellationToken cancellationToken)
    {
        var result = await fastSaleAppService.CreateUnpaidQuickSaleAsync(new QuickCreateOrderAppDto
        {
            CustomerId = request.CustomerId,
            Items = request.Items.Select(FastSaleCommandHandlerMapper.MapItem).ToList(),
            OrderDiscount = request.OrderDiscount,
            Note = request.Note,
            FulfillmentMode = request.FulfillmentMode,
            PaymentTiming = request.PaymentTiming,
            PaymentMethod = 0,
            PaidAmount = 0,
            ShippingAddress = request.ShippingAddress,
            ShippingPhoneNumber = request.ShippingPhoneNumber
        }).ConfigureAwait(false);

        return FastSaleCommandHandlerMapper.MapResult(result);
    }
}

public sealed class CreateBankTransferPaymentIntentHandler(IBankTransferPaymentIntentAppService paymentIntentAppService)
    : IRequestHandler<CreateBankTransferPaymentIntentCommand, BankTransferPaymentIntentResultModel>
{
    public async Task<BankTransferPaymentIntentResultModel> Handle(CreateBankTransferPaymentIntentCommand request, CancellationToken cancellationToken)
    {
        var result = await paymentIntentAppService.CreateAsync(new CreateBankTransferPaymentIntentAppDto
        {
            Amount = request.Amount,
            CustomerId = request.CustomerId,
            Note = request.Note
        }).ConfigureAwait(false);

        return FastSaleCommandHandlerMapper.MapIntentResult(result);
    }
}

public sealed class GetBankTransferPaymentIntentStatusHandler(IBankTransferPaymentIntentAppService paymentIntentAppService)
    : IRequestHandler<GetBankTransferPaymentIntentStatusCommand, BankTransferPaymentIntentResultModel>
{
    public async Task<BankTransferPaymentIntentResultModel> Handle(GetBankTransferPaymentIntentStatusCommand request, CancellationToken cancellationToken)
    {
        var result = await paymentIntentAppService.GetStatusAsync(request.IntentId).ConfigureAwait(false);
        return FastSaleCommandHandlerMapper.MapIntentResult(result);
    }
}

public sealed class ManualConfirmBankTransferPaymentIntentHandler(IBankTransferPaymentIntentAppService paymentIntentAppService)
    : IRequestHandler<ManualConfirmBankTransferPaymentIntentCommand, BankTransferPaymentIntentResultModel>
{
    public async Task<BankTransferPaymentIntentResultModel> Handle(ManualConfirmBankTransferPaymentIntentCommand request, CancellationToken cancellationToken)
    {
        var result = await paymentIntentAppService.ConfirmManuallyAsync(new ManualConfirmBankTransferPaymentIntentAppDto
        {
            IntentId = request.IntentId,
            Note = request.Note
        }).ConfigureAwait(false);

        return FastSaleCommandHandlerMapper.MapIntentResult(result);
    }
}

public sealed class ProcessBankTransferProviderTransactionHandler(IBankTransferPaymentIntentAppService paymentIntentAppService)
    : IRequestHandler<ProcessBankTransferProviderTransactionCommand, BankTransferPaymentIntentResultModel>
{
    public async Task<BankTransferPaymentIntentResultModel> Handle(ProcessBankTransferProviderTransactionCommand request, CancellationToken cancellationToken)
    {
        var result = await paymentIntentAppService.ProcessProviderTransactionAsync(new ProcessBankTransferProviderTransactionAppDto
        {
            ReferenceCode = request.ReferenceCode,
            Amount = request.Amount,
            BankId = request.BankId,
            AccountNo = request.AccountNo,
            ProviderTransactionId = request.ProviderTransactionId,
            Source = request.Source,
            RawPayload = request.RawPayload,
            ConfirmedAtUtc = request.ConfirmedAtUtc
        }).ConfigureAwait(false);

        return FastSaleCommandHandlerMapper.MapIntentResult(result);
    }
}

internal static class FastSaleCommandHandlerMapper
{
    public static QuickCreateOrderItemAppDto MapItem(QuickSaleItemCommand item)
        => new()
        {
            ProductId = item.ProductId,
            WarehouseId = item.WarehouseId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        };

    public static QuickSaleResultModel MapResult(QuickSaleResultAppDto result)
        => new()
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            OrderId = result.OrderId,
            DeliveryNoteId = result.DeliveryNoteId,
            CustomerDebtId = result.CustomerDebtId,
            CustomerPaymentId = result.CustomerPaymentId,
            PaymentIntentId = result.PaymentIntentId,
            OrderItems = result.OrderItems.Select(item => new QuickSaleOrderItemResultModel
            {
                OrderItemId = item.OrderItemId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity
            }).ToList()
        };

    public static BankTransferPaymentIntentResultModel MapIntentResult(BankTransferPaymentIntentResultAppDto result)
        => new()
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            Intent = result.Intent is null
                ? null
                : MapIntent(result.Intent)
        };

    public static BankTransferPaymentIntentResultModel MapIntentResult(BankTransferProviderProcessingResultAppDto result)
        => new()
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            VerificationLogId = result.VerificationLogId,
            Intent = result.Intent is null
                ? null
                : MapIntent(result.Intent)
        };

    private static BankTransferPaymentIntentModel MapIntent(BankTransferPaymentIntentAppDto intent)
    {
        var status = (BankTransferPaymentIntentStatus)intent.Status;
        return new()
        {
            Id = intent.Id,
            ReferenceCode = intent.ReferenceCode,
            Amount = intent.Amount,
            BankId = intent.BankId,
            AccountNo = intent.AccountNo,
            AccountName = intent.AccountName,
            QrImageUrl = intent.QrImageUrl,
            Status = intent.Status,
            ExpiresAtUtc = intent.ExpiresAtUtc,
            VerificationSource = intent.VerificationSource,
            VerifiedAtUtc = intent.VerifiedAtUtc,
            IsPending = status is BankTransferPaymentIntentStatus.Pending,
            IsCancelled = status is BankTransferPaymentIntentStatus.Cancelled,
            IsExpired = status is BankTransferPaymentIntentStatus.Expired,
            IsConfirmed = status is BankTransferPaymentIntentStatus.Confirmed or BankTransferPaymentIntentStatus.ManuallyConfirmed
        };
    }
}
