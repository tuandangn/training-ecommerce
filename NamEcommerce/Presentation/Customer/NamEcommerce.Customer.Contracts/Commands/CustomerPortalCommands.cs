using MediatR;
using NamEcommerce.Customer.Contracts.Models;

namespace NamEcommerce.Customer.Contracts.Commands;

public sealed record CustomerPortalLocationCommand(double Latitude, double Longitude, double? AccuracyMeters, DateTime? CapturedOnUtc);
public sealed record RequestCustomerOtpCommand(string DeliveryToken, string? RequestedIp, string? RequestedUserAgent, CustomerPortalLocationCommand? Location) : IRequest<CustomerOtpRequestResultModel>;
public sealed record VerifyCustomerOtpCommand(Guid ChallengeId, string Otp, string? RequestedIp, string? RequestedUserAgent, CustomerPortalLocationCommand? Location) : IRequest<CustomerLoginResultModel>;
public sealed record CustomerPasswordLoginCommand(string Login, string Password, string? RequestedIp, string? RequestedUserAgent) : IRequest<CustomerLoginResultModel>;
public sealed record SetCustomerPasswordCommand(string Password) : IRequest<CustomerActionResultModel>;
public sealed record ChangeCustomerPasswordCommand(string CurrentPassword, string NewPassword) : IRequest<CustomerActionResultModel>;
public sealed record LogoutCustomerCommand : IRequest<CustomerActionResultModel>;

public sealed record CreateCustomerOrderRequestCommand(
    DateTime? ExpectedShippingDate,
    string? ShippingAddress,
    string? Note,
    IList<CreateCustomerOrderRequestItemCommand> Items) : IRequest<CustomerOrderRequestModel>;

public sealed record CreateCustomerOrderRequestItemCommand(Guid ProductId, decimal Quantity);

public sealed record ConfirmCustomerOrderRequestCommand(Guid OrderRequestId) : IRequest<CustomerPortalConversionResultModel>;
public sealed record UpdateCustomerOrderNoteCommand(Guid OrderId, string? Note) : IRequest<CustomerActionResultModel>;

public sealed record ConfirmCustomerDeliveryAcceptanceItemCommand(
    Guid DeliveryNoteItemId,
    decimal AcceptedQuantity,
    decimal RejectedQuantity,
    string? RejectReason);

public sealed record ConfirmCustomerDeliveryAcceptanceCommand(
    decimal AgreedCustomerCharge,
    string? AgreedCustomerChargeReason,
    bool CompensateInNextDelivery,
    IList<ConfirmCustomerDeliveryAcceptanceItemCommand> Items);

public sealed record ConfirmCustomerDeliveryNoteCommand(
    Guid DeliveryNoteId,
    string? ReceiverName,
    string? Note,
    ConfirmCustomerDeliveryAcceptanceCommand? Acceptance,
    CustomerPortalLocationCommand? Location) : IRequest<CustomerActionResultModel>;
public sealed record CreateCustomerDeliveryFeedbackCommand(Guid DeliveryNoteId, int? Rating, string? Message) : IRequest<CustomerActionResultModel>;

public sealed record CreateCustomerReturnRequestCommand(
    Guid? DeliveryNoteId,
    string? Reason,
    bool CompensateInNextDelivery,
    IList<CreateCustomerReturnRequestItemCommand> Items) : IRequest<CustomerReturnRequestModel>;

public sealed record CreateCustomerReturnRequestItemCommand(
    Guid? DeliveryNoteItemId,
    Guid? ProductId,
    decimal RequestedQuantity,
    string? Reason,
    IList<CreateCustomerReturnRequestPictureCommand>? EvidencePictures = null);

public sealed record CreateCustomerReturnRequestPictureCommand(string FileName, string MimeType, string Base64Data);

public sealed record CancelCustomerReturnRequestCommand(Guid ReturnRequestId) : IRequest<CustomerActionResultModel>;

public sealed record CreateCustomerPaymentIntentCommand(Guid? CustomerDebtId, decimal Amount) : IRequest<CustomerPaymentIntentModel?>;
public sealed record CompleteMockCustomerPaymentCommand(Guid PaymentIntentId, bool Success) : IRequest<CustomerPaymentIntentModel?>;
