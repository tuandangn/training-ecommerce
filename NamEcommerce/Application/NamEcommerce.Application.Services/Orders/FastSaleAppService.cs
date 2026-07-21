using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Customers;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Application.Contracts.Customers;

namespace NamEcommerce.Application.Services.Orders;

public sealed class FastSaleAppService(
    IOrderManager orderManager,
    IDeliveryNoteManager deliveryNoteManager,
    ICustomerDebtManager customerDebtManager,
    IBankTransferPaymentIntentManager paymentIntentManager,
    IInventoryStockManager inventoryStockManager,
    ICustomerAppService customerAppService,
    IEntityDataReader<Product> productReader,
    IEntityDataReader<Customer> customerReader,
    IEntityDataReader<Warehouse> warehouseReader,
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

        var customer = await customerReader.GetByIdAsync(dto.CustomerId, default).ConfigureAwait(false);
        if (customer is null)
            return QuickSaleResultAppDto.CreateError("Error.CustomerIsNotFound");

        // Khách lẻ (tài khoản dùng chung) không được bán chịu — phải thanh toán đủ.
        if (paymentTiming == QuickSalePaymentTiming.Unpaid
            && customer.Kind == CustomerKind.RetailWalkIn && customer.IsSystem)
            return QuickSaleResultAppDto.CreateError("Error.RetailOrderCannotLeaveDebt");

        var warehouseIds = GetWarehouseIdsToValidate(dto, fulfillmentMode);
        foreach (var warehouseId in warehouseIds)
        {
            var warehouse = await warehouseReader.GetByIdAsync(warehouseId, default).ConfigureAwait(false);
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
            var product = await productReader.GetByIdAsync(itemGroup.Key, default).ConfigureAwait(false);
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
        CreateQuickSaleAppDto dto, PaymentMethod? paymentMethod, Guid? paymentIntentId)
    {
        var fulfillmentMode = (QuickSaleFulfillmentMode)dto.FulfillmentMode;
        var paymentTiming = (QuickSalePaymentTiming)dto.PaymentTiming;

        CurrentUserInfoDto? currentUser = null;
        if (paymentTiming == QuickSalePaymentTiming.PayNow)
        {
            currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
            if (currentUser is null)
                return QuickSaleResultAppDto.CreateError("Error.UserRequired");
        }

        var total = CalculateTotal(dto);
        var orderResult = await CreateOrderAsync(dto).ConfigureAwait(false);
        var orderId = orderResult.CreatedId;

        CustomerPaymentDto? payment = null;

        if (fulfillmentMode == QuickSaleFulfillmentMode.DeliverNow)
        {
            var deliveryNote = await CreateAndConfirmDeliveryNoteAsync(dto, orderId, total).ConfigureAwait(false);

            if (paymentTiming == QuickSalePaymentTiming.PayNow)
            {
                payment = await customerDebtManager.RecordPaymentAsync(new CreateCustomerPaymentDto
                {
                    CustomerId = dto.CustomerId,
                    OrderId = orderId,
                    DeliveryNoteId = deliveryNote.Id,
                    CustomerDebtId = null,
                    Amount = total,
                    PaymentMethod = paymentMethod!.Value,
                    PaymentType = PaymentType.DebtPayment,
                    PaidOnUtc = DateTime.UtcNow,
                    RecordedByUserId = currentUser!.Id
                }).ConfigureAwait(false);
            }

            var requestedAtUtc = DateTime.UtcNow;
            await orderManager.RequestQuickSaleDeliveryAsync(orderId, deliveryNote.Id, requestedAtUtc).ConfigureAwait(false);

            if (paymentIntentId.HasValue)
            {
                if (payment is null)
                    throw new InvalidOperationException("Error.CustomerPaymentIsNotFound");
                await paymentIntentManager.ConsumeAsync(
                    paymentIntentId.Value, orderId, deliveryNote.Id, null, payment.Id).ConfigureAwait(false);
            }

            return new QuickSaleResultAppDto
            {
                Success = true,
                OrderId = orderId,
                DeliveryNoteId = deliveryNote.Id,
                CustomerPaymentId = payment?.Id,
                PaymentIntentId = paymentIntentId
            };
        }

        // OrderOnly
        if (paymentTiming == QuickSalePaymentTiming.PayNow)
        {
            payment = await customerDebtManager.RecordPaymentAsync(new CreateCustomerPaymentDto
            {
                CustomerId = dto.CustomerId,
                OrderId = orderId,
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
                paymentIntentId.Value, orderId, null, null, payment.Id).ConfigureAwait(false);
        }

        var createdOrder = await orderManager.GetOrderByIdAsync(orderId).ConfigureAwait(false);
        var orderItems = createdOrder?.Items.Select(item => new QuickSaleOrderItemResultAppDto
        {
            OrderItemId = item.Id,
            ProductId = item.ProductId,
            ProductName = item.ProductName ?? string.Empty,
            Quantity = item.Quantity
        }).ToList() ?? [];

        return new QuickSaleResultAppDto
        {
            Success = true,
            OrderId = orderId,
            CustomerPaymentId = payment?.Id,
            PaymentIntentId = paymentIntentId,
            OrderItems = orderItems
        };
    }

    private async Task<CreateOrderResultDto> CreateOrderAsync(CreateQuickSaleAppDto dto)
    {
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = dto.CustomerId,
            Note = dto.Note,
            OrderDiscount = dto.OrderDiscount,
            ExpectedShippingDateUtc = DateTime.UtcNow.Date,
            ShippingAddress = dto.ShippingAddress,
            ShippingPhoneNumber = dto.ShippingPhoneNumber,
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

    private async Task<DeliveryNoteDto> CreateAndConfirmDeliveryNoteAsync(CreateQuickSaleAppDto dto, Guid orderId, decimal total)
    {
        var order = await orderManager.GetOrderByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new InvalidOperationException("Error.Order.QuickCreateFailed");

        if (order.Items.Count != dto.Items.Count)
            throw new InvalidOperationException("Error.OrderItemMismatch");

        var retailWalkInCustomer = await customerAppService.GetOrCreateRetailWalkInCustomerAsync().ConfigureAwait(false);
        var deliveryNote = await deliveryNoteManager.CreateFromOrderAsync(new CreateDeliveryNoteDto
        {
            OrderId = orderId,
            ShippingAddress = string.IsNullOrEmpty(dto.ShippingAddress) ? retailWalkInCustomer.Address : dto.ShippingAddress,
            ShippingPhoneNumber = string.IsNullOrWhiteSpace(dto.ShippingPhoneNumber) ? order.ShippingPhoneNumber : dto.ShippingPhoneNumber,
            ShowPrice = true,
            Surcharge = 0,
            AmountToCollect = total,
            Items = order.Items.Select((item, index) => new CreateDeliveryNoteItemDto
            {
                OrderItemId = item.Id,
                WarehouseId = ResolveItemWarehouseId(dto.Items[index], dto),
                Quantity = item.Quantity
            }).ToList()
        }).ConfigureAwait(false);

        await deliveryNoteManager.ConfirmAsync(deliveryNote.Id).ConfigureAwait(false);

        return deliveryNote;
    }

    private static decimal CalculateTotal(CreateQuickSaleAppDto dto)
        => dto.Items.Sum(item => item.Quantity * item.UnitPrice) - (dto.OrderDiscount ?? 0);

    private static Guid ResolveItemWarehouseId(QuickSaleItemAppDto item, CreateQuickSaleAppDto dto)
        => item.WarehouseId == Guid.Empty ? dto.WarehouseId : item.WarehouseId;

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
