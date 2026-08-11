using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.CustomerPortal;
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.CustomerPortal;

namespace NamEcommerce.Domain.Services.CustomerPortal;

public sealed class CustomerPortalManager(
    IRepository<CustomerDeliveryFeedback> feedbackRepository,
    IEntityDataReader<CustomerDeliveryFeedback> feedbackReader,
    IRepository<CustomerPortalNotification> notificationRepository,
    IEntityDataReader<CustomerPortalNotification> notificationReader,
    IRepository<CustomerOrderRequest> orderRequestRepository,
    IEntityDataReader<CustomerOrderRequest> orderRequestReader,
    IRepository<CustomerReturnRequest> returnRequestRepository,
    IEntityDataReader<CustomerReturnRequest> returnRequestReader,
    IRepository<CustomerPaymentIntent> paymentIntentRepository,
    IEntityDataReader<CustomerPaymentIntent> paymentIntentReader,
    EntityCodeGenerator entityCodeGenerator) : ICustomerPortalManager
{
    public async Task<CustomerDeliveryFeedbackDto> CreateDeliveryFeedbackAsync(CreateCustomerDeliveryFeedbackDto dto)
    {
        dto.Verify();

        var feedback = new CustomerDeliveryFeedback(dto.CustomerId, dto.DeliveryNoteId, dto.Rating, dto.Message);
        var inserted = await feedbackRepository.InsertAsync(feedback).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public Task<IReadOnlyCollection<CustomerDeliveryFeedbackDto>> GetDeliveryFeedbacksAsync(Guid customerId)
    {
        var feedbacks = feedbackReader.DataSource
            .Where(feedback => feedback.CustomerId == customerId)
            .OrderByDescending(feedback => feedback.CreatedOnUtc)
            .ToList()
            .Select(MapToDto)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerDeliveryFeedbackDto>>(feedbacks);
    }

    public async Task<CustomerPortalNotificationDto> CreateNotificationAsync(CreateCustomerPortalNotificationDto dto)
    {
        dto.Verify();

        var notification = new CustomerPortalNotification(
            dto.CustomerId,
            dto.Type,
            dto.Title,
            dto.Message,
            dto.RelatedEntityId,
            dto.RelatedEntityType);
        var inserted = await notificationRepository.InsertAsync(notification).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<CustomerPortalNotificationDto?> GetNotificationByIdAsync(Guid id)
    {
        var notification = await notificationReader.GetByIdAsync(id).ConfigureAwait(false);
        return notification is null ? null : MapToDto(notification);
    }

    public Task<IReadOnlyCollection<CustomerPortalNotificationDto>> GetNotificationsAsync(CustomerPortalNotificationStatus? status = null, int take = 100)
    {
        take = Math.Clamp(take, 1, 500);
        var query = notificationReader.DataSource.AsQueryable();
        if (status.HasValue)
            query = query.Where(notification => notification.Status == status.Value);

        var notifications = query
            .OrderByDescending(notification => notification.CreatedOnUtc)
            .Take(take)
            .ToList()
            .Select(MapToDto)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerPortalNotificationDto>>(notifications);
    }

    public Task<int> CountNotificationsAsync(CustomerPortalNotificationStatus status)
        => Task.FromResult(notificationReader.DataSource.Count(notification => notification.Status == status));

    public async Task MarkNotificationReadAsync(Guid id, Guid readByUserId, DateTime nowUtc)
    {
        var notification = await notificationRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CustomerPortal.NotificationNotFound", id);

        notification.MarkRead(readByUserId, nowUtc);
        await notificationRepository.UpdateAsync(notification).ConfigureAwait(false);
    }

    public async Task<CustomerOrderRequestDto> CreateOrderRequestAsync(CreateCustomerOrderRequestDto dto)
    {
        dto.Verify();

        var code = await GenerateOrderRequestCode().ConfigureAwait(false);
        var request = new CustomerOrderRequest(dto.CustomerId, code)
        {
            ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc,
            ShippingAddress = dto.ShippingAddress,
            Note = dto.Note
        };

        foreach (var item in dto.Items)
            request.AddItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPriceSnapshot);

        var inserted = await orderRequestRepository.InsertAsync(request).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<CustomerOrderRequestDto?> GetOrderRequestByIdAsync(Guid id)
    {
        var request = await orderRequestReader.GetByIdAsync(id).ConfigureAwait(false);
        return request is null ? null : MapToDto(request);
    }

    public Task<IReadOnlyCollection<CustomerOrderRequestDto>> GetOrderRequestsAsync(Guid customerId)
    {
        var requests = orderRequestReader.DataSource
            .Where(request => request.CustomerId == customerId)
            .OrderByDescending(request => request.CreatedOnUtc)
            .ToList()
            .Select(MapToDto)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerOrderRequestDto>>(requests);
    }

    public Task<IReadOnlyCollection<CustomerOrderRequestDto>> GetOrderRequestsByStatusAsync(CustomerOrderRequestStatus status)
    {
        var requests = orderRequestReader.DataSource
            .Where(request => request.Status == status)
            .OrderBy(request => request.CreatedOnUtc)
            .ToList()
            .Select(MapToDto)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerOrderRequestDto>>(requests);
    }

    public async Task ApproveOrderRequestAsync(Guid id, Guid reviewedByUserId, IReadOnlyDictionary<Guid, decimal> itemPrices, string? adminNote, DateTime nowUtc)
    {
        var request = await orderRequestRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CustomerPortal.OrderRequestNotFound", id);

        request.UpdateItemPrices(itemPrices);
        request.Approve(reviewedByUserId, adminNote, nowUtc);
        await orderRequestRepository.UpdateAsync(request).ConfigureAwait(false);
    }

    public async Task RejectOrderRequestAsync(Guid id, Guid reviewedByUserId, string? adminNote, DateTime nowUtc)
    {
        var request = await orderRequestRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CustomerPortal.OrderRequestNotFound", id);

        request.Reject(reviewedByUserId, adminNote, nowUtc);
        await orderRequestRepository.UpdateAsync(request).ConfigureAwait(false);
    }

    public async Task MarkOrderRequestConvertedAsync(Guid id, Guid orderId, DateTime nowUtc)
    {
        var request = await orderRequestRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CustomerPortal.OrderRequestNotFound", id);

        request.MarkConverted(orderId, nowUtc);
        await orderRequestRepository.UpdateAsync(request).ConfigureAwait(false);
    }

    public async Task<CustomerReturnRequestDto> CreateReturnRequestAsync(CreateCustomerReturnRequestDto dto)
    {
        dto.Verify();

        var request = new CustomerReturnRequest(
            dto.CustomerId,
            dto.DeliveryNoteId,
            dto.Reason,
            dto.CompensateInNextDelivery);
        foreach (var item in dto.Items)
            request.AddItem(
                item.DeliveryNoteItemId,
                item.ProductId,
                item.ProductName,
                item.RequestedQuantity,
                item.Reason,
                item.EvidencePictureIds);

        var inserted = await returnRequestRepository.InsertAsync(request).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<CustomerReturnRequestDto?> GetReturnRequestByIdAsync(Guid id)
    {
        var request = await returnRequestReader.GetByIdAsync(id).ConfigureAwait(false);
        return request is null ? null : MapToDto(request);
    }

    public Task<IReadOnlyCollection<CustomerReturnRequestDto>> GetReturnRequestsAsync(Guid customerId)
    {
        var requests = returnRequestReader.DataSource
            .Where(request => request.CustomerId == customerId)
            .OrderByDescending(request => request.CreatedOnUtc)
            .ToList()
            .Select(MapToDto)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerReturnRequestDto>>(requests);
    }

    public Task<IReadOnlyCollection<CustomerReturnRequestDto>> GetReturnRequestsByStatusAsync(CustomerReturnRequestStatus status)
    {
        var requests = returnRequestReader.DataSource
            .Where(request => request.Status == status)
            .OrderBy(request => request.CreatedOnUtc)
            .ToList()
            .Select(MapToDto)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerReturnRequestDto>>(requests);
    }

    public async Task AcceptReturnRequestAsync(Guid id, Guid reviewedByUserId, string? adminNote, DateTime nowUtc)
    {
        var request = await returnRequestRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CustomerPortal.ReturnRequestNotFound", id);

        request.Accept(reviewedByUserId, adminNote, nowUtc);
        await returnRequestRepository.UpdateAsync(request).ConfigureAwait(false);
    }

    public async Task RejectReturnRequestAsync(Guid id, Guid reviewedByUserId, string? adminNote, DateTime nowUtc)
    {
        var request = await returnRequestRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CustomerPortal.ReturnRequestNotFound", id);

        request.Reject(reviewedByUserId, adminNote, nowUtc);
        await returnRequestRepository.UpdateAsync(request).ConfigureAwait(false);
    }

    public async Task CancelReturnRequestAsync(Guid id, DateTime nowUtc)
    {
        var request = await returnRequestRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CustomerPortal.ReturnRequestNotFound", id);

        request.Cancel(nowUtc);
        await returnRequestRepository.UpdateAsync(request).ConfigureAwait(false);
    }

    public async Task MarkReturnRequestConvertedAsync(Guid id, Guid customerReturnId, DateTime nowUtc)
    {
        var request = await returnRequestRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CustomerPortal.ReturnRequestNotFound", id);

        request.MarkConverted(customerReturnId, nowUtc);
        await returnRequestRepository.UpdateAsync(request).ConfigureAwait(false);
    }

    public async Task<CustomerPaymentIntentDto> CreatePaymentIntentAsync(CreateCustomerPaymentIntentDto dto)
    {
        dto.Verify();

        var intent = new CustomerPaymentIntent(dto.CustomerId, dto.CustomerDebtId, dto.Amount, dto.Provider);
        var inserted = await paymentIntentRepository.InsertAsync(intent).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<CustomerPaymentIntentDto?> GetPaymentIntentByIdAsync(Guid id)
    {
        var intent = await paymentIntentReader.GetByIdAsync(id).ConfigureAwait(false);
        return intent is null ? null : MapToDto(intent);
    }

    public Task<IReadOnlyCollection<CustomerPaymentIntentDto>> GetPaymentIntentsAsync(Guid customerId)
    {
        var intents = paymentIntentReader.DataSource
            .Where(intent => intent.CustomerId == customerId)
            .OrderByDescending(intent => intent.CreatedOnUtc)
            .ToList()
            .Select(MapToDto)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerPaymentIntentDto>>(intents);
    }

    public Task<IReadOnlyCollection<CustomerPaymentIntentDto>> GetPaymentIntentsByStatusAsync(CustomerPaymentIntentStatus status)
    {
        var intents = paymentIntentReader.DataSource
            .Where(intent => intent.Status == status)
            .OrderBy(intent => intent.CreatedOnUtc)
            .ToList()
            .Select(MapToDto)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerPaymentIntentDto>>(intents);
    }

    public async Task<CustomerPaymentIntentDto> MarkPaymentIntentProcessingAsync(Guid id, string providerIntentId)
    {
        var intent = await paymentIntentRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CustomerPortal.PaymentIntentNotFound", id);

        intent.MarkProcessing(providerIntentId);
        var updated = await paymentIntentRepository.UpdateAsync(intent).ConfigureAwait(false);
        return MapToDto(updated);
    }

    public async Task<CustomerPaymentIntentDto> MarkPaymentIntentSucceededPendingReconciliationAsync(Guid id, DateTime nowUtc)
    {
        var intent = await paymentIntentRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CustomerPortal.PaymentIntentNotFound", id);

        intent.MarkSucceededPendingReconciliation(nowUtc);
        var updated = await paymentIntentRepository.UpdateAsync(intent).ConfigureAwait(false);
        return MapToDto(updated);
    }

    public async Task<CustomerPaymentIntentDto> MarkPaymentIntentFailedAsync(Guid id, string? failureReason, DateTime nowUtc)
    {
        var intent = await paymentIntentRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CustomerPortal.PaymentIntentNotFound", id);

        intent.MarkFailed(failureReason, nowUtc);
        var updated = await paymentIntentRepository.UpdateAsync(intent).ConfigureAwait(false);
        return MapToDto(updated);
    }

    public async Task MarkPaymentIntentReconciledAsync(Guid id, Guid customerPaymentId, Guid reconciledByUserId, DateTime nowUtc)
    {
        var intent = await paymentIntentRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CustomerPortal.PaymentIntentNotFound", id);

        intent.MarkReconciled(customerPaymentId, reconciledByUserId, nowUtc);
        await paymentIntentRepository.UpdateAsync(intent).ConfigureAwait(false);
    }

    private Task<string> GenerateOrderRequestCode()
    {
        var prefix = $"YC-DH-{DateTime.UtcNow:yyMM}";
        return entityCodeGenerator.NextAsync(prefix, () => orderRequestReader.TrackingDataSource.CountAsync(d => d.Code.StartsWith(prefix)));
    }

    private static CustomerDeliveryFeedbackDto MapToDto(CustomerDeliveryFeedback feedback)
        => new(feedback.Id)
        {
            CustomerId = feedback.CustomerId,
            DeliveryNoteId = feedback.DeliveryNoteId,
            Rating = feedback.Rating,
            Message = feedback.Message,
            Status = feedback.Status,
            CreatedOnUtc = feedback.CreatedOnUtc,
            ReviewedOnUtc = feedback.ReviewedOnUtc
        };

    private static CustomerPortalNotificationDto MapToDto(CustomerPortalNotification notification)
        => new(notification.Id)
        {
            CustomerId = notification.CustomerId,
            Type = notification.Type,
            Status = notification.Status,
            Title = notification.Title,
            Message = notification.Message,
            RelatedEntityId = notification.RelatedEntityId,
            RelatedEntityType = notification.RelatedEntityType,
            CreatedOnUtc = notification.CreatedOnUtc,
            ReadOnUtc = notification.ReadOnUtc,
            ReadByUserId = notification.ReadByUserId
        };

    private static CustomerOrderRequestDto MapToDto(CustomerOrderRequest request)
        => new(request.Id)
        {
            CustomerId = request.CustomerId,
            Code = request.Code,
            Status = request.Status,
            ExpectedShippingDateUtc = request.ExpectedShippingDateUtc,
            ShippingAddress = request.ShippingAddress,
            Note = request.Note,
            AdminNote = request.AdminNote,
            CreatedOnUtc = request.CreatedOnUtc,
            ReviewedOnUtc = request.ReviewedOnUtc,
            ReviewedByUserId = request.ReviewedByUserId,
            ConvertedOrderId = request.ConvertedOrderId,
            Items = request.Items.Select(MapToDto).ToList()
        };

    private static CustomerOrderRequestItemDto MapToDto(CustomerOrderRequestItem item)
        => new(item.Id)
        {
            CustomerOrderRequestId = item.CustomerOrderRequestId,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            UnitPriceSnapshot = item.UnitPriceSnapshot,
            SubTotal = item.SubTotal
        };

    private static CustomerReturnRequestDto MapToDto(CustomerReturnRequest request)
        => new(request.Id)
        {
            CustomerId = request.CustomerId,
            DeliveryNoteId = request.DeliveryNoteId,
            Status = request.Status,
            Reason = request.Reason,
            CompensateInNextDelivery = request.CompensateInNextDelivery,
            AdminNote = request.AdminNote,
            CreatedOnUtc = request.CreatedOnUtc,
            ReviewedOnUtc = request.ReviewedOnUtc,
            ReviewedByUserId = request.ReviewedByUserId,
            ConvertedCustomerReturnId = request.ConvertedCustomerReturnId,
            Items = request.Items.Select(MapToDto).ToList()
        };

    private static CustomerReturnRequestItemDto MapToDto(CustomerReturnRequestItem item)
        => new(item.Id)
        {
            CustomerReturnRequestId = item.CustomerReturnRequestId,
            DeliveryNoteItemId = item.DeliveryNoteItemId,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            RequestedQuantity = item.RequestedQuantity,
            Reason = item.Reason,
            EvidencePictures = item.EvidencePictures.Select(MapToDto).ToList()
        };

    private static CustomerReturnRequestItemPictureDto MapToDto(CustomerReturnRequestItemPicture picture)
        => new(picture.Id)
        {
            CustomerReturnRequestItemId = picture.CustomerReturnRequestItemId,
            PictureId = picture.PictureId,
            CreatedOnUtc = picture.CreatedOnUtc
        };

    private static CustomerPaymentIntentDto MapToDto(CustomerPaymentIntent intent)
        => new(intent.Id)
        {
            CustomerId = intent.CustomerId,
            CustomerDebtId = intent.CustomerDebtId,
            Amount = intent.Amount,
            Provider = intent.Provider,
            ProviderIntentId = intent.ProviderIntentId,
            Status = intent.Status,
            FailureReason = intent.FailureReason,
            CreatedOnUtc = intent.CreatedOnUtc,
            CompletedOnUtc = intent.CompletedOnUtc,
            ReconciledOnUtc = intent.ReconciledOnUtc,
            ReconciledByUserId = intent.ReconciledByUserId,
            CustomerPaymentId = intent.CustomerPaymentId
        };
}
