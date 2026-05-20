using MediatR;
using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Customer.Contracts.Models;
using NamEcommerce.Customer.Contracts.Queries;
using NamEcommerce.Customer.Framework.Services;

namespace NamEcommerce.Customer.Framework.Queries.Handlers;

public sealed class CustomerPortalQueryHandlers(
    ICustomerPortalAppService portalAppService,
    ICustomerSessionAccessor sessionAccessor) :
    IRequestHandler<GetPublicDeliveryNoteQuery, PublicDeliveryNoteModel?>,
    IRequestHandler<GetCurrentCustomerSessionQuery, CustomerSessionModel?>,
    IRequestHandler<GetCustomerDashboardQuery, CustomerDashboardModel>,
    IRequestHandler<GetCustomerOrdersQuery, CustomerOrderListModel>,
    IRequestHandler<GetCustomerOrderDetailsQuery, CustomerOrderDetailsModel?>,
    IRequestHandler<GetCustomerDeliveryNotesQuery, CustomerDeliveryNoteListModel>,
    IRequestHandler<GetCustomerDeliveryNoteDetailsQuery, CustomerDeliveryNoteDetailsModel?>,
    IRequestHandler<GetCustomerDebtsQuery, CustomerDebtSummaryModel>
{
    public async Task<PublicDeliveryNoteModel?> Handle(GetPublicDeliveryNoteQuery request, CancellationToken cancellationToken)
    {
        var note = await portalAppService.GetPublicDeliveryNoteByTokenAsync(request.Token).ConfigureAwait(false);
        return note is null
            ? null
            : new PublicDeliveryNoteModel(
                note.Id,
                note.Code,
                note.OrderCode,
                note.Status,
                note.DeliveryConfirmationStatus,
                note.CreatedOnUtc,
                note.DeliveredOnUtc,
                note.Items.Select(item => new PublicDeliveryNoteItemModel(item.Id, item.ProductId, item.ProductName, item.Quantity)).ToList());
    }

    public Task<CustomerSessionModel?> Handle(GetCurrentCustomerSessionQuery request, CancellationToken cancellationToken)
        => Task.FromResult(sessionAccessor.CurrentSession);

    public async Task<CustomerDashboardModel> Handle(GetCustomerDashboardQuery request, CancellationToken cancellationToken)
    {
        var dashboard = await portalAppService.GetDashboardAsync(RequireCustomerId()).ConfigureAwait(false);
        return new CustomerDashboardModel(
            dashboard.RecentOrders.Select(MapOrder).ToList(),
            dashboard.RecentDeliveryNotes.Select(MapDeliveryNote).ToList(),
            MapDebtSummary(dashboard.DebtSummary));
    }

    public async Task<CustomerOrderListModel> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await portalAppService.GetOrdersAsync(RequireCustomerId()).ConfigureAwait(false);
        return new CustomerOrderListModel(orders.Select(MapOrder).ToList());
    }

    public async Task<CustomerOrderDetailsModel?> Handle(GetCustomerOrderDetailsQuery request, CancellationToken cancellationToken)
    {
        var order = await portalAppService.GetOrderDetailsAsync(RequireCustomerId(), request.OrderId).ConfigureAwait(false);
        return order is null
            ? null
            : new CustomerOrderDetailsModel(
                order.Id,
                order.Code,
                order.Status,
                order.TotalAmount,
                order.CreatedOnUtc,
                order.ExpectedShippingDateUtc,
                order.ShippingAddress,
                order.Note,
                order.Items.Select(item => new CustomerOrderItemModel(item.Id, item.ProductId, item.ProductName, item.Quantity, item.UnitPrice, item.SubTotal)).ToList());
    }

    public async Task<CustomerDeliveryNoteListModel> Handle(GetCustomerDeliveryNotesQuery request, CancellationToken cancellationToken)
    {
        var notes = await portalAppService.GetDeliveryNotesAsync(RequireCustomerId()).ConfigureAwait(false);
        return new CustomerDeliveryNoteListModel(notes.Select(MapDeliveryNote).ToList());
    }

    public async Task<CustomerDeliveryNoteDetailsModel?> Handle(GetCustomerDeliveryNoteDetailsQuery request, CancellationToken cancellationToken)
    {
        var note = await portalAppService.GetDeliveryNoteDetailsAsync(RequireCustomerId(), request.DeliveryNoteId).ConfigureAwait(false);
        return note is null
            ? null
            : new CustomerDeliveryNoteDetailsModel(
                note.Id,
                note.Code,
                note.OrderCode,
                note.Status,
                note.DeliveryConfirmationStatus,
                note.CreatedOnUtc,
                note.DeliveredOnUtc,
                note.Items.Select(item => new CustomerDeliveryNoteItemModel(item.Id, item.ProductId, item.ProductName, item.Quantity, item.UnitPrice, item.SubTotal)).ToList());
    }

    public async Task<CustomerDebtSummaryModel> Handle(GetCustomerDebtsQuery request, CancellationToken cancellationToken)
    {
        var summary = await portalAppService.GetDebtSummaryAsync(RequireCustomerId()).ConfigureAwait(false);
        return MapDebtSummary(summary);
    }

    private Guid RequireCustomerId()
        => sessionAccessor.CustomerId ?? throw new UnauthorizedAccessException();

    private static CustomerOrderSummaryModel MapOrder(Application.Contracts.Dtos.CustomerPortal.CustomerOrderSummaryAppDto order)
        => new(order.Id, order.Code, order.Status, order.TotalAmount, order.CreatedOnUtc, order.ExpectedShippingDateUtc);

    private static CustomerDeliveryNoteSummaryModel MapDeliveryNote(Application.Contracts.Dtos.CustomerPortal.CustomerDeliveryNoteSummaryAppDto note)
        => new(note.Id, note.Code, note.OrderCode, note.Status, note.DeliveryConfirmationStatus, note.CreatedOnUtc, note.DeliveredOnUtc);

    private static CustomerDebtSummaryModel MapDebtSummary(Application.Contracts.Dtos.CustomerPortal.CustomerDebtSummaryPortalAppDto summary)
        => new(
            summary.TotalDebtAmount,
            summary.TotalPaidAmount,
            summary.TotalRemainingAmount,
            summary.DepositBalance,
            summary.Debts.Select(debt => new CustomerDebtModel(debt.Id, debt.Code, debt.OrderCode, debt.DeliveryNoteCode, debt.TotalAmount, debt.PaidAmount, debt.RemainingAmount, debt.Status, debt.DueDateUtc)).ToList(),
            summary.RecentPayments.Select(payment => new CustomerPaymentModel(payment.Id, payment.Code, payment.Amount, payment.PaymentMethod, payment.PaymentType, payment.PaidOnUtc)).ToList());
}
