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
        var validation = await ValidateQuickSaleAsync(dto, QuickSalePaymentTiming.PayNow, PaymentMethod.Cash).ConfigureAwait(false);
        if (!validation.Success)
            return validation;

        return await CreateQuickSaleRecordsAsync(dto, PaymentMethod.Cash, null).ConfigureAwait(false);
    }

    public async Task<QuickSaleResultAppDto> CreateBankTransferQuickSaleAsync(CreateQuickSaleAppDto dto, Guid paymentIntentId)
    {
        var validation = await ValidateQuickSaleAsync(dto, QuickSalePaymentTiming.PayNow, PaymentMethod.BankTransfer).ConfigureAwait(false);
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

    public async Task<QuickSaleResultAppDto> CreateUnpaidQuickSaleAsync(CreateQuickSaleAppDto dto)
    {
        var validation = await ValidateQuickSaleAsync(dto, QuickSalePaymentTiming.Unpaid, null).ConfigureAwait(false);
        if (!validation.Success)
            return validation;

        return await CreateQuickSaleRecordsAsync(dto, null, null).ConfigureAwait(false);
    }

    private async Task<QuickSaleResultAppDto> ValidateQuickSaleAsync(
        CreateQuickSaleAppDto dto,
        QuickSalePaymentTiming expectedPaymentTiming,
        PaymentMethod? expectedPaymentMethod)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return QuickSaleResultAppDto.CreateError(errorMessage);

        var fulfillmentMode = (QuickSaleFulfillmentMode)dto.FulfillmentMode;
        var paymentTiming = (QuickSalePaymentTiming)dto.PaymentTiming;
        if (paymentTiming != expectedPaymentTiming)
            return QuickSaleResultAppDto.CreateError("Error.FastSalePaymentTimingInvalid");

        if (expectedPaymentMethod.HasValue && (PaymentMethod)dto.PaymentMethod != expectedPaymentMethod.Value)
            return QuickSaleResultAppDto.CreateError("Error.PaymentMethodInvalid");

        var customer = await customerReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        if (customer is null)
            return QuickSaleResultAppDto.CreateError("Error.CustomerIsNotFound");

        var warehouseIds = GetWarehouseIdsToValidate(dto, fulfillmentMode);
        foreach (var warehouseId in warehouseIds)
        {
            var warehouse = await warehouseReader.GetByIdAsync(warehouseId).ConfigureAwait(false);
            if (warehouse is null)
                return QuickSaleResultAppDto.CreateError("Error.WarehouseIsNotFound");
        }

        var total = CalculateTotal(dto);
        if (total <= 0)
            return QuickSaleResultAppDto.CreateError("Error.TotalAmountMustBePositive");
        if (paymentTiming == QuickSalePaymentTiming.PayNow && dto.PaidAmount != total)
            return QuickSaleResultAppDto.CreateError("Error.PaymentAmountMustEqualSaleTotal");
        if (paymentTiming == QuickSalePaymentTiming.Unpaid && dto.PaidAmount != 0)
            return QuickSaleResultAppDto.CreateError("Error.PaymentAmountMustBeZeroWhenUnpaid");

        foreach (var itemGroup in dto.Items.GroupBy(item => item.ProductId))
        {
            var product = await productReader.GetByIdAsync(itemGroup.Key).ConfigureAwait(false);
            if (product is null)
                return QuickSaleResultAppDto.CreateError("Error.ProductIsNotFound");
        }

        if (fulfillmentMode != QuickSaleFulfillmentMode.DeliverNow)
            return new QuickSaleResultAppDto { Success = true };

        foreach (var itemGroup in dto.Items.GroupBy(item => new { item.ProductId, WarehouseId = ResolveItemWarehouseId(item, dto) }))
        {
            if (itemGroup.Key.WarehouseId == Guid.Empty)
                return QuickSaleResultAppDto.CreateError("Error.WarehouseRequired");

            var requestedQuantity = itemGroup.Sum(item => item.Quantity);
            var stock = await inventoryStockManager
                .GetInventoryStockForProductAsync(itemGroup.Key.ProductId, itemGroup.Key.WarehouseId)
                .ConfigureAwait(false);
            if (stock is null || stock.QuantityAvailable < requestedQuantity)
                return QuickSaleResultAppDto.CreateError("Error.ProductInsufficientStock");
        }

        return new QuickSaleResultAppDto { Success = true };
    }

    private async Task<QuickSaleResultAppDto> CreateQuickSaleRecordsAsync(
        CreateQuickSaleAppDto dto,
        PaymentMethod? paymentMethod,
        Guid? paymentIntentId)
    {
        var fulfillmentMode = (QuickSaleFulfillmentMode)dto.FulfillmentMode;
        var paymentTiming = (QuickSalePaymentTiming)dto.PaymentTiming;
        var currentUser = paymentTiming == QuickSalePaymentTiming.PayNow
            ? await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false)
            : null;
        if (paymentTiming == QuickSalePaymentTiming.PayNow && currentUser is null)
            return QuickSaleResultAppDto.CreateError("Error.UserRequired");

        await using var transaction = await dbContext.BeginTransactionAsync().ConfigureAwait(false);
        try
        {
            var total = CalculateTotal(dto);
            var orderResult = await CreateOrderAsync(dto).ConfigureAwait(false);
            var order = await orderManager.GetOrderByIdAsync(orderResult.CreatedId).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Error.OrderIsNotFound");

            DeliveryNoteDto? deliveryNote = null;
            CustomerDebt? debt = null;
            CustomerPaymentDto? payment = null;

            if (fulfillmentMode == QuickSaleFulfillmentMode.DeliverNow)
            {
                deliveryNote = await CreateAndCompleteDeliveryNoteAsync(dto, order, total).ConfigureAwait(false);
                debt = customerDebtReader.DataSource.FirstOrDefault(x => x.DeliveryNoteId == deliveryNote.Id)
                    ?? throw new InvalidOperationException("Error.CustomerDebtIsNotFound");

                if (paymentTiming == QuickSalePaymentTiming.PayNow)
                {
                    payment = await customerDebtManager.RecordPaymentAsync(new CreateCustomerPaymentDto
                    {
                        CustomerId = dto.CustomerId,
                        OrderId = order.Id,
                        DeliveryNoteId = deliveryNote.Id,
                        CustomerDebtId = debt.Id,
                        Amount = total,
                        PaymentMethod = paymentMethod!.Value,
                        PaymentType = PaymentType.DebtPayment,
                        PaidOnUtc = DateTime.UtcNow,
                        RecordedByUserId = currentUser!.Id
                    }).ConfigureAwait(false);
                }

                await orderManager.CompleteOrderAsync(new CompleteOrderDto
                {
                    OrderId = order.Id
                }).ConfigureAwait(false);
            }
            else if (paymentTiming == QuickSalePaymentTiming.PayNow)
            {
                payment = await customerDebtManager.RecordPaymentAsync(new CreateCustomerPaymentDto
                {
                    CustomerId = dto.CustomerId,
                    OrderId = order.Id,
                    Amount = total,
                    PaymentMethod = paymentMethod!.Value,
                    PaymentType = PaymentType.Deposit,
                    PaidOnUtc = DateTime.UtcNow,
                    RecordedByUserId = currentUser!.Id
                }).ConfigureAwait(false);
            }

            if (paymentIntentId.HasValue)
            {
                if (payment is null)
                    throw new InvalidOperationException("Error.CustomerPaymentIsNotFound");

                await paymentIntentManager.ConsumeAsync(
                    paymentIntentId.Value,
                    order.Id,
                    deliveryNote?.Id,
                    debt?.Id,
                    payment.Id).ConfigureAwait(false);
            }

            await transaction.CommitAsync().ConfigureAwait(false);

            return new QuickSaleResultAppDto
            {
                Success = true,
                OrderId = order.Id,
                DeliveryNoteId = deliveryNote?.Id,
                CustomerDebtId = debt?.Id,
                CustomerPaymentId = payment?.Id,
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
            Note = dto.Note,
            OrderDiscount = dto.OrderDiscount,
            ExpectedShippingDateUtc = DateTime.UtcNow.Date,
            ShippingAddress = string.Empty,
            RequireAvailableStock = (QuickSaleFulfillmentMode)dto.FulfillmentMode == QuickSaleFulfillmentMode.DeliverNow
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
        var orderItems = order.Items.ToList();
        var quickSaleItems = dto.Items.ToList();
        if (orderItems.Count != quickSaleItems.Count)
            throw new InvalidOperationException("Error.OrderItemMismatch");

        var deliveryNote = await deliveryNoteManager.CreateFromOrderAsync(new CreateDeliveryNoteDto
        {
            OrderId = order.Id,
            WarehouseId = ResolveHeaderWarehouseId(dto),
            ShippingAddress = string.IsNullOrWhiteSpace(order.ShippingAddress) ? "Tai quay" : order.ShippingAddress,
            ShowPrice = true,
            CompensateReturnedQuantityInNextDelivery = false,
            Surcharge = 0,
            AmountToCollect = total,
            Items = orderItems.Select((item, index) => new CreateDeliveryNoteItemDto
            {
                OrderItemId = item.Id,
                WarehouseId = ResolveItemWarehouseId(quickSaleItems[index], dto),
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

    private static Guid ResolveItemWarehouseId(QuickSaleItemAppDto item, CreateQuickSaleAppDto dto)
        => item.WarehouseId == Guid.Empty ? dto.WarehouseId : item.WarehouseId;

    private static Guid ResolveHeaderWarehouseId(CreateQuickSaleAppDto dto)
        => dto.Items.Select(item => ResolveItemWarehouseId(item, dto)).FirstOrDefault(id => id != Guid.Empty);

    private static IReadOnlyCollection<Guid> GetWarehouseIdsToValidate(
        CreateQuickSaleAppDto dto,
        QuickSaleFulfillmentMode fulfillmentMode)
    {
        if (fulfillmentMode != QuickSaleFulfillmentMode.DeliverNow)
            return dto.WarehouseId == Guid.Empty ? [] : [dto.WarehouseId];

        return dto.Items
            .Select(item => ResolveItemWarehouseId(item, dto))
            .Where(id => id != Guid.Empty)
            .Append(dto.WarehouseId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
    }
}
