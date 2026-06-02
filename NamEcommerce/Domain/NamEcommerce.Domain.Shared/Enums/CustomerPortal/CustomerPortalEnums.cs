namespace NamEcommerce.Domain.Shared.Enums.CustomerPortal;

public enum CustomerPortalAccountStatus
{
    Active = 0,
    Blocked = 1
}

public enum CustomerOtpChannel
{
    Sms = 0,
    Email = 1
}

public enum CustomerOtpChallengeStatus
{
    Pending = 0,
    Verified = 1,
    Expired = 2,
    Locked = 3,
    Cancelled = 4
}

public enum CustomerPortalSecurityEventOutcome
{
    Succeeded = 0,
    Failed = 1,
    Blocked = 2
}

public enum CustomerDeliveryFeedbackStatus
{
    New = 0,
    Reviewed = 1
}

public enum CustomerOrderRequestStatus
{
    PendingApproval = 0,
    Approved = 1,
    Rejected = 2,
    ConvertedToOrder = 3,
    Cancelled = 4
}

public enum CustomerReturnRequestStatus
{
    PendingReview = 0,
    Accepted = 1,
    Rejected = 2,
    ConvertedToReturn = 3,
    Cancelled = 4
}

public enum CustomerPaymentIntentStatus
{
    Created = 0,
    Processing = 1,
    SucceededPendingReconciliation = 2,
    Failed = 3,
    Cancelled = 4,
    Reconciled = 5
}

public enum CustomerPortalNotificationType
{
    OrderRequestCreated = 0,
    ReturnRequestCreated = 1,
    DeliveryReceivedConfirmed = 2
}

public enum CustomerPortalNotificationStatus
{
    Unread = 0,
    Read = 1
}
