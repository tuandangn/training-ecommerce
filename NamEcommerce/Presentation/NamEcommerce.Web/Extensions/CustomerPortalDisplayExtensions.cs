using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Web.Extensions;

public static class CustomerPortalDisplayExtensions
{
    public static string GetDisplayText(this CustomerPortalAccountStatus status)
        => status switch
        {
            CustomerPortalAccountStatus.Active => "Đang hoạt động",
            CustomerPortalAccountStatus.Blocked => "Đã khóa",
            _ => "Không rõ"
        };

    public static string GetDisplayColor(this CustomerPortalAccountStatus status)
        => status switch
        {
            CustomerPortalAccountStatus.Active => "bg-success",
            CustomerPortalAccountStatus.Blocked => "bg-danger",
            _ => "bg-secondary"
        };

    public static string GetDisplayText(this CustomerPortalSecurityEventOutcome outcome)
        => outcome switch
        {
            CustomerPortalSecurityEventOutcome.Succeeded => "Thành công",
            CustomerPortalSecurityEventOutcome.Failed => "Thất bại",
            CustomerPortalSecurityEventOutcome.Blocked => "Bị chặn",
            _ => "Không rõ"
        };

    public static string GetDisplayColor(this CustomerPortalSecurityEventOutcome outcome)
        => outcome switch
        {
            CustomerPortalSecurityEventOutcome.Succeeded => "bg-success",
            CustomerPortalSecurityEventOutcome.Failed => "bg-warning text-dark",
            CustomerPortalSecurityEventOutcome.Blocked => "bg-danger",
            _ => "bg-secondary"
        };

    public static string GetDisplayText(this CustomerOrderRequestStatus status)
        => status switch
        {
            CustomerOrderRequestStatus.PendingApproval => "Chờ duyệt",
            CustomerOrderRequestStatus.Approved => "Đã duyệt - chờ khách xác nhận",
            CustomerOrderRequestStatus.Rejected => "Đã từ chối",
            CustomerOrderRequestStatus.ConvertedToOrder => "Đã tạo đơn",
            CustomerOrderRequestStatus.Cancelled => "Đã hủy",
            _ => "Không rõ"
        };

    public static string GetDisplayColor(this CustomerOrderRequestStatus status)
        => status switch
        {
            CustomerOrderRequestStatus.PendingApproval => "bg-warning text-dark",
            CustomerOrderRequestStatus.Approved => "bg-primary",
            CustomerOrderRequestStatus.Rejected => "bg-danger",
            CustomerOrderRequestStatus.ConvertedToOrder => "bg-success",
            CustomerOrderRequestStatus.Cancelled => "bg-secondary",
            _ => "bg-secondary"
        };

    public static string GetDisplayText(this CustomerReturnRequestStatus status)
        => status switch
        {
            CustomerReturnRequestStatus.PendingReview => "Chờ xem xét",
            CustomerReturnRequestStatus.Accepted => "Đã chấp nhận",
            CustomerReturnRequestStatus.Rejected => "Đã từ chối",
            CustomerReturnRequestStatus.ConvertedToReturn => "Đã tạo phiếu trả",
            CustomerReturnRequestStatus.Cancelled => "Đã hủy",
            _ => "Không rõ"
        };

    public static string GetDisplayColor(this CustomerReturnRequestStatus status)
        => status switch
        {
            CustomerReturnRequestStatus.PendingReview => "bg-warning text-dark",
            CustomerReturnRequestStatus.Accepted => "bg-primary",
            CustomerReturnRequestStatus.Rejected => "bg-danger",
            CustomerReturnRequestStatus.ConvertedToReturn => "bg-success",
            CustomerReturnRequestStatus.Cancelled => "bg-secondary",
            _ => "bg-secondary"
        };

    public static string GetDisplayText(this CustomerPaymentIntentStatus status)
        => status switch
        {
            CustomerPaymentIntentStatus.Created => "Mới tạo",
            CustomerPaymentIntentStatus.Processing => "Đang xử lý",
            CustomerPaymentIntentStatus.SucceededPendingReconciliation => "Chờ đối soát",
            CustomerPaymentIntentStatus.Failed => "Thất bại",
            CustomerPaymentIntentStatus.Cancelled => "Đã hủy",
            CustomerPaymentIntentStatus.Reconciled => "Đã đối soát",
            _ => "Không rõ"
        };

    public static string GetDisplayColor(this CustomerPaymentIntentStatus status)
        => status switch
        {
            CustomerPaymentIntentStatus.Created => "bg-secondary",
            CustomerPaymentIntentStatus.Processing => "bg-primary",
            CustomerPaymentIntentStatus.SucceededPendingReconciliation => "bg-warning text-dark",
            CustomerPaymentIntentStatus.Failed => "bg-danger",
            CustomerPaymentIntentStatus.Cancelled => "bg-secondary",
            CustomerPaymentIntentStatus.Reconciled => "bg-success",
            _ => "bg-secondary"
        };
}
