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
public sealed record CustomerOtpRequestResultModel(
    bool Success,
    string? Message,
    bool RequiresOtp,
    Guid? ChallengeId,
    string? MaskedDestination,
    string? MockOtp,
    CustomerSessionModel? Session = null,
    [property: System.Text.Json.Serialization.JsonIgnore] string? SessionToken = null);

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
    bool CanUpdateNote,
    IList<CustomerOrderItemModel> Items);

[Serializable]
public sealed record CustomerOrderItemModel(Guid Id, Guid ProductId, string ProductName, decimal Quantity, decimal UnitPrice, decimal SubTotal);

[Serializable]
public sealed record CustomerOrderRequestModel(Guid Id, string Code, int Status, DateTime CreatedOn);

[Serializable]
public sealed record CustomerOrderRequestListModel(IList<CustomerOrderRequestSummaryModel> Items);

[Serializable]
public record CustomerOrderRequestSummaryModel(
    Guid Id,
    string Code,
    int Status,
    decimal? TotalAmount,
    DateTime CreatedOn,
    DateTime? ExpectedShippingDate,
    DateTime? ReviewedOn,
    Guid? ConvertedOrderId,
    bool CanConfirm);

[Serializable]
public sealed record CustomerOrderRequestDetailsModel(
    Guid Id,
    string Code,
    int Status,
    decimal? TotalAmount,
    DateTime CreatedOn,
    DateTime? ExpectedShippingDate,
    DateTime? ReviewedOn,
    Guid? ConvertedOrderId,
    bool CanConfirm,
    string? ShippingAddress,
    string? Note,
    string? AdminNote,
    IList<CustomerOrderRequestItemModel> Items);

[Serializable]
public sealed record CustomerOrderRequestItemModel(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal? UnitPrice,
    decimal? SubTotal,
    bool IsPriced);

[Serializable]
public sealed record CustomerPortalConversionResultModel(bool Success, string? Message, Guid? CreatedId);

[Serializable]
public sealed record CustomerProductListModel(IList<CustomerProductModel> Items, bool HasMore, int PageSize);

[Serializable]
public sealed record CustomerProductModel(Guid Id, string Name, Guid? CategoryId, string? CategoryName, string? PictureUrl, decimal? UnitPrice, bool HasPurchased);

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
public sealed record CustomerDeliveryNoteItemModel(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal SubTotal,
    decimal ReservedReturnQuantity,
    decimal PendingPortalReturnQuantity,
    decimal ReturnableQuantity);

[Serializable]
public sealed record CustomerReturnRequestModel(Guid Id, Guid DeliveryNoteId, int Status, DateTime CreatedOn, bool CompensateInNextDelivery);

[Serializable]
public sealed record CustomerReturnableItemListModel(IList<CustomerReturnableItemModel> Items);

[Serializable]
public sealed record CustomerReturnableItemModel(
    Guid ProductId,
    string ProductName,
    string Unit,
    decimal DeliveredQuantity,
    decimal ReservedReturnQuantity,
    decimal ReturnableQuantity,
    decimal LatestUnitPrice);

[Serializable]
public sealed record CustomerReturnRequestListModel(IList<CustomerReturnRequestSummaryModel> Items);

[Serializable]
public record CustomerReturnRequestSummaryModel(
    Guid Id,
    Guid DeliveryNoteId,
    string? DeliveryNoteCode,
    int Status,
    string? Reason,
    bool CompensateInNextDelivery,
    string? AdminNote,
    DateTime CreatedOn,
    DateTime? ReviewedOn,
    Guid? ConvertedCustomerReturnId,
    decimal TotalRequestedQuantity,
    int ItemCount);

[Serializable]
public sealed record CustomerReturnRequestDetailsModel(
    Guid Id,
    Guid DeliveryNoteId,
    string? DeliveryNoteCode,
    int Status,
    string? Reason,
    bool CompensateInNextDelivery,
    string? AdminNote,
    DateTime CreatedOn,
    DateTime? ReviewedOn,
    Guid? ConvertedCustomerReturnId,
    decimal TotalRequestedQuantity,
    int ItemCount,
    IList<CustomerReturnRequestItemModel> Items);

[Serializable]
public sealed record CustomerReturnRequestItemModel(
    Guid Id,
    Guid DeliveryNoteItemId,
    Guid ProductId,
    string ProductName,
    decimal RequestedQuantity,
    string? Reason,
    IList<CustomerReturnRequestEvidencePictureModel> EvidencePictures);

[Serializable]
public sealed record CustomerReturnRequestEvidencePictureModel(Guid PictureId, string? PictureUrl, string? FileName);

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

[Serializable]
public sealed record CustomerLedgerSummaryModel(
    decimal Balance,
    DateTime? LastEntryOnUtc,
    IList<CustomerLedgerStatementItemModel> RecentEntries);

[Serializable]
public sealed record CustomerLedgerStatementItemModel(
    Guid EntryId,
    int EntryType,
    decimal Amount,
    decimal RunningBalance,
    int ReferenceType,
    Guid? ReferenceId,
    string? ReferenceCode,
    string? Note,
    DateTime OccurredAtUtc);
