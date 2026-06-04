using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.FastSales;
using NamEcommerce.Web.Contracts.Models.FastSales;

namespace NamEcommerce.Web.Framework.Commands.Handlers.FastSales;

public sealed class CreateCashQuickSaleHandler(IFastSaleAppService fastSaleAppService)
    : IRequestHandler<CreateCashQuickSaleCommand, QuickSaleResultModel>
{
    public async Task<QuickSaleResultModel> Handle(CreateCashQuickSaleCommand request, CancellationToken cancellationToken)
    {
        var result = await fastSaleAppService.CreateCashQuickSaleAsync(new CreateQuickSaleAppDto
        {
            CustomerId = request.CustomerId,
            WarehouseId = request.WarehouseId,
            Items = request.Items.Select(FastSaleCommandHandlerMapper.MapItem).ToList(),
            OrderDiscount = request.OrderDiscount,
            Note = request.Note,
            PaymentMethod = 0,
            PaidAmount = request.PaidAmount
        }).ConfigureAwait(false);

        return FastSaleCommandHandlerMapper.MapResult(result);
    }
}

public sealed class CreateBankTransferQuickSaleHandler(IFastSaleAppService fastSaleAppService)
    : IRequestHandler<CreateBankTransferQuickSaleCommand, QuickSaleResultModel>
{
    public async Task<QuickSaleResultModel> Handle(CreateBankTransferQuickSaleCommand request, CancellationToken cancellationToken)
    {
        var result = await fastSaleAppService.CreateBankTransferQuickSaleAsync(new CreateQuickSaleAppDto
        {
            CustomerId = request.CustomerId,
            WarehouseId = request.WarehouseId,
            Items = request.Items.Select(FastSaleCommandHandlerMapper.MapItem).ToList(),
            OrderDiscount = request.OrderDiscount,
            Note = request.Note,
            PaymentMethod = 1,
            PaidAmount = request.PaidAmount
        }, request.PaymentIntentId).ConfigureAwait(false);

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

internal static class FastSaleCommandHandlerMapper
{
    public static QuickSaleItemAppDto MapItem(QuickSaleItemCommand item)
        => new()
        {
            ProductId = item.ProductId,
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
            PaymentIntentId = result.PaymentIntentId
        };

    public static BankTransferPaymentIntentResultModel MapIntentResult(BankTransferPaymentIntentResultAppDto result)
        => new()
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            Intent = result.Intent is null
                ? null
                : new BankTransferPaymentIntentModel
                {
                    Id = result.Intent.Id,
                    ReferenceCode = result.Intent.ReferenceCode,
                    Amount = result.Intent.Amount,
                    BankId = result.Intent.BankId,
                    AccountNo = result.Intent.AccountNo,
                    AccountName = result.Intent.AccountName,
                    QrImageUrl = result.Intent.QrImageUrl,
                    Status = result.Intent.Status
                }
        };
}
