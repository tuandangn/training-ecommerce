namespace NamEcommerce.Customer.Contracts.Models;

[Serializable]
public sealed record CustomerActionResultModel(bool Success, string? Message = null);

[Serializable]
public sealed record PublicDeliveryNoteModel(
    Guid Id,
    string Code,
    string? OrderCode,
    int Status,
    int DeliveryConfirmationStatus,
    DateTime CreatedOn,
    DateTime? DeliveredOn,
    IList<PublicDeliveryNoteItemModel> Items);

[Serializable]
public sealed record PublicDeliveryNoteItemModel(Guid Id, Guid ProductId, string ProductName, decimal Quantity);

[Serializable]
public sealed record CustomerSessionModel(
    Guid SessionId,
    Guid CustomerId,
    string CustomerName,
    string? PhoneNumber,
    string? Email,
    bool HasPassword,
    DateTime ExpiresOn);

[Serializable]
public sealed record CustomerLoginResultModel(bool Success, string? Message, string? SessionToken, CustomerSessionModel? Session);

[Serializable]
public sealed record CustomerOtpRequestResultModel(bool Success, string? Message, Guid? ChallengeId, string? MaskedDestination, string? MockOtp);

[Serializable]
public sealed record CustomerOrderListModel(IList<CustomerOrderSummaryModel> Items);

[Serializable]
public record CustomerOrderSummaryModel(
    Guid Id,
    string Code,
    int Status,
    decimal TotalAmount,
    DateTime CreatedOn,
    DateTime? ExpectedShippingDate);

[Serializable]
public sealed record CustomerOrderDetailsModel(
    Guid Id,
    string Code,
    int Status,
    decimal TotalAmount,
    DateTime CreatedOn,
    DateTime? ExpectedShippingDate,
    string? ShippingAddress,
    string? Note,
    IList<CustomerOrderItemModel> Items);

[Serializable]
public sealed record CustomerOrderItemModel(Guid Id, Guid ProductId, string ProductName, decimal Quantity, decimal UnitPrice, decimal SubTotal);

[Serializable]
public sealed record CustomerOrderRequestModel(Guid Id, string Code, int Status, DateTime CreatedOn);

[Serializable]
public sealed record CustomerProductListModel(IList<CustomerProductModel> Items, bool HasMore, int PageSize);

[Serializable]
public sealed record CustomerProductModel(Guid Id, string Name, Guid? CategoryId, string? CategoryName, string? PictureUrl, decimal UnitPrice);

[Serializable]
public sealed record CustomerProductCategoryListModel(IList<CustomerProductCategoryModel> Items);

[Serializable]
public sealed record CustomerProductCategoryModel(Guid Id, string Name, Guid? ParentId);

[Serializable]
public sealed record CustomerOrderRequestDefaultsModel(string? ShippingAddress, string? ShippingAddressSource);

[Serializable]
public sealed record CustomerContactModel(CustomerStoreContactModel Store, IList<CustomerWarehouseContactModel> Warehouses);

[Serializable]
public sealed record CustomerStoreContactModel(string StoreName, string? PhoneNumber, string? Address, string? Email, string? MapQuery);

[Serializable]
public sealed record CustomerWarehouseContactModel(Guid Id, string Name, string? PhoneNumber, string? Address, string? MapQuery);

[Serializable]
public sealed record CustomerDeliveryNoteListModel(IList<CustomerDeliveryNoteSummaryModel> Items);

[Serializable]
public record CustomerDeliveryNoteSummaryModel(
    Guid Id,
    string Code,
    string? OrderCode,
    int Status,
    int DeliveryConfirmationStatus,
    DateTime CreatedOn,
    DateTime? DeliveredOn);

[Serializable]
public sealed record CustomerDeliveryNoteDetailsModel(
    Guid Id,
    string Code,
    string? OrderCode,
    int Status,
    int DeliveryConfirmationStatus,
    DateTime CreatedOn,
    DateTime? DeliveredOn,
    IList<CustomerDeliveryNoteItemModel> Items);

[Serializable]
public sealed record CustomerDeliveryNoteItemModel(Guid Id, Guid ProductId, string ProductName, decimal Quantity, decimal UnitPrice, decimal SubTotal);

[Serializable]
public sealed record CustomerReturnRequestModel(Guid Id, Guid DeliveryNoteId, int Status, DateTime CreatedOn);

[Serializable]
public sealed record CustomerDebtSummaryModel(
    decimal TotalDebtAmount,
    decimal TotalPaidAmount,
    decimal TotalRemainingAmount,
    decimal DepositBalance,
    IList<CustomerDebtModel> Debts,
    IList<CustomerPaymentModel> RecentPayments);

[Serializable]
public sealed record CustomerDebtModel(
    Guid Id,
    string Code,
    string? OrderCode,
    string? DeliveryNoteCode,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    int Status,
    DateTime? DueDate);

[Serializable]
public sealed record CustomerPaymentModel(Guid Id, string Code, decimal Amount, int PaymentMethod, int PaymentType, DateTime PaidOn);

[Serializable]
public sealed record CustomerDashboardModel(
    IList<CustomerOrderSummaryModel> RecentOrders,
    IList<CustomerDeliveryNoteSummaryModel> RecentDeliveryNotes,
    CustomerDebtSummaryModel DebtSummary);

[Serializable]
public sealed record CustomerPaymentIntentModel(
    Guid Id,
    Guid? CustomerDebtId,
    decimal Amount,
    string Provider,
    string? ProviderIntentId,
    int Status,
    string? FailureReason,
    DateTime CreatedOn,
    DateTime? CompletedOn);
