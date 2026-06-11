using MediatR;
using NamEcommerce.Customer.Contracts.Models;

namespace NamEcommerce.Customer.Contracts.Queries;

public sealed record GetPublicDeliveryNoteQuery(string Token) : IRequest<PublicDeliveryNoteModel?>;
public sealed record GetCurrentCustomerSessionQuery : IRequest<CustomerSessionModel?>;
public sealed record GetCustomerDashboardQuery : IRequest<CustomerDashboardModel>;
public sealed record GetCustomerOrdersQuery : IRequest<CustomerOrderListModel>;
public sealed record GetCustomerOrderDetailsQuery(Guid OrderId) : IRequest<CustomerOrderDetailsModel?>;
public sealed record GetCustomerOrderRequestsQuery : IRequest<CustomerOrderRequestListModel>;
public sealed record GetCustomerOrderRequestDetailsQuery(Guid OrderRequestId) : IRequest<CustomerOrderRequestDetailsModel?>;
public sealed record GetCustomerOrderRequestDefaultsQuery : IRequest<CustomerOrderRequestDefaultsModel>;
public sealed record GetCustomerProductsQuery(Guid? CategoryId, string? Keywords, bool PurchasedOnly = true, int PageSize = 30) : IRequest<CustomerProductListModel>;
public sealed record GetCustomerProductCategoriesQuery : IRequest<CustomerProductCategoryListModel>;
public sealed record GetCustomerContactQuery : IRequest<CustomerContactModel>;
public sealed record GetCustomerDeliveryNotesQuery : IRequest<CustomerDeliveryNoteListModel>;
public sealed record GetCustomerDeliveryNoteDetailsQuery(Guid DeliveryNoteId) : IRequest<CustomerDeliveryNoteDetailsModel?>;
public sealed record GetCustomerReturnableItemsQuery : IRequest<CustomerReturnableItemListModel>;
public sealed record GetCustomerReturnRequestsQuery : IRequest<CustomerReturnRequestListModel>;
public sealed record GetCustomerReturnRequestDetailsQuery(Guid ReturnRequestId) : IRequest<CustomerReturnRequestDetailsModel?>;
public sealed record GetCustomerDebtsQuery : IRequest<CustomerDebtSummaryModel>;
public sealed record GetCustomerLedgerQuery : IRequest<CustomerLedgerSummaryModel>;
