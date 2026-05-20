using MediatR;
using NamEcommerce.Customer.Contracts.Models;

namespace NamEcommerce.Customer.Contracts.Queries;

public sealed record GetPublicDeliveryNoteQuery(string Token) : IRequest<PublicDeliveryNoteModel?>;
public sealed record GetCurrentCustomerSessionQuery : IRequest<CustomerSessionModel?>;
public sealed record GetCustomerDashboardQuery : IRequest<CustomerDashboardModel>;
public sealed record GetCustomerOrdersQuery : IRequest<CustomerOrderListModel>;
public sealed record GetCustomerOrderDetailsQuery(Guid OrderId) : IRequest<CustomerOrderDetailsModel?>;
public sealed record GetCustomerDeliveryNotesQuery : IRequest<CustomerDeliveryNoteListModel>;
public sealed record GetCustomerDeliveryNoteDetailsQuery(Guid DeliveryNoteId) : IRequest<CustomerDeliveryNoteDetailsModel?>;
public sealed record GetCustomerDebtsQuery : IRequest<CustomerDebtSummaryModel>;
