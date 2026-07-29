using NamEcommerce.Application.Contracts.Customers;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Enums.Customers;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.Orders;

public sealed class FastSaleAppService(
    IOrderManager orderManager,
    IOrderAppService orderAppService,
    IDeliveryNoteManager deliveryNoteManager,
    IDeliveryNoteAppService deliveryNoteAppService,
    ICustomerDebtManager customerDebtManager,
    ICustomerDebtAppService customerDebtAppService,
    IBankTransferPaymentIntentManager paymentIntentManager,
    IInventoryStockManager inventoryStockManager,
    ICustomerAppService customerAppService,
    IEntityDataReader<Product> productReader,
    IEntityDataReader<Customer> customerReader,
    IEntityDataReader<Warehouse> warehouseReader,
    ICurrentUserAccessor currentUserAccessor) : IFastSaleAppService
{
    public async Task<QuickCreateOrderResultAppDto> QuickCreateOrderAsync(QuickCreateOrderAppDto2 dto)
    {
        var validateResult = await ValidateQuickCreateAsync(dto).ConfigureAwait(false);
        if (!validateResult.success)
            return QuickCreateOrderResultAppDto.CreateError(validateResult.errorMessage);

        var customer = await customerReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        if (customer is null)
            return QuickCreateOrderResultAppDto.CreateError("Error.CustomerIsNotFound");

        var createOrderDto = new CreateOrderAppDto
        {
            CustomerId = dto.CustomerId,
            Note = dto.Note,
            OrderDiscount = dto.OrderDiscount,
            ShippingAddress = dto.ShippingAddress,
            ShippingPhoneNumber = dto.ShippingPhoneNumber,
            SkipScheduling = dto.DeliveryNow,
            RequiresPayOff = dto.DeliveryNow
        };
        foreach (var item in dto.Items)
        {
            createOrderDto.Items.Add(new CreateOrderAppDto.OrderItemAppDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }
        var orderResult = await orderAppService.CreateOrderAsync(createOrderDto).ConfigureAwait(false);
        if (!orderResult.Success)
            return QuickCreateOrderResultAppDto.CreateError(orderResult.ErrorMessage);

        var createdOrder = await orderManager.GetOrderByIdAsync(orderResult.CreatedId!.Value).ConfigureAwait(false);
        if (createdOrder is null)
            return QuickCreateOrderResultAppDto.CreateError("Error.OrderIsNotFound");

        DeliveryNoteDto? deliveryNote = null;
        if (dto.DeliveryNow)
        {
            deliveryNote = await CreateDeliveryNoteAsync(dto, createdOrder.Id, createdOrder.CanProcess).ConfigureAwait(false);
            if (createdOrder.CanProcess)
                await orderManager.RequestDeliveryAsync(createdOrder.Id, deliveryNote.Id, DateTime.UtcNow).ConfigureAwait(false);
        }
        return new QuickCreateOrderResultAppDto
        {
            Success = true,
            OrderId = createdOrder.Id
        };
    }

    public async Task<CommonActionResultDto> CompleteQuickCreateOrderPaymentAsync(CompleteQuickCreateOrderPaymentAppDto dto)
    {
        var validateResult = dto.Validate();
        if (!validateResult.valid)
            return CommonActionResultDto.CreateError(validateResult.errorMessage);

        var order = await orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            return CommonActionResultDto.CreateError("Error.OrderIsNotFound");

        if (order.ProcessRequiresPayment && order.HadPaid)
            return CommonActionResultDto.CreateError("Error.OrderHadPaid");

        BankTransferPaymentIntentDto? paymentIntent = null;
        if (dto.PaymentIntentId.HasValue)
        {
            paymentIntent = await paymentIntentManager.GetByIdAsync(dto.PaymentIntentId.Value).ConfigureAwait(false);
            if (paymentIntent is null)
            {
                return CommonActionResultDto.CreateError("Error.PaymentIntentIsNotFound");
            }

            if (paymentIntent.CustomerId.HasValue && paymentIntent.CustomerId.Value != order.CustomerId)
                return CommonActionResultDto.CreateError("Error.PaymentIntentCustomerMismatch");

            await paymentIntentManager.ExpireIfPendingAsync(paymentIntent!.Id, DateTime.UtcNow).ConfigureAwait(false);

            if (paymentIntent.Status is BankTransferPaymentIntentStatus.Expired
                or BankTransferPaymentIntentStatus.Cancelled
                or BankTransferPaymentIntentStatus.Consumed)
            {
                return CommonActionResultDto.CreateError("Error.PaymentIntentCannotConsume");
            }

            if (paymentIntent.Status is not BankTransferPaymentIntentStatus.Confirmed and not BankTransferPaymentIntentStatus.ManuallyConfirmed)
                return CommonActionResultDto.CreateError("Error.PaymentIntentIsNotConfirmed");
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        if (currentUser is null)
            return CommonActionResultDto.CreateError("Error.UserRequired");
        var deliveryNoteId = await deliveryNoteAppService.GetWaitingPaymentDeliveryNoteIdAsync(order.Id);

        var payment = await customerDebtAppService.RecordPaymentAsync(new CreateCustomerPaymentAppDto
        {
            CustomerId = order.CustomerId,
            OrderId = order.Id,
            DeliveryNoteId = deliveryNoteId,
            Amount = dto.PaidAmount,
            PaymentMethod = dto.PaymentIntentId.HasValue ? (int)PaymentMethod.BankTransfer : (int)PaymentMethod.Cash,
            PaymentType = deliveryNoteId.HasValue ? (int)PaymentType.DebtPayment : (int)PaymentType.Deposit,
            PaidOnUtc = DateTime.UtcNow,
            RecordedByUserId = currentUser!.Id,
            Note = $"Payment for order {order.Code}"
        }).ConfigureAwait(false);

        if (dto.PaymentIntentId.HasValue)
        {
            if (payment is null)
                return CommonActionResultDto.CreateError("Error.CustomerPaymentIsNotFound");
            await paymentIntentManager.ConsumeAsync(
                dto.PaymentIntentId.Value, order.Id, null, null, payment.Id).ConfigureAwait(false);
        }

        await orderManager.MarkOrderHasPayment(order.Id, dto.PaidAmount, dto.PaymentIntentId).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<QuickSaleResultAppDto> CreateCashQuickSaleAsync(QuickCreateOrderAppDto dto)
    {
        var validation = await ValidateQuickSaleAsync(dto, QuickSalePaymentTiming.PayNow, PaymentMethod.Cash).ConfigureAwait(false);
        if (!validation.Success)
            return validation;

        return await CreateQuickSaleRecordsAsync(dto, PaymentMethod.Cash, null).ConfigureAwait(false);
    }

    public async Task<QuickSaleResultAppDto> CreateBankTransferQuickSaleAsync(QuickCreateOrderAppDto dto, Guid paymentIntentId)
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

        //var total = CalculateTotal(dto);
        //if (intent.Amount != total || intent.Amount != dto.PaidAmount)
        //    return QuickSaleResultAppDto.CreateError("Error.PaymentIntentAmountMismatch");
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

    public async Task<QuickSaleResultAppDto> CreateUnpaidQuickSaleAsync(QuickCreateOrderAppDto dto)
    {
        var validation = await ValidateQuickSaleAsync(dto, QuickSalePaymentTiming.Unpaid, null).ConfigureAwait(false);
        if (!validation.Success)
            return validation;

        return await CreateQuickSaleRecordsAsync(dto, null, null).ConfigureAwait(false);
    }

    private async Task<(bool success, string? errorMessage)> ValidateQuickCreateAsync(QuickCreateOrderAppDto2 dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var validateResult = dto.Validate();
        if (!validateResult.valid)
            return validateResult;

        if (dto.DeliveryNow)
        {
            var warehouseIds = dto.Items.Select(item => item.WarehouseId).OfType<Guid>().Distinct().ToList();
            foreach (var warehouseId in warehouseIds)
            {
                var warehouse = await warehouseReader.GetByIdAsync(warehouseId).ConfigureAwait(false);
                if (warehouse is null)
                    return (false, "Error.WarehouseIsNotFound");
            }
        }

        foreach (var itemGroup in dto.Items.GroupBy(item => item.ProductId))
        {
            var product = await productReader.GetByIdAsync(itemGroup.Key).ConfigureAwait(false);
            if (product is null)
                return (false, "Error.ProductIsNotFound");
        }

        if (!dto.DeliveryNow)
            return (true, string.Empty);

        foreach (var itemGroup in dto.Items.GroupBy(item => new { item.ProductId, item.WarehouseId }))
        {
            var requestedQuantity = itemGroup.Sum(item => item.Quantity);
            var stock = await inventoryStockManager
                .GetInventoryStockForProductAsync(itemGroup.Key.ProductId, itemGroup.Key.WarehouseId!.Value)
                .ConfigureAwait(false);
            if (stock is null || stock.QuantityOnHand < requestedQuantity)
                return (false, "Error.ProductInsufficientStock");
        }

        return (true, string.Empty);
    }

    private async Task<QuickSaleResultAppDto> ValidateQuickSaleAsync(
        QuickCreateOrderAppDto dto,
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

        // Khách lẻ (tài khoản dùng chung) không được bán chịu — phải thanh toán đủ.
        if (paymentTiming == QuickSalePaymentTiming.Unpaid
            && customer.Kind == CustomerKind.RetailWalkIn && customer.IsSystem)
            return QuickSaleResultAppDto.CreateError("Error.RetailOrderCannotLeaveDebt");

        if (dto.FulfillmentMode == (int)QuickSaleFulfillmentMode.DeliverNow)
        {
            var warehouseIds = dto.Items.Select(item => item.WarehouseId).Distinct().ToList();
            foreach (var warehouseId in warehouseIds)
            {
                var warehouse = await warehouseReader.GetByIdAsync(warehouseId).ConfigureAwait(false);
                if (warehouse is null)
                    return QuickSaleResultAppDto.CreateError("Error.WarehouseIsNotFound");
            }
        }

        var total = CalculateTotal(dto);
        if (total <= 0)
            return QuickSaleResultAppDto.CreateError("Error.TotalAmountMustBePositive");
        if (customer.Kind == CustomerKind.RetailWalkIn && paymentTiming == QuickSalePaymentTiming.PayNow
            && fulfillmentMode == QuickSaleFulfillmentMode.DeliverNow && dto.PaidAmount != total)
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

        foreach (var itemGroup in dto.Items.GroupBy(item => new { item.ProductId, WarehouseId = item.WarehouseId }))
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
        QuickCreateOrderAppDto dto, PaymentMethod? paymentMethod, Guid? paymentIntentId)
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
                    Amount = dto.PaidAmount,
                    PaymentMethod = paymentMethod!.Value,
                    PaymentType = PaymentType.DebtPayment,
                    PaidOnUtc = DateTime.UtcNow,
                    RecordedByUserId = currentUser!.Id
                }).ConfigureAwait(false);
            }

            var requestedAtUtc = DateTime.UtcNow;
            await orderManager.RequestDeliveryAsync(orderId, deliveryNote.Id, requestedAtUtc).ConfigureAwait(false);

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
                Amount = dto.PaidAmount,
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

    private async Task<CreateOrderResultDto> CreateOrderAsync(QuickCreateOrderAppDto dto)
    {
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = dto.CustomerId,
            Note = dto.Note,
            OrderDiscount = dto.OrderDiscount,
            ExpectedShippingDateUtc = DateTime.UtcNow.Date,
            ShippingAddress = dto.ShippingAddress,
            ShippingPhoneNumber = dto.ShippingPhoneNumber
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

    private async Task<DeliveryNoteDto> CreateDeliveryNoteAsync(QuickCreateOrderAppDto2 dto, Guid orderId, bool confirmDeliveryNote)
    {
        var order = await orderManager.GetOrderByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new InvalidOperationException("Error.Order.QuickCreateFailed");

        if (order.Items.Count != dto.Items.Count)
            throw new InvalidOperationException("Error.OrderItemMismatch");

        var deliveryNote = await deliveryNoteManager.CreateFromOrderAsync(new CreateDeliveryNoteDto
        {
            OrderId = orderId,
            ShippingAddress = order.ShippingAddress ?? string.Empty,
            ShippingPhoneNumber = order.ShippingPhoneNumber,
            ShowPrice = true,
            Surcharge = 0,
            AmountToCollect = order.TotalAmount,
            Items = order.Items.Select((item, index) => new CreateDeliveryNoteItemDto
            {
                OrderItemId = item.Id,
                WarehouseId = dto.Items[index].WarehouseId!.Value,
                Quantity = item.Quantity
            }).ToList(),
            Note = order.Note
        }).ConfigureAwait(false);

        if (confirmDeliveryNote)
            await deliveryNoteManager.ConfirmAsync(deliveryNote.Id).ConfigureAwait(false);

        return deliveryNote;
    }

    private async Task<DeliveryNoteDto> CreateAndConfirmDeliveryNoteAsync(QuickCreateOrderAppDto dto, Guid orderId, decimal total)
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
                WarehouseId = dto.Items[index].WarehouseId,
                Quantity = item.Quantity
            }).ToList()
        }).ConfigureAwait(false);

        await deliveryNoteManager.ConfirmAsync(deliveryNote.Id).ConfigureAwait(false);

        return deliveryNote;
    }

    private static decimal CalculateTotal(QuickCreateOrderAppDto dto)
        => dto.Items.Sum(item => item.Quantity * item.UnitPrice) - (dto.OrderDiscount ?? 0);

}
