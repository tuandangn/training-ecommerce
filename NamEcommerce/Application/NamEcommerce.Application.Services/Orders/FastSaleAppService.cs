using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.Orders;

public sealed class FastSaleAppService(
    IDbContext dbContext,
    IOrderManager orderManager,
    IDeliveryNoteManager deliveryNoteManager,
    ICustomerDebtManager customerDebtManager,
    IBankTransferPaymentIntentManager paymentIntentManager,
    IInventoryStockManager inventoryStockManager,
    IEntityDataReader<Product> productReader,
    IEntityDataReader<Customer> customerReader,
    IEntityDataReader<Warehouse> warehouseReader,
    IEntityDataReader<CustomerDebt> customerDebtReader,
    ICurrentUserAccessor currentUserAccessor) : IFastSaleAppService
{
    public async Task<QuickSaleResultAppDto> CreateCashQuickSaleAsync(CreateQuickSaleAppDto dto)
    {
        var validation = await ValidateQuickSaleAsync(dto, PaymentMethod.Cash).ConfigureAwait(false);
        if (!validation.Success)
            return validation;

        return await CreateQuickSaleRecordsAsync(dto, PaymentMethod.Cash, null).ConfigureAwait(false);
    }

    public async Task<QuickSaleResultAppDto> CreateBankTransferQuickSaleAsync(CreateQuickSaleAppDto dto, Guid paymentIntentId)
    {
        var validation = await ValidateQuickSaleAsync(dto, PaymentMethod.BankTransfer).ConfigureAwait(false);
        if (!validation.Success)
            return validation;

        BankTransferPaymentIntentDto intent;
        try
        {
            intent = await paymentIntentManager.ExpireIfPendingAsync(paymentIntentId, DateTime.UtcNow).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return QuickSaleResultAppDto.CreateError(ex.Message);
        }

        if (intent.CustomerId.HasValue && intent.CustomerId.Value != dto.CustomerId)
            return QuickSaleResultAppDto.CreateError("Error.PaymentIntentCustomerMismatch");

        var total = CalculateTotal(dto);
        if (intent.Amount != total || intent.Amount != dto.PaidAmount)
            return QuickSaleResultAppDto.CreateError("Error.PaymentIntentAmountMismatch");
        if (intent.Status is BankTransferPaymentIntentStatus.Expired
            or BankTransferPaymentIntentStatus.Cancelled
            or BankTransferPaymentIntentStatus.Consumed)
        {
            return QuickSaleResultAppDto.CreateError("Error.PaymentIntentCannotConsume");
        }
        if (intent.Status is not BankTransferPaymentIntentStatus.Confirmed and not BankTransferPaymentIntentStatus.ManuallyConfirmed)
            return QuickSaleResultAppDto.CreateError("Error.PaymentIntentIsNotConfirmed");

        return await CreateQuickSaleRecordsAsync(dto, PaymentMethod.BankTransfer, paymentIntentId).ConfigureAwait(false);
    }

    private async Task<QuickSaleResultAppDto> ValidateQuickSaleAsync(CreateQuickSaleAppDto dto, PaymentMethod expectedPaymentMethod)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return QuickSaleResultAppDto.CreateError(errorMessage);

        if ((PaymentMethod)dto.PaymentMethod != expectedPaymentMethod)
            return QuickSaleResultAppDto.CreateError("Error.PaymentMethodInvalid");

        var customer = await customerReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        if (customer is null)
            return QuickSaleResultAppDto.CreateError("Error.CustomerIsNotFound");

        var warehouse = await warehouseReader.GetByIdAsync(dto.WarehouseId).ConfigureAwait(false);
        if (warehouse is null)
            return QuickSaleResultAppDto.CreateError("Error.WarehouseIsNotFound");

        var total = CalculateTotal(dto);
        if (total <= 0)
            return QuickSaleResultAppDto.CreateError("Error.TotalAmountMustBePositive");
        if (dto.PaidAmount != total)
            return QuickSaleResultAppDto.CreateError("Error.PaymentAmountMustEqualSaleTotal");

        foreach (var itemGroup in dto.Items.GroupBy(item => item.ProductId))
        {
            var product = await productReader.GetByIdAsync(itemGroup.Key).ConfigureAwait(false);
            if (product is null)
                return QuickSaleResultAppDto.CreateError("Error.ProductIsNotFound");

            var requestedQuantity = itemGroup.Sum(item => item.Quantity);
            var stock = await inventoryStockManager
                .GetInventoryStockForProductAsync(itemGroup.Key, dto.WarehouseId)
                .ConfigureAwait(false);
            if (stock is null || stock.QuantityAvailable < requestedQuantity)
                return QuickSaleResultAppDto.CreateError("Error.ProductInsufficientStock");
        }

        return new QuickSaleResultAppDto { Success = true };
    }

    private async Task<QuickSaleResultAppDto> CreateQuickSaleRecordsAsync(
        CreateQuickSaleAppDto dto,
        PaymentMethod paymentMethod,
        Guid? paymentIntentId)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        if (currentUser is null)
            return QuickSaleResultAppDto.CreateError("Error.UserRequired");

        await using var transaction = await dbContext.BeginTransactionAsync().ConfigureAwait(false);
        try
        {
            var total = CalculateTotal(dto);
            var orderResult = await CreateOrderAsync(dto).ConfigureAwait(false);
            var order = await orderManager.GetOrderByIdAsync(orderResult.CreatedId).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Error.OrderIsNotFound");

            var deliveryNote = await CreateAndCompleteDeliveryNoteAsync(dto, order, total).ConfigureAwait(false);
            var debt = customerDebtReader.DataSource.FirstOrDefault(x => x.DeliveryNoteId == deliveryNote.Id)
                ?? throw new InvalidOperationException("Error.CustomerDebtIsNotFound");

            var payment = await customerDebtManager.RecordPaymentAsync(new CreateCustomerPaymentDto
            {
                CustomerId = dto.CustomerId,
                OrderId = order.Id,
                DeliveryNoteId = deliveryNote.Id,
                CustomerDebtId = debt.Id,
                Amount = total,
                PaymentMethod = paymentMethod,
                PaymentType = PaymentType.DebtPayment,
                PaidOnUtc = DateTime.UtcNow,
                RecordedByUserId = currentUser.Id,
                Note = BuildPaymentNote(paymentMethod, paymentIntentId, dto.Note)
            }).ConfigureAwait(false);

            await orderManager.CompleteOrderAsync(new CompleteOrderDto
            {
                OrderId = order.Id
            }).ConfigureAwait(false);

            if (paymentIntentId.HasValue)
            {
                await paymentIntentManager.ConsumeAsync(
                    paymentIntentId.Value,
                    order.Id,
                    deliveryNote.Id,
                    debt.Id,
                    payment.Id).ConfigureAwait(false);
            }

            await transaction.CommitAsync().ConfigureAwait(false);

            return new QuickSaleResultAppDto
            {
                Success = true,
                OrderId = order.Id,
                DeliveryNoteId = deliveryNote.Id,
                CustomerDebtId = debt.Id,
                CustomerPaymentId = payment.Id,
                PaymentIntentId = paymentIntentId
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            return QuickSaleResultAppDto.CreateError(ex.Message);
        }
    }

    private async Task<CreateOrderResultDto> CreateOrderAsync(CreateQuickSaleAppDto dto)
    {
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = dto.CustomerId,
            Note = BuildOrderNote(dto.Note),
            OrderDiscount = dto.OrderDiscount,
            ExpectedShippingDateUtc = DateTime.UtcNow.Date,
            ShippingAddress = string.Empty
        };

        foreach (var item in dto.Items)
        {
            createOrderDto.Items.Add(new AddOrderItemDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        return await orderManager.CreateOrderAsync(createOrderDto).ConfigureAwait(false);
    }

    private async Task<DeliveryNoteDto> CreateAndCompleteDeliveryNoteAsync(CreateQuickSaleAppDto dto, OrderDto order, decimal total)
    {
        var deliveryNote = await deliveryNoteManager.CreateFromOrderAsync(new CreateDeliveryNoteDto
        {
            OrderId = order.Id,
            WarehouseId = dto.WarehouseId,
            ShippingAddress = string.IsNullOrWhiteSpace(order.ShippingAddress) ? "Tai quay" : order.ShippingAddress,
            ShowPrice = true,
            CompensateReturnedQuantityInNextDelivery = false,
            Note = BuildDeliveryNote(dto.Note),
            Surcharge = 0,
            AmountToCollect = total,
            Items = order.Items.Select(item => new CreateDeliveryNoteItemDto
            {
                OrderItemId = item.Id,
                WarehouseId = dto.WarehouseId,
                Quantity = item.Quantity
            }).ToList()
        }).ConfigureAwait(false);

        await deliveryNoteManager.ConfirmAsync(deliveryNote.Id).ConfigureAwait(false);
        await deliveryNoteManager
            .MarkReceivedByCustomerAsync(deliveryNote.Id, DateTime.UtcNow, null, "Fast sale at counter")
            .ConfigureAwait(false);

        return await deliveryNoteManager.GetByIdAsync(deliveryNote.Id).ConfigureAwait(false)
            ?? deliveryNote;
    }

    private static decimal CalculateTotal(CreateQuickSaleAppDto dto)
        => dto.Items.Sum(item => item.Quantity * item.UnitPrice) - (dto.OrderDiscount ?? 0);

    private static string BuildOrderNote(string? note)
        => string.IsNullOrWhiteSpace(note) ? "Fast sale at counter" : $"Fast sale at counter. {note.Trim()}";

    private static string BuildDeliveryNote(string? note)
        => string.IsNullOrWhiteSpace(note) ? "Fast sale at counter" : note.Trim();

    private static string BuildPaymentNote(PaymentMethod paymentMethod, Guid? paymentIntentId, string? note)
    {
        var prefix = paymentMethod == PaymentMethod.BankTransfer
            ? $"Fast sale bank transfer intent {paymentIntentId}"
            : "Fast sale cash payment";

        return string.IsNullOrWhiteSpace(note) ? prefix : $"{prefix}. {note.Trim()}";
    }
}
