using MediatR;
using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;
using NamEcommerce.Customer.Contracts.Commands;
using NamEcommerce.Customer.Contracts.Models;
using NamEcommerce.Customer.Framework.Services;

namespace NamEcommerce.Customer.Framework.Commands.Handlers;

public sealed class CustomerPortalCommandHandlers(
    ICustomerPortalAuthAppService authAppService,
    ICustomerPortalAppService portalAppService,
    ICustomerPortalPaymentAppService paymentAppService,
    ICustomerSessionAccessor sessionAccessor) :
    IRequestHandler<RequestCustomerOtpCommand, CustomerOtpRequestResultModel>,
    IRequestHandler<VerifyCustomerOtpCommand, CustomerLoginResultModel>,
    IRequestHandler<CustomerPasswordLoginCommand, CustomerLoginResultModel>,
    IRequestHandler<SetCustomerPasswordCommand, CustomerActionResultModel>,
    IRequestHandler<ChangeCustomerPasswordCommand, CustomerActionResultModel>,
    IRequestHandler<LogoutCustomerCommand, CustomerActionResultModel>,
    IRequestHandler<CreateCustomerOrderRequestCommand, CustomerOrderRequestModel>,
    IRequestHandler<ConfirmCustomerOrderRequestCommand, CustomerPortalConversionResultModel>,
    IRequestHandler<ConfirmCustomerDeliveryNoteCommand, CustomerActionResultModel>,
    IRequestHandler<CreateCustomerDeliveryFeedbackCommand, CustomerActionResultModel>,
    IRequestHandler<CreateCustomerReturnRequestCommand, CustomerReturnRequestModel>,
    IRequestHandler<CancelCustomerReturnRequestCommand, CustomerActionResultModel>,
    IRequestHandler<CreateCustomerPaymentIntentCommand, CustomerPaymentIntentModel?>,
    IRequestHandler<CompleteMockCustomerPaymentCommand, CustomerPaymentIntentModel?>
{
    public async Task<CustomerOtpRequestResultModel> Handle(RequestCustomerOtpCommand request, CancellationToken cancellationToken)
    {
        var result = await authAppService.RequestOtpAsync(new CustomerOtpRequestAppDto
        {
            DeliveryToken = request.DeliveryToken,
            RequestedIp = request.RequestedIp,
            RequestedUserAgent = request.RequestedUserAgent
        }).ConfigureAwait(false);

        return new CustomerOtpRequestResultModel(result.Success, result.Message, result.ChallengeId, result.MaskedDestination, result.MockOtp);
    }

    public async Task<CustomerLoginResultModel> Handle(VerifyCustomerOtpCommand request, CancellationToken cancellationToken)
    {
        var result = await authAppService.VerifyOtpAsync(new CustomerOtpVerifyAppDto
        {
            ChallengeId = request.ChallengeId,
            Otp = request.Otp,
            RequestedIp = request.RequestedIp,
            RequestedUserAgent = request.RequestedUserAgent
        }).ConfigureAwait(false);

        return MapLoginResult(result);
    }

    public async Task<CustomerLoginResultModel> Handle(CustomerPasswordLoginCommand request, CancellationToken cancellationToken)
    {
        var result = await authAppService.PasswordLoginAsync(new CustomerPasswordLoginAppDto
        {
            Login = request.Login,
            Password = request.Password,
            RequestedIp = request.RequestedIp,
            RequestedUserAgent = request.RequestedUserAgent
        }).ConfigureAwait(false);

        return MapLoginResult(result);
    }

    public async Task<CustomerActionResultModel> Handle(SetCustomerPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await authAppService.SetPasswordAsync(RequireCustomerId(), new SetCustomerPasswordAppDto { Password = request.Password }).ConfigureAwait(false);
        return new CustomerActionResultModel(result.Success, result.Message);
    }

    public async Task<CustomerActionResultModel> Handle(ChangeCustomerPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await authAppService.ChangePasswordAsync(RequireCustomerId(), new ChangeCustomerPasswordAppDto
        {
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
        }).ConfigureAwait(false);
        return new CustomerActionResultModel(result.Success, result.Message);
    }

    public async Task<CustomerActionResultModel> Handle(LogoutCustomerCommand request, CancellationToken cancellationToken)
    {
        var sessionId = sessionAccessor.SessionId;
        if (!sessionId.HasValue)
            return new CustomerActionResultModel(true);

        var result = await authAppService.LogoutAsync(sessionId.Value).ConfigureAwait(false);
        return new CustomerActionResultModel(result.Success, result.Message);
    }

    public async Task<CustomerOrderRequestModel> Handle(CreateCustomerOrderRequestCommand request, CancellationToken cancellationToken)
    {
        var result = await portalAppService.CreateOrderRequestAsync(RequireCustomerId(), new CreateCustomerOrderRequestAppDto
        {
            ExpectedShippingDateUtc = request.ExpectedShippingDate,
            ShippingAddress = request.ShippingAddress,
            Note = request.Note,
            Items = request.Items.Select(item => new CreateCustomerOrderRequestItemAppDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        }).ConfigureAwait(false);

        return new CustomerOrderRequestModel(result.Id, result.Code, result.Status, result.CreatedOnUtc);
    }

    public async Task<CustomerPortalConversionResultModel> Handle(ConfirmCustomerOrderRequestCommand request, CancellationToken cancellationToken)
    {
        var result = await portalAppService.ConfirmOrderRequestAsync(RequireCustomerId(), request.OrderRequestId).ConfigureAwait(false);
        return new CustomerPortalConversionResultModel(result.Success, result.Message, result.CreatedId);
    }

    public async Task<CustomerActionResultModel> Handle(ConfirmCustomerDeliveryNoteCommand request, CancellationToken cancellationToken)
    {
        var result = await portalAppService.ConfirmDeliveryNoteAsync(RequireCustomerId(), request.DeliveryNoteId, new ConfirmCustomerDeliveryNoteAppDto
        {
            ReceiverName = request.ReceiverName,
            Note = request.Note,
            Acceptance = request.Acceptance is null
                ? null
                : new ConfirmCustomerDeliveryAcceptanceAppDto
                {
                    AgreedCustomerCharge = request.Acceptance.AgreedCustomerCharge,
                    AgreedCustomerChargeReason = request.Acceptance.AgreedCustomerChargeReason,
                    CompensateInNextDelivery = request.Acceptance.CompensateInNextDelivery,
                    Items = request.Acceptance.Items
                        .Select(item => new ConfirmCustomerDeliveryAcceptanceItemAppDto
                        {
                            DeliveryNoteItemId = item.DeliveryNoteItemId,
                            AcceptedQuantity = item.AcceptedQuantity,
                            RejectedQuantity = item.RejectedQuantity,
                            RejectReason = item.RejectReason
                        })
                        .ToList()
                }
        }).ConfigureAwait(false);

        return new CustomerActionResultModel(result.Success, result.Message);
    }

    public async Task<CustomerActionResultModel> Handle(CreateCustomerDeliveryFeedbackCommand request, CancellationToken cancellationToken)
    {
        var result = await portalAppService.CreateDeliveryFeedbackAsync(RequireCustomerId(), new CreateCustomerDeliveryFeedbackAppDto
        {
            DeliveryNoteId = request.DeliveryNoteId,
            Rating = request.Rating,
            Message = request.Message
        }).ConfigureAwait(false);

        return new CustomerActionResultModel(result.Success, result.Message);
    }

    public async Task<CustomerReturnRequestModel> Handle(CreateCustomerReturnRequestCommand request, CancellationToken cancellationToken)
    {
        var result = await portalAppService.CreateReturnRequestAsync(RequireCustomerId(), new CreateCustomerReturnRequestAppDto
        {
            DeliveryNoteId = request.DeliveryNoteId,
            Reason = request.Reason,
            CompensateInNextDelivery = request.CompensateInNextDelivery,
            Items = request.Items.Select(item => new CreateCustomerReturnRequestItemAppDto
            {
                DeliveryNoteItemId = item.DeliveryNoteItemId,
                ProductId = item.ProductId,
                RequestedQuantity = item.RequestedQuantity,
                Reason = item.Reason,
                EvidencePictures = item.EvidencePictures?
                    .Select(picture => new CreateCustomerReturnRequestPictureAppDto
                    {
                        FileName = picture.FileName,
                        MimeType = picture.MimeType,
                        Base64Data = picture.Base64Data
                    })
                    .ToList() ?? []
            }).ToList()
        }).ConfigureAwait(false);

        return new CustomerReturnRequestModel(result.Id, result.DeliveryNoteId, result.Status, result.CreatedOnUtc, result.CompensateInNextDelivery);
    }

    public async Task<CustomerActionResultModel> Handle(CancelCustomerReturnRequestCommand request, CancellationToken cancellationToken)
    {
        var result = await portalAppService.CancelReturnRequestAsync(RequireCustomerId(), request.ReturnRequestId).ConfigureAwait(false);
        return new CustomerActionResultModel(result.Success, result.Message);
    }

    public async Task<CustomerPaymentIntentModel?> Handle(CreateCustomerPaymentIntentCommand request, CancellationToken cancellationToken)
    {
        var result = await paymentAppService.CreatePaymentIntentAsync(RequireCustomerId(), new CreateCustomerPaymentIntentAppDto
        {
            CustomerDebtId = request.CustomerDebtId,
            Amount = request.Amount
        }).ConfigureAwait(false);

        return result is null ? null : MapPaymentIntent(result);
    }

    public async Task<CustomerPaymentIntentModel?> Handle(CompleteMockCustomerPaymentCommand request, CancellationToken cancellationToken)
    {
        var result = await paymentAppService.CompleteMockPaymentAsync(RequireCustomerId(), request.PaymentIntentId, request.Success).ConfigureAwait(false);
        return result is null ? null : MapPaymentIntent(result);
    }

    private Guid RequireCustomerId()
        => sessionAccessor.CustomerId ?? throw new UnauthorizedAccessException();

    private static CustomerLoginResultModel MapLoginResult(CustomerPortalLoginResultAppDto result)
        => new(result.Success, result.Message, result.SessionToken, result.Session is null ? null : MapSession(result.Session));

    private static CustomerSessionModel MapSession(CustomerSessionAppDto session)
        => new(session.SessionId, session.CustomerId, session.CustomerName, session.PhoneNumber, session.Email, session.HasPassword, session.ExpiresOnUtc);

    private static CustomerPaymentIntentModel MapPaymentIntent(CustomerPaymentIntentAppDto intent)
        => new(intent.Id, intent.CustomerDebtId, intent.Amount, intent.Provider, intent.ProviderIntentId, intent.Status, intent.FailureReason, intent.CreatedOnUtc, intent.CompletedOnUtc);
}
