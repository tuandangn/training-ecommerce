using MediatR;
using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;
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
    IRequestHandler<GetCustomerOrderRequestsQuery, CustomerOrderRequestListModel>,
    IRequestHandler<GetCustomerOrderRequestDetailsQuery, CustomerOrderRequestDetailsModel?>,
    IRequestHandler<GetCustomerOrderRequestDefaultsQuery, CustomerOrderRequestDefaultsModel>,
    IRequestHandler<GetCustomerProductsQuery, CustomerProductListModel>,
    IRequestHandler<GetCustomerProductCategoriesQuery, CustomerProductCategoryListModel>,
    IRequestHandler<GetCustomerContactQuery, CustomerContactModel>,
    IRequestHandler<GetCustomerDeliveryNotesQuery, CustomerDeliveryNoteListModel>,
    IRequestHandler<GetCustomerDeliveryNoteDetailsQuery, CustomerDeliveryNoteDetailsModel?>,
    IRequestHandler<GetCustomerReturnableItemsQuery, CustomerReturnableItemListModel>,
    IRequestHandler<GetCustomerReturnRequestsQuery, CustomerReturnRequestListModel>,
    IRequestHandler<GetCustomerReturnRequestDetailsQuery, CustomerReturnRequestDetailsModel?>,
    IRequestHandler<GetCustomerDebtsQuery, CustomerDebtSummaryModel>,
    IRequestHandler<GetCustomerLedgerQuery, CustomerLedgerSummaryModel>
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
                order.CanUpdateNote,
                order.Items.Select(item => new CustomerOrderItemModel(item.Id, item.ProductId, item.ProductName, item.Quantity, item.UnitPrice, item.SubTotal)).ToList());
    }

    public async Task<CustomerOrderRequestListModel> Handle(GetCustomerOrderRequestsQuery request, CancellationToken cancellationToken)
    {
        var orderRequests = await portalAppService.GetOrderRequestsAsync(RequireCustomerId()).ConfigureAwait(false);
        return new CustomerOrderRequestListModel(orderRequests.Select(MapOrderRequest).ToList());
    }

    public async Task<CustomerOrderRequestDetailsModel?> Handle(GetCustomerOrderRequestDetailsQuery request, CancellationToken cancellationToken)
    {
        var orderRequest = await portalAppService.GetOrderRequestDetailsAsync(RequireCustomerId(), request.OrderRequestId).ConfigureAwait(false);
        return orderRequest is null ? null : MapOrderRequestDetails(orderRequest);
    }

    public async Task<CustomerOrderRequestDefaultsModel> Handle(GetCustomerOrderRequestDefaultsQuery request, CancellationToken cancellationToken)
    {
        var defaults = await portalAppService.GetOrderRequestDefaultsAsync(RequireCustomerId()).ConfigureAwait(false);
        return new CustomerOrderRequestDefaultsModel(defaults.ShippingAddress, defaults.ShippingAddressSource);
    }

    public async Task<CustomerProductListModel> Handle(GetCustomerProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await portalAppService.GetProductsAsync(RequireCustomerId(), request.CategoryId, request.Keywords, request.PurchasedOnly, request.PageSize).ConfigureAwait(false);
        return new CustomerProductListModel(
            products.Items
                .Select(product => new CustomerProductModel(product.Id, product.Name, product.CategoryId, product.CategoryName, product.PictureUrl, product.UnitPrice, product.HasPurchased))
                .ToList(),
            products.HasMore,
            products.PageSize);
    }

    public async Task<CustomerProductCategoryListModel> Handle(GetCustomerProductCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await portalAppService.GetProductCategoriesAsync().ConfigureAwait(false);
        return new CustomerProductCategoryListModel(categories.Items
            .Select(category => new CustomerProductCategoryModel(category.Id, category.Name, category.ParentId))
            .ToList());
    }

    public async Task<CustomerContactModel> Handle(GetCustomerContactQuery request, CancellationToken cancellationToken)
    {
        var contact = await portalAppService.GetContactAsync().ConfigureAwait(false);
        return new CustomerContactModel(
            new CustomerStoreContactModel(
                contact.Store.StoreName,
                contact.Store.PhoneNumber,
                contact.Store.Address,
                contact.Store.Email,
                contact.Store.MapQuery),
            contact.Warehouses.Select(warehouse => new CustomerWarehouseContactModel(
                    warehouse.Id,
                    warehouse.Name,
                    warehouse.PhoneNumber,
                    warehouse.Address,
                    warehouse.MapQuery))
                .ToList());
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
                note.Items.Select(item => new CustomerDeliveryNoteItemModel(
                    item.Id,
                    item.ProductId,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice,
                    item.SubTotal,
                    item.ReservedReturnQuantity,
                    item.PendingPortalReturnQuantity,
                    item.ReturnableQuantity)).ToList());
    }

    public async Task<CustomerReturnableItemListModel> Handle(GetCustomerReturnableItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await portalAppService.GetReturnableItemsAsync(RequireCustomerId()).ConfigureAwait(false);
        return new CustomerReturnableItemListModel(items.Select(item => new CustomerReturnableItemModel(
            item.ProductId,
            item.ProductName,
            item.Unit,
            item.DeliveredQuantity,
            item.ReservedReturnQuantity,
            item.ReturnableQuantity,
            item.LatestUnitPrice)).ToList());
    }

    public async Task<CustomerReturnRequestListModel> Handle(GetCustomerReturnRequestsQuery request, CancellationToken cancellationToken)
    {
        var requests = await portalAppService.GetReturnRequestsAsync(RequireCustomerId()).ConfigureAwait(false);
        return new CustomerReturnRequestListModel(requests.Select(MapReturnRequestSummary).ToList());
    }

    public async Task<CustomerReturnRequestDetailsModel?> Handle(GetCustomerReturnRequestDetailsQuery request, CancellationToken cancellationToken)
    {
        var returnRequest = await portalAppService.GetReturnRequestDetailsAsync(RequireCustomerId(), request.ReturnRequestId).ConfigureAwait(false);
        return returnRequest is null ? null : MapReturnRequestDetails(returnRequest);
    }

    public async Task<CustomerDebtSummaryModel> Handle(GetCustomerDebtsQuery request, CancellationToken cancellationToken)
    {
        var summary = await portalAppService.GetDebtSummaryAsync(RequireCustomerId()).ConfigureAwait(false);
        return MapDebtSummary(summary);
    }

    public async Task<CustomerLedgerSummaryModel> Handle(GetCustomerLedgerQuery request, CancellationToken cancellationToken)
    {
        var result = await portalAppService.GetLedgerSummaryAsync(RequireCustomerId()).ConfigureAwait(false);
        return new CustomerLedgerSummaryModel(
            result.Balance,
            result.LastEntryOnUtc,
            result.RecentEntries.Select(e => new CustomerLedgerStatementItemModel(
                e.EntryId,
                e.EntryType,
                e.Amount,
                e.RunningBalance,
                e.ReferenceType,
                e.ReferenceId,
                e.ReferenceCode,
                e.Note,
                e.OccurredAtUtc)).ToList());
    }

    private Guid RequireCustomerId()
        => sessionAccessor.CustomerId ?? throw new UnauthorizedAccessException();

    private static CustomerOrderSummaryModel MapOrder(Application.Contracts.Dtos.CustomerPortal.CustomerOrderSummaryAppDto order)
        => new(order.Id, order.Code, order.Status, order.TotalAmount, order.CreatedOnUtc, order.ExpectedShippingDateUtc);

    private static CustomerOrderRequestSummaryModel MapOrderRequest(CustomerOrderRequestSummaryAppDto request)
        => new(
            request.Id,
            request.Code,
            request.Status,
            request.TotalAmount,
            request.CreatedOnUtc,
            request.ExpectedShippingDateUtc,
            request.ReviewedOnUtc,
            request.ConvertedOrderId,
            request.CanConfirm);

    private static CustomerOrderRequestDetailsModel MapOrderRequestDetails(CustomerOrderRequestDetailsAppDto request)
        => new(
            request.Id,
            request.Code,
            request.Status,
            request.TotalAmount,
            request.CreatedOnUtc,
            request.ExpectedShippingDateUtc,
            request.ReviewedOnUtc,
            request.ConvertedOrderId,
            request.CanConfirm,
            request.ShippingAddress,
            request.Note,
            request.AdminNote,
            request.Items.Select(item => new CustomerOrderRequestItemModel(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.SubTotal,
                item.IsPriced)).ToList());

    private static CustomerDeliveryNoteSummaryModel MapDeliveryNote(Application.Contracts.Dtos.CustomerPortal.CustomerDeliveryNoteSummaryAppDto note)
        => new(note.Id, note.Code, note.OrderCode, note.Status, note.DeliveryConfirmationStatus, note.CreatedOnUtc, note.DeliveredOnUtc);

    private static CustomerReturnRequestSummaryModel MapReturnRequestSummary(CustomerReturnRequestSummaryAppDto request)
        => new(
            request.Id,
            request.DeliveryNoteId,
            request.DeliveryNoteCode,
            request.Status,
            request.Reason,
            request.CompensateInNextDelivery,
            request.AdminNote,
            request.CreatedOnUtc,
            request.ReviewedOnUtc,
            request.ConvertedCustomerReturnId,
            request.TotalRequestedQuantity,
            request.ItemCount);

    private static CustomerReturnRequestDetailsModel MapReturnRequestDetails(CustomerReturnRequestDetailsAppDto request)
        => new(
            request.Id,
            request.DeliveryNoteId,
            request.DeliveryNoteCode,
            request.Status,
            request.Reason,
            request.CompensateInNextDelivery,
            request.AdminNote,
            request.CreatedOnUtc,
            request.ReviewedOnUtc,
            request.ConvertedCustomerReturnId,
            request.TotalRequestedQuantity,
            request.ItemCount,
            request.Items.Select(MapReturnRequestItem).ToList());

    private static CustomerReturnRequestItemModel MapReturnRequestItem(CustomerReturnRequestItemAppDto item)
        => new(
            item.Id,
            item.DeliveryNoteItemId,
            item.ProductId,
            item.ProductName,
            item.RequestedQuantity,
            item.Reason,
            item.EvidencePictures.Select(picture => new CustomerReturnRequestEvidencePictureModel(
                    picture.PictureId,
                    picture.PictureUrl,
                    picture.FileName))
                .ToList());

    private static CustomerDebtSummaryModel MapDebtSummary(Application.Contracts.Dtos.CustomerPortal.CustomerDebtSummaryPortalAppDto summary)
        => new(
            summary.TotalDebtAmount,
            summary.TotalPaidAmount,
            summary.TotalRemainingAmount,
            summary.DepositBalance,
            summary.Debts.Select(debt => new CustomerDebtModel(debt.Id, debt.Code, debt.OrderCode, debt.DeliveryNoteCode, debt.TotalAmount, debt.PaidAmount, debt.RemainingAmount, debt.Status, debt.DueDateUtc)).ToList(),
            summary.RecentPayments.Select(payment => new CustomerPaymentModel(payment.Id, payment.Code, payment.Amount, payment.PaymentMethod, payment.PaymentType, payment.PaidOnUtc)).ToList());
}
