using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.CustomerPortal;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;

namespace NamEcommerce.Application.Services.CustomerPortal;

public sealed class CustomerPortalAppService(
    ICustomerPortalSecurityManager securityManager,
    ICustomerPortalManager customerPortalManager,
    IDeliveryNoteAppService deliveryNoteAppService,
    IDeliveryNoteManager deliveryNoteManager,
    ICustomerDebtAppService customerDebtAppService,
    IEntityDataReader<Order> orderReader,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IEntityDataReader<Product> productReader) : ICustomerPortalAppService
{
    public async Task<PublicDeliveryNoteAppDto?> GetPublicDeliveryNoteByTokenAsync(string token)
    {
        var tokenDto = await securityManager.ResolveDeliveryNoteAccessTokenAsync(CustomerPortalHashing.Hash(token), DateTime.UtcNow).ConfigureAwait(false);
        if (tokenDto is null)
            return null;

        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(tokenDto.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            return null;

        await securityManager.MarkDeliveryNoteAccessTokenViewedAsync(tokenDto.Id, DateTime.UtcNow).ConfigureAwait(false);

        return new PublicDeliveryNoteAppDto
        {
            Id = deliveryNote.Id,
            Code = deliveryNote.Code,
            OrderCode = deliveryNote.OrderCode,
            Status = deliveryNote.Status,
            DeliveryConfirmationStatus = deliveryNote.DeliveryConfirmationStatus,
            CreatedOnUtc = deliveryNote.CreatedOnUtc,
            DeliveredOnUtc = deliveryNote.DeliveredOnUtc,
            Items = deliveryNote.Items.Select(item => new PublicDeliveryNoteItemAppDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity
            }).ToList()
        };
    }

    public async Task<CustomerDashboardAppDto> GetDashboardAsync(Guid customerId)
        => new()
        {
            RecentOrders = (await GetOrdersAsync(customerId).ConfigureAwait(false)).Take(5).ToList(),
            RecentDeliveryNotes = (await GetDeliveryNotesAsync(customerId).ConfigureAwait(false)).Take(5).ToList(),
            DebtSummary = await GetDebtSummaryAsync(customerId).ConfigureAwait(false)
        };

    public Task<IReadOnlyCollection<CustomerOrderSummaryAppDto>> GetOrdersAsync(Guid customerId)
    {
        var orders = orderReader.DataSource
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.CreatedOnUtc)
            .ToList()
            .Select(MapOrderSummary)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerOrderSummaryAppDto>>(orders);
    }

    public Task<CustomerOrderDetailsAppDto?> GetOrderDetailsAsync(Guid customerId, Guid orderId)
    {
        var order = orderReader.DataSource.FirstOrDefault(order => order.Id == orderId && order.CustomerId == customerId);
        if (order is null)
            return Task.FromResult<CustomerOrderDetailsAppDto?>(null);

        var productNames = productReader.DataSource
            .Where(product => order.OrderItems.Select(item => item.ProductId).Contains(product.Id))
            .ToDictionary(product => product.Id, product => product.Name);

        return Task.FromResult<CustomerOrderDetailsAppDto?>(new CustomerOrderDetailsAppDto
        {
            Id = order.Id,
            Code = order.Code,
            Status = (int)order.OrderStatus,
            TotalAmount = order.OrderTotal,
            CreatedOnUtc = order.CreatedOnUtc,
            ExpectedShippingDateUtc = order.ExpectedShippingDateUtc,
            ShippingAddress = order.ShippingAddress,
            Note = order.Note,
            Items = order.OrderItems.Select(item => new CustomerOrderItemAppDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = productNames.GetValueOrDefault(item.ProductId) ?? string.Empty,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SubTotal = item.SubTotal
            }).ToList()
        });
    }

    public async Task<CustomerOrderRequestAppDto> CreateOrderRequestAsync(Guid customerId, CreateCustomerOrderRequestAppDto dto)
    {
        var productIds = dto.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = productReader.DataSource
            .Where(product => productIds.Contains(product.Id))
            .ToDictionary(product => product.Id);
        if (products.Count != productIds.Count)
            throw new InvalidOperationException("Order request contains unknown products.");

        var domainDto = new CreateCustomerOrderRequestDto
        {
            CustomerId = customerId,
            ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc,
            ShippingAddress = dto.ShippingAddress,
            Note = dto.Note,
            Items = dto.Items.Select(item =>
            {
                var product = products.GetValueOrDefault(item.ProductId);
                return new CreateCustomerOrderRequestItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = product?.Name ?? string.Empty,
                    Quantity = item.Quantity,
                    UnitPriceSnapshot = product?.UnitPrice ?? 0
                };
            }).ToList()
        };

        var created = await customerPortalManager.CreateOrderRequestAsync(domainDto).ConfigureAwait(false);
        return new CustomerOrderRequestAppDto
        {
            Id = created.Id,
            Code = created.Code,
            Status = (int)created.Status,
            CreatedOnUtc = created.CreatedOnUtc
        };
    }

    public Task<IReadOnlyCollection<CustomerDeliveryNoteSummaryAppDto>> GetDeliveryNotesAsync(Guid customerId)
    {
        var notes = deliveryNoteReader.DataSource
            .Where(note => note.CustomerId == customerId)
            .OrderByDescending(note => note.CreatedOnUtc)
            .ToList()
            .Select(MapDeliveryNoteSummary)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerDeliveryNoteSummaryAppDto>>(notes);
    }

    public Task<CustomerDeliveryNoteDetailsAppDto?> GetDeliveryNoteDetailsAsync(Guid customerId, Guid deliveryNoteId)
    {
        var deliveryNote = deliveryNoteReader.DataSource.FirstOrDefault(note => note.Id == deliveryNoteId && note.CustomerId == customerId);
        if (deliveryNote is null)
            return Task.FromResult<CustomerDeliveryNoteDetailsAppDto?>(null);

        return Task.FromResult<CustomerDeliveryNoteDetailsAppDto?>(new CustomerDeliveryNoteDetailsAppDto
        {
            Id = deliveryNote.Id,
            Code = deliveryNote.Code,
            OrderCode = deliveryNote.OrderCode,
            Status = (int)deliveryNote.Status,
            DeliveryConfirmationStatus = (int)deliveryNote.DeliveryConfirmationStatus,
            CreatedOnUtc = deliveryNote.CreatedOnUtc,
            DeliveredOnUtc = deliveryNote.DeliveredOnUtc,
            Items = deliveryNote.Items.Select(item => new CustomerDeliveryNoteItemAppDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName ?? string.Empty,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SubTotal = item.SubTotal
            }).ToList()
        });
    }

    public async Task<CustomerActionResultAppDto> ConfirmDeliveryNoteAsync(Guid customerId, Guid deliveryNoteId, ConfirmCustomerDeliveryNoteAppDto dto)
    {
        var deliveryNote = deliveryNoteReader.DataSource.FirstOrDefault(note => note.Id == deliveryNoteId && note.CustomerId == customerId);
        if (deliveryNote is null)
            return CustomerActionResultAppDto.Fail("Không tìm thấy phiếu giao hàng.");

        await deliveryNoteManager.MarkReceivedByCustomerAsync(deliveryNote.Id, DateTime.UtcNow, dto.ReceiverName, dto.Note).ConfigureAwait(false);

        await customerPortalManager.CreateDeliveryFeedbackAsync(new CreateCustomerDeliveryFeedbackDto
        {
            CustomerId = customerId,
            DeliveryNoteId = deliveryNoteId,
            Message = BuildConfirmationMessage(dto)
        }).ConfigureAwait(false);

        return CustomerActionResultAppDto.Ok("Đã ghi nhận khách đã nhận hàng.");
    }

    public async Task<CustomerActionResultAppDto> CreateDeliveryFeedbackAsync(Guid customerId, CreateCustomerDeliveryFeedbackAppDto dto)
    {
        var ownsDeliveryNote = deliveryNoteReader.DataSource.Any(note => note.Id == dto.DeliveryNoteId && note.CustomerId == customerId);
        if (!ownsDeliveryNote)
            return CustomerActionResultAppDto.Fail("Không tìm thấy phiếu giao hàng.");

        await customerPortalManager.CreateDeliveryFeedbackAsync(new CreateCustomerDeliveryFeedbackDto
        {
            CustomerId = customerId,
            DeliveryNoteId = dto.DeliveryNoteId,
            Rating = dto.Rating,
            Message = dto.Message
        }).ConfigureAwait(false);

        return CustomerActionResultAppDto.Ok("Đã ghi nhận phản hồi.");
    }

    public async Task<CustomerReturnRequestAppDto> CreateReturnRequestAsync(Guid customerId, CreateCustomerReturnRequestAppDto dto)
    {
        var deliveryNote = deliveryNoteReader.DataSource.FirstOrDefault(note => note.Id == dto.DeliveryNoteId && note.CustomerId == customerId);
        if (deliveryNote is null)
            throw new InvalidOperationException("Delivery note was not found.");

        var itemsById = deliveryNote.Items.ToDictionary(item => item.Id);
        foreach (var item in dto.Items)
        {
            var deliveryItem = itemsById.GetValueOrDefault(item.DeliveryNoteItemId);
            if (deliveryItem is null || item.RequestedQuantity > deliveryItem.Quantity)
                throw new InvalidOperationException("Return request item is invalid.");
        }

        var request = new CreateCustomerReturnRequestDto
        {
            CustomerId = customerId,
            DeliveryNoteId = dto.DeliveryNoteId,
            Reason = dto.Reason,
            Items = dto.Items.Select(item =>
            {
                var deliveryItem = itemsById.GetValueOrDefault(item.DeliveryNoteItemId);
                return new CreateCustomerReturnRequestItemDto
                {
                    DeliveryNoteItemId = item.DeliveryNoteItemId,
                    ProductId = deliveryItem?.ProductId ?? Guid.Empty,
                    ProductName = deliveryItem?.ProductName ?? string.Empty,
                    RequestedQuantity = item.RequestedQuantity,
                    Reason = item.Reason
                };
            }).ToList()
        };

        var created = await customerPortalManager.CreateReturnRequestAsync(request).ConfigureAwait(false);
        return new CustomerReturnRequestAppDto
        {
            Id = created.Id,
            DeliveryNoteId = created.DeliveryNoteId,
            Status = (int)created.Status,
            CreatedOnUtc = created.CreatedOnUtc
        };
    }

    public async Task<CustomerDebtSummaryPortalAppDto> GetDebtSummaryAsync(Guid customerId)
    {
        var debts = await customerDebtAppService.GetDebtsByCustomerIdAsync(customerId).ConfigureAwait(false);
        if (debts is null)
            return new CustomerDebtSummaryPortalAppDto();

        return new CustomerDebtSummaryPortalAppDto
        {
            TotalDebtAmount = debts.TotalDebtAmount,
            TotalPaidAmount = debts.TotalPaidAmount,
            TotalRemainingAmount = debts.TotalRemainingAmount,
            DepositBalance = debts.DepositBalance,
            Debts = debts.Debts.Select(debt => new CustomerDebtPortalAppDto
            {
                Id = debt.Id,
                Code = debt.Code,
                OrderCode = debt.OrderCode,
                DeliveryNoteCode = debt.DeliveryNoteCode,
                TotalAmount = debt.TotalAmount,
                PaidAmount = debt.PaidAmount,
                RemainingAmount = debt.RemainingAmount,
                Status = debt.Status,
                DueDateUtc = debt.DueDateUtc
            }).ToList(),
            RecentPayments = debts.RecentPayments.Select(payment => new CustomerPaymentPortalAppDto
            {
                Id = payment.Id,
                Code = payment.Code,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                PaymentType = payment.PaymentType,
                PaidOnUtc = payment.PaidOnUtc
            }).ToList()
        };
    }

    private static CustomerOrderSummaryAppDto MapOrderSummary(Order order)
        => new()
        {
            Id = order.Id,
            Code = order.Code,
            Status = (int)order.OrderStatus,
            TotalAmount = order.OrderTotal,
            CreatedOnUtc = order.CreatedOnUtc,
            ExpectedShippingDateUtc = order.ExpectedShippingDateUtc
        };

    private static CustomerDeliveryNoteSummaryAppDto MapDeliveryNoteSummary(DeliveryNote deliveryNote)
        => new()
        {
            Id = deliveryNote.Id,
            Code = deliveryNote.Code,
            OrderCode = deliveryNote.OrderCode,
            Status = (int)deliveryNote.Status,
            DeliveryConfirmationStatus = (int)deliveryNote.DeliveryConfirmationStatus,
            CreatedOnUtc = deliveryNote.CreatedOnUtc,
            DeliveredOnUtc = deliveryNote.DeliveredOnUtc
        };

    private static string BuildConfirmationMessage(ConfirmCustomerDeliveryNoteAppDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ReceiverName))
            return dto.Note ?? "Customer confirmed delivery.";

        return string.IsNullOrWhiteSpace(dto.Note)
            ? $"Customer confirmed delivery. Receiver: {dto.ReceiverName}"
            : $"Customer confirmed delivery. Receiver: {dto.ReceiverName}. Note: {dto.Note}";
    }
}
