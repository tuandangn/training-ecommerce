using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.Orders;

public sealed class QuickCreateOrderAppService(
    IOrderManager orderManager,
    IOrderAppService orderAppService,
    IDeliveryNoteManager deliveryNoteManager,
    IDeliveryNoteAppService deliveryNoteAppService,
    ICustomerDebtAppService customerDebtAppService,
    IBankTransferPaymentIntentManager paymentIntentManager,
    IInventoryStockManager inventoryStockManager,
    IEntityDataReader<Product> productReader,
    IEntityDataReader<Customer> customerReader,
    IEntityDataReader<Warehouse> warehouseReader,
    IOrderFulfillmentScheduleAppService orderFulfillmentScheduleAppService,
    ICurrentUserAccessor currentUserAccessor,
    ICurrentUserService currentUserService) : IQuickCreateOrderAppService
{
    public async Task<QuickCreateOrderResultAppDto> QuickCreateOrderAsync(QuickCreateOrderAppDto dto)
    {
        var (success, errorMessage) = await ValidateAsync(dto).ConfigureAwait(false);
        if (!success)
            return QuickCreateOrderResultAppDto.CreateError(errorMessage);

        var customer = await customerReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        if (customer is null)
            return QuickCreateOrderResultAppDto.CreateError("Error.CustomerIsNotFound");

        var createOrderDto = new CreateOrderAppDto
        {
            CustomerId = dto.CustomerId,
            Note = dto.Note,
            OrderDiscount = dto.OrderDiscount,
            ShippingAddress = dto.ShippingAddress,
            ExpectedShippingDateUtc = dto.DeliveryNow ? null : dto.ExpectedShippingDateUtc,
            ShippingPhoneNumber = dto.ShippingPhoneNumber,
            SkipScheduling = dto.DeliveryNow || customer.IsRetailWalkIn(),
            RequiresPayOff = dto.DeliveryNow
        };
        foreach (var item in dto.Items)
        {
            createOrderDto.Items.Add(new CreateOrderAppDto.OrderItemAppDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                WarehouseId = item.WarehouseId
            });
        }
        var orderResult = await orderAppService.CreateOrderAsync(createOrderDto).ConfigureAwait(false);
        if (!orderResult.Success)
            return QuickCreateOrderResultAppDto.CreateError(orderResult.ErrorMessage);

        var createdOrder = await orderManager.GetOrderByIdAsync(orderResult.CreatedId!.Value).ConfigureAwait(false);
        if (createdOrder is null)
            return QuickCreateOrderResultAppDto.CreateError("Error.OrderIsNotFound");

        if (dto.DeliveryNow && createdOrder.CanProcess)
        {
            var deliveryNote = await CreateDeliveryNoteAsync(createdOrder.Id, true).ConfigureAwait(false);
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
            PaymentIntentId = dto.PaymentIntentId,
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

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> ProcessOrderWithoutPaymentAsync(Guid orderId)
    {
        if (!await currentUserService.IsAdminAsync())
            return CommonActionResultDto.CreateError("Error.Unauthorized");

        var order = await orderManager.GetOrderByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(orderId);

        await orderManager.ProcessOrderWithoutPayment(order.Id).ConfigureAwait(false);

        await ProcessOrderAsync(order.Id).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task ProcessOrderAsync(Guid orderId)
    {
        var order = await orderManager.GetOrderByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(orderId);

        if (!order.CanProcess)
            throw new OrderCannotProcessException();

        var deliveryNow = order.PayOffRequired;

        if (!deliveryNow && order.IsRetailWalkInCustomer)
        {
            await orderFulfillmentScheduleAppService.CreateDefaultSchedulesForOrderAsync(order.Id, null).ConfigureAwait(false);
            return;
        }

        if (!deliveryNow)
            return;

        if (order.Items.Any(item => item.IsDelivered))
            throw new OrderCannotProcessException();

        if (order.Items.Any(item => !item.WarehouseId.HasValue))
            throw new OrderCannotProcessException();

        var deliveryNote = await CreateDeliveryNoteAsync(orderId, false).ConfigureAwait(false);

        await deliveryNoteManager.MarkAsOrderIsPaid(deliveryNote.Id).ConfigureAwait(false);
        await deliveryNoteManager.ConfirmAsync(deliveryNote.Id).ConfigureAwait(false);

        await orderManager.RequestDeliveryAsync(order.Id, deliveryNote.Id, DateTime.UtcNow).ConfigureAwait(false);
    }

    private async Task<(bool success, string? errorMessage)> ValidateAsync(QuickCreateOrderAppDto dto)
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

    private async Task<DeliveryNoteDto> CreateDeliveryNoteAsync(Guid orderId, bool confirmDeliveryNote)
    {
        var order = await orderManager.GetOrderByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new InvalidOperationException("Error.Order.QuickCreateFailed");

        var deliveryNote = await deliveryNoteManager.CreateFromOrderAsync(new CreateDeliveryNoteDto
        {
            OrderId = orderId,
            ShippingAddress = order.ShippingAddress ?? string.Empty,
            ShippingPhoneNumber = order.ShippingPhoneNumber,
            ShowPrice = true,
            Surcharge = 0,
            AmountToCollect = Math.Max(0, order.TotalAmount - order.PaidAmount),
            Items = order.Items.Select((item, index) => new CreateDeliveryNoteItemDto
            {
                OrderItemId = item.Id,
                WarehouseId = item.WarehouseId!.Value,
                Quantity = item.Quantity
            }).ToList(),
            Note = order.Note
        }).ConfigureAwait(false);

        if (confirmDeliveryNote)
            await deliveryNoteManager.ConfirmAsync(deliveryNote.Id).ConfigureAwait(false);

        return deliveryNote;
    }
}
