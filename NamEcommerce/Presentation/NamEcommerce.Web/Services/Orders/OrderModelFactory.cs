using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Customers;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Web.Models.Orders;

namespace NamEcommerce.Web.Services.Orders;

public sealed class OrderModelFactory : IOrderModelFactory
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;
    private readonly IDirectShipAppService _directShipAppService;
    private readonly IWarehouseAppService _warehouseAppService;
    private readonly ICustomerDebtAppService _customerDebtAppService;
    private readonly IExpenseAppService _expenseAppService;
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;

    public OrderModelFactory(
        AppConfig appConfig,
        IMediator mediator,
        IDeliveryNoteAppService deliveryNoteAppService,
        IDirectShipAppService directShipAppService,
        IWarehouseAppService warehouseAppService,
        ICustomerDebtAppService customerDebtAppService,
        IExpenseAppService expenseAppService,
        IGoodsReceiptAppService goodsReceiptAppService)
    {
        _appConfig = appConfig;
        _mediator = mediator;
        _deliveryNoteAppService = deliveryNoteAppService;
        _directShipAppService = directShipAppService;
        _warehouseAppService = warehouseAppService;
        _customerDebtAppService = customerDebtAppService;
        _expenseAppService = expenseAppService;
        _goodsReceiptAppService = goodsReceiptAppService;
    }

    public async Task<CreateOrderModel> PrepareCreateOrderModel(CreateOrderModel? oldModel = null)
    {
        var model = oldModel ?? new CreateOrderModel();
        model.AvailableCategories = await _mediator.Send(new GetCategoryOptionListQuery()).ConfigureAwait(false);
        model.AvailableUnitMeasurements = await _mediator.Send(new GetUnitMeasurementOptionListQuery()).ConfigureAwait(false);
        model.AvailableVendors = await _mediator.Send(new GetVendorOptionListQuery()).ConfigureAwait(false);

        if (model.CustomerId.HasValue)
        {
            var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = model.CustomerId.Value }).ConfigureAwait(false);
            if (customer is null)
                model.CustomerId = null;
            else
            {
                model.CustomerDisplayName = customer.FullName;
                model.CustomerDisplayPhone = customer.PhoneNumber;
                model.CustomerDisplayAddress = customer.Address;
            }
        }

        if (model.Items.Count > 0)
        {
            var productIds = model.Items.Select(i => i.ProductId).OfType<Guid>().ToList();
            if (productIds.Count > 0)
            {
                var products = await _mediator.Send(new GetProductsByIdsForOrderQuery
                {
                    Ids = productIds
                }).ConfigureAwait(false);

                model.Items = model.Items.Where(i => products.Any(p => p.Id == i.ProductId)).ToList();

                foreach (var item in model.Items)
                {
                    var product = products.First(p => p.Id == item.ProductId);
                    item.ProductDisplayName = product.Name;
                    item.ProductDisplayQty = product.QuantityAvailable;
                    item.ProductDisplayPicture = product.PictureUrl;
                }
            }
            else
            {
                model.Items.Clear();
            }
        }

        return model;
    }

    public async Task<OrderDetailsModel?> PrepareOrderDetailsModel(Guid orderId)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery { Id = orderId }).ConfigureAwait(false);
        if (order is null)
            return null;

        var model = new OrderDetailsModel
        {
            Id = order.Id,
            Code = order.Code,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            OrderSubTotal = order.OrderSubTotal,
            TotalAmount = order.TotalAmount,
            OrderDiscount = order.OrderDiscount,
            Status = order.Status,
            Note = order.Note,
            ExpectedShippingDate = order.ExpectedShippingDate,
            ShippingAddress = order.ShippingAddress,
            CompletedOn = order.CompletedOn,
            CustomerAddress = order.CustomerAddress,
            CustomerPhoneNumber = order.CustomerPhoneNumber,
            CanUpdateInfo = order.CanUpdateInfo,
            CanUpdateOrderItems = order.CanUpdateOrderItems,
            CanCompleteOrder = order.CanCompleteOrder,
            CreatedOn = order.CreatedOn
        };
        foreach (var it in order.Items)
        {
            model.Items.Add(new OrderDetailsModel.OrderItemModel(it.Id)
            {
                ProductId = it.ProductId,
                ProductName = it.ProductName,
                ProductPicture = it.ProductPicture,
                ProductAvailableQty = it.ProductAvailableQty,
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice
            });
        }

        var deliveryNotes = await _deliveryNoteAppService.GetByOrderIdAsync(orderId).ConfigureAwait(false);
        foreach (var dn in deliveryNotes)
        {
            if (dn.Status == (int)DeliveryNoteStatus.Cancelled)
                continue;
            var dnModel = new OrderDetailsModel.DeliveryNoteBasicModel
            {
                Id = dn.Id,
                Code = dn.Code,
                Status = dn.Status,
                SourceType = dn.SourceType,
                IsDirectShip = dn.IsDirectShip,
                WarehouseName = dn.WarehouseName,
                CreatedOn = dn.CreatedOnUtc.ToLocalTime(),
                DeliveredOn = dn.DeliveredOnUtc?.ToLocalTime()
            };

            var returnedQuantities = await _mediator.Send(new GetReturnedQuantitiesByDeliveryNoteQuery
            {
                DeliveryNoteId = dn.Id
            }).ConfigureAwait(false);

            foreach (var item in dn.Items)
            {
                returnedQuantities.TryGetValue(item.Id, out var returnSummary);
                dnModel.Items.Add(new OrderDetailsModel.DeliveryNoteItemModel
                {
                    OrderItemId = item.OrderItemId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SubTotal = item.SubTotal,
                    CostAtDispatch = item.CostAtDispatch,
                    ConfirmedReturnQuantity = returnSummary?.ConfirmedQuantity ?? 0m,
                    PendingReturnQuantity = returnSummary?.PendingQuantity ?? 0m,
                    CompensatedReturnQuantity = returnSummary?.ActiveCompensatedQuantity ?? 0m
                });
            }

            model.DeliveryNotes.Add(dnModel);
        }

        model.ShortageInfo = await _mediator.Send(new GetOrderShortageInfoQuery
        {
            OrderId = orderId
        }).ConfigureAwait(false);

        model.AllocatedPurchaseOrders = await _mediator.Send(new GetAllocatedPurchaseOrdersForOrderQuery
        {
            OrderId = orderId
        }).ConfigureAwait(false);

        var orderItemIds = order.Items.Select(i => i.Id).ToList();
        if (orderItemIds.Count > 0)
        {
            var dsAllocations = await _directShipAppService
                .GetDirectShipAllocationsForOrderAsync(orderItemIds)
                .ConfigureAwait(false);

            foreach (var alloc in dsAllocations)
            {
                var orderItem = order.Items.FirstOrDefault(i => i.Id == alloc.OrderItemId);

                model.DirectShipAllocations.Add(new OrderDetailsModel.DirectShipAllocationModel
                {
                    AllocationId = alloc.AllocationId,
                    OrderItemId = alloc.OrderItemId,
                    ProductName = orderItem?.ProductName ?? string.Empty,
                    Status = alloc.Status,
                    DeliveryStatus = alloc.DeliveryStatus,
                    AllocatedQuantity = alloc.AllocatedQuantity,
                    ReceivedQuantity = alloc.ReceivedQuantity,
                    DeliveryNoteId = alloc.DeliveryNoteId,
                    DeliveryNoteCode = alloc.DeliveryNoteCode
                });
            }
        }

        model.CanCancelOrder = order.Status != (int)OrderStatus.Completed
            && order.Status != (int)OrderStatus.Cancelled;
        model.CanDeleteOrder = order.Status is (int)OrderStatus.Pending or (int)OrderStatus.Cancelled;
        model.FullyReceivedDirectShipCount = model.DirectShipAllocations.Count(a =>
            a.ReceivedQuantity > 0 &&
            (!a.DeliveryStatus.HasValue || a.DeliveryStatus == (int)DeliveryNoteStatus.Confirmed));
        model.ReturnWarehouseOptions = await GetReturnWarehouseOptionsAsync().ConfigureAwait(false);
        await PrepareWorkflowAsync(model).ConfigureAwait(false);

        return model;
    }

    public async Task<OrderListModel> PrepareOrderListModel(OrderListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;
        if (_appConfig.PageSizeOptions.Contains(pageSize)) pageSize = _appConfig.DefaultPageSize;

        var model = await _mediator.Send(new GetOrderListQuery
        {
            Keywords = searchModel?.Keywords,
            Status = searchModel?.Status,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return model;
    }

    private async Task<IList<OrderDetailsModel.ReturnWarehouseOptionModel>> GetReturnWarehouseOptionsAsync()
    {
        var warehouses = await _warehouseAppService.GetWarehousesAsync(0, int.MaxValue).ConfigureAwait(false);
        return warehouses
            .OrderBy(w => w.Name)
            .Select(w => new OrderDetailsModel.ReturnWarehouseOptionModel
            {
                Id = w.Id,
                Name = w.Name
            })
            .ToList();
    }

    private async Task PrepareWorkflowAsync(OrderDetailsModel model)
    {
        var deliveryStatus = CalculateDeliveryStatus(model);
        var activeStage = CalculateActiveStage(model, deliveryStatus);
        var relatedReceipts = await GetRelatedGoodsReceiptsAsync(model).ConfigureAwait(false);
        var customerDebts = await _customerDebtAppService.GetDebtsByCustomerIdAsync(model.CustomerId).ConfigureAwait(false);
        var orderDebts = customerDebts?.Debts.Where(debt => debt.OrderId == model.Id).ToList() ?? [];
        var expenses = await _expenseAppService.GetExpensesByOrderIdAsync(model.Id).ConfigureAwait(false);

        model.CanCompleteOrder = model.CanCompleteOrder && deliveryStatus == OrderDetailsModel.OrderDeliverySummaryStatus.Delivered;
        model.Workflow = BuildWorkflow(model, deliveryStatus, activeStage);
        model.Preparation = BuildPreparation(model);
        model.DeliveryWorkflow = BuildDeliveryWorkflow(model, deliveryStatus);
        model.Settlement = BuildSettlement(model, orderDebts, expenses);
        model.Timeline = BuildTimeline(model, relatedReceipts, orderDebts);
    }

    private async Task<IDictionary<Guid, IList<GoodsReceiptAppDto>>> GetRelatedGoodsReceiptsAsync(OrderDetailsModel model)
    {
        if (model.AllocatedPurchaseOrders?.Items.Count is not > 0)
            return new Dictionary<Guid, IList<GoodsReceiptAppDto>>();

        var tasks = model.AllocatedPurchaseOrders.Items
            .Select(async purchaseOrder => new
            {
                purchaseOrder.PurchaseOrderId,
                Receipts = await _goodsReceiptAppService
                    .GetGoodsReceiptsByPurchaseOrderIdAsync(purchaseOrder.PurchaseOrderId)
                    .ConfigureAwait(false)
            })
            .ToList();

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.ToDictionary(item => item.PurchaseOrderId, item => item.Receipts);
    }

    private static OrderDetailsModel.OrderDeliverySummaryStatus CalculateDeliveryStatus(OrderDetailsModel model)
    {
        if (model.Items.Count == 0)
            return OrderDetailsModel.OrderDeliverySummaryStatus.Pending;

        var validNotes = model.DeliveryNotes
            .Where(deliveryNote => deliveryNote.Status != (int)DeliveryNoteStatus.Cancelled)
            .ToList();

        var deliveredAll = model.Items.All(item => item.GetDeliveredToCustomerQuantity(validNotes) >= item.Quantity);
        if (deliveredAll)
            return OrderDetailsModel.OrderDeliverySummaryStatus.Delivered;

        var deliveredAny = model.Items.Any(item => item.GetDeliveredToCustomerQuantity(validNotes) > 0);
        if (deliveredAny)
            return OrderDetailsModel.OrderDeliverySummaryStatus.PartialDelivered;

        return validNotes.Count > 0 || model.DirectShipAllocations.Count > 0
            ? OrderDetailsModel.OrderDeliverySummaryStatus.Shipping
            : OrderDetailsModel.OrderDeliverySummaryStatus.Pending;
    }

    private static OrderDetailsModel.WorkflowStage CalculateActiveStage(
        OrderDetailsModel model,
        OrderDetailsModel.OrderDeliverySummaryStatus deliveryStatus)
    {
        if (model.Status is (int)OrderStatus.Completed or (int)OrderStatus.Cancelled
            || deliveryStatus == OrderDetailsModel.OrderDeliverySummaryStatus.Delivered)
            return OrderDetailsModel.WorkflowStage.Settlement;

        if (deliveryStatus is OrderDetailsModel.OrderDeliverySummaryStatus.Shipping
            or OrderDetailsModel.OrderDeliverySummaryStatus.PartialDelivered)
            return OrderDetailsModel.WorkflowStage.Delivery;

        var hasPreparationWork = model.ShortageInfo.HasShortage
            || (model.AllocatedPurchaseOrders?.Items.Any(purchaseOrder => !purchaseOrder.IsFullyReceived) ?? false)
            || model.DirectShipAllocations.Any(allocation => allocation.ReceivedQuantity < allocation.AllocatedQuantity);

        return hasPreparationWork
            ? OrderDetailsModel.WorkflowStage.Preparation
            : OrderDetailsModel.WorkflowStage.Delivery;
    }

    private static OrderDetailsModel.WorkflowModel BuildWorkflow(
        OrderDetailsModel model,
        OrderDetailsModel.OrderDeliverySummaryStatus deliveryStatus,
        OrderDetailsModel.WorkflowStage activeStage)
    {
        var hasOpenPreparation = model.ShortageInfo.HasShortage
            || (model.AllocatedPurchaseOrders?.Items.Any(purchaseOrder => !purchaseOrder.IsFullyReceived) ?? false)
            || model.DirectShipAllocations.Any(allocation => allocation.ReceivedQuantity < allocation.AllocatedQuantity);

        var deliveryStatusText = GetDeliverySummaryText(deliveryStatus);
        var deliveryStatusClass = GetDeliverySummaryClass(deliveryStatus);

        return new OrderDetailsModel.WorkflowModel
        {
            ActiveStage = activeStage,
            DeliveryStatus = deliveryStatus,
            DeliveryStatusText = deliveryStatusText,
            DeliveryStatusClass = deliveryStatusClass,
            Steps =
            [
                new OrderDetailsModel.WorkflowStepModel
                {
                    Stage = OrderDetailsModel.WorkflowStage.Order,
                    Key = "order",
                    Title = "Đặt hàng",
                    Icon = "bi-receipt",
                    Summary = $"{model.Items.Count} mặt hàng",
                    IsActive = activeStage == OrderDetailsModel.WorkflowStage.Order,
                    IsComplete = model.Items.Count > 0
                },
                new OrderDetailsModel.WorkflowStepModel
                {
                    Stage = OrderDetailsModel.WorkflowStage.Preparation,
                    Key = "preparation",
                    Title = "Chuẩn bị hàng",
                    Icon = "bi-box-seam",
                    Summary = hasOpenPreparation ? "Đang xử lý" : "Đủ hàng",
                    IsActive = activeStage == OrderDetailsModel.WorkflowStage.Preparation,
                    IsComplete = !hasOpenPreparation,
                    HasWarnings = model.ShortageInfo.HasShortage
                },
                new OrderDetailsModel.WorkflowStepModel
                {
                    Stage = OrderDetailsModel.WorkflowStage.Delivery,
                    Key = "delivery",
                    Title = "Giao hàng",
                    Icon = "bi-truck",
                    Summary = deliveryStatusText,
                    IsActive = activeStage == OrderDetailsModel.WorkflowStage.Delivery,
                    IsComplete = deliveryStatus == OrderDetailsModel.OrderDeliverySummaryStatus.Delivered
                },
                new OrderDetailsModel.WorkflowStepModel
                {
                    Stage = OrderDetailsModel.WorkflowStage.Settlement,
                    Key = "settlement",
                    Title = "Kết sổ",
                    Icon = "bi-clipboard-check",
                    Summary = ((OrderStatus)model.Status).GetDisplayText(),
                    IsActive = activeStage == OrderDetailsModel.WorkflowStage.Settlement,
                    IsComplete = model.Status == (int)OrderStatus.Completed
                }
            ]
        };
    }

    private static OrderDetailsModel.PreparationModel BuildPreparation(OrderDetailsModel model)
    {
        var validNotes = model.DeliveryNotes
            .Where(deliveryNote => deliveryNote.Status != (int)DeliveryNoteStatus.Cancelled)
            .ToList();
        var purchaseRows = BuildPreparationPurchaseOrderRows(model);

        return new OrderDetailsModel.PreparationModel
        {
            HasShortage = model.ShortageInfo.HasShortage,
            TotalShortageQuantity = model.ShortageInfo.TotalShortageQuantity,
            PurchaseOrders = purchaseRows,
            Items = model.Items.Select(item =>
            {
                var shortageItem = model.ShortageInfo.Items.FirstOrDefault(shortage => shortage.OrderItemId == item.Id);
                var directShipAllocations = model.DirectShipAllocations
                    .Where(allocation => allocation.OrderItemId == item.Id)
                    .ToList();

                return new OrderDetailsModel.PreparationItemModel
                {
                    OrderItemId = item.Id,
                    ProductName = item.ProductName ?? string.Empty,
                    OrderedQuantity = item.Quantity,
                    AvailableQuantity = Math.Max(0, item.ProductAvailableQty ?? shortageItem?.AvailableQuantity ?? 0),
                    ShortageQuantity = shortageItem?.ShortageQuantity ?? 0,
                    IssuedQuantity = item.GetDeliveredQuantity(validNotes),
                    DeliveredQuantity = item.GetDeliveredToCustomerQuantity(validNotes),
                    CompensatedReturnQuantity = item.GetCompensatedReturnQuantity(validNotes),
                    DirectShipQuantity = directShipAllocations.Sum(allocation => allocation.AllocatedQuantity),
                    DirectShipReceivedQuantity = directShipAllocations.Sum(allocation => allocation.ReceivedQuantity),
                    DirectShipStatusText = GetDirectShipStatusText(directShipAllocations),
                    UnitCost = shortageItem?.UnitCost,
                    RelatedPurchaseOrders = purchaseRows
                        .Where(row => row.ProductName == (item.ProductName ?? string.Empty) || row.PendingQuantity > 0)
                        .Where(row => model.AllocatedPurchaseOrders?.Items
                            .Any(purchaseOrder => purchaseOrder.PurchaseOrderId == row.PurchaseOrderId
                                && purchaseOrder.Items.Any(purchaseItem => purchaseItem.OrderItemId == item.Id)) == true)
                        .ToList()
                };
            }).ToList()
        };
    }

    private static IList<OrderDetailsModel.PreparationPurchaseOrderModel> BuildPreparationPurchaseOrderRows(OrderDetailsModel model)
    {
        if (model.AllocatedPurchaseOrders?.Items.Count is not > 0)
            return [];

        return model.AllocatedPurchaseOrders.Items
            .SelectMany(purchaseOrder => purchaseOrder.Items.Select(item => new OrderDetailsModel.PreparationPurchaseOrderModel
            {
                PurchaseOrderId = purchaseOrder.PurchaseOrderId,
                PurchaseOrderCode = purchaseOrder.PurchaseOrderCode,
                VendorName = purchaseOrder.VendorName,
                ProductName = item.ProductName,
                StatusText = purchaseOrder.StatusName,
                StatusClass = purchaseOrder.StatusClass,
                PlacedOn = purchaseOrder.PlacedOn,
                ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
                AllocatedQuantity = item.AllocatedQuantity,
                ReceivedQuantity = item.ReceivedQuantity
            }))
            .ToList();
    }

    private static OrderDetailsModel.DeliveryWorkflowModel BuildDeliveryWorkflow(
        OrderDetailsModel model,
        OrderDetailsModel.OrderDeliverySummaryStatus deliveryStatus)
    {
        return new OrderDetailsModel.DeliveryWorkflowModel
        {
            Status = deliveryStatus,
            StatusText = GetDeliverySummaryText(deliveryStatus),
            StatusClass = GetDeliverySummaryClass(deliveryStatus),
            Notes = model.DeliveryNotes
                .OrderBy(deliveryNote => deliveryNote.CreatedOn)
                .Select(deliveryNote => new OrderDetailsModel.DeliveryProgressModel
                {
                    DeliveryNoteId = deliveryNote.Id,
                    Code = deliveryNote.Code,
                    StatusText = GetDeliveryNoteStatusText((DeliveryNoteStatus)deliveryNote.Status),
                    StatusClass = GetDeliveryNoteStatusClass((DeliveryNoteStatus)deliveryNote.Status),
                    IsDirectShip = deliveryNote.IsDirectShip,
                    WarehouseName = deliveryNote.WarehouseName,
                    CreatedOn = deliveryNote.CreatedOn,
                    DeliveredOn = deliveryNote.DeliveredOn,
                    TotalQuantity = deliveryNote.Items.Sum(item => item.Quantity),
                    CompensatedReturnQuantity = deliveryNote.Items.Sum(item => item.CompensatedReturnQuantity),
                    TotalAmount = deliveryNote.Items.Sum(item => item.SubTotal)
                })
                .ToList()
        };
    }

    private static OrderDetailsModel.SettlementModel BuildSettlement(
        OrderDetailsModel model,
        IEnumerable<CustomerDebtAppDto> debts,
        IEnumerable<ExpenseAppDto> expenses)
    {
        var debtRows = debts
            .OrderBy(debt => debt.CreatedOnUtc)
            .Select(debt => new OrderDetailsModel.SettlementDebtModel
            {
                Id = debt.Id,
                Code = debt.Code,
                DeliveryNoteCode = debt.DeliveryNoteCode,
                TotalAmount = debt.TotalAmount,
                PaidAmount = debt.PaidAmount,
                RemainingAmount = debt.RemainingAmount,
                StatusText = GetDebtStatusText((DebtStatus)debt.Status),
                DueDate = debt.DueDateUtc?.ToLocalTime(),
                CreatedOn = debt.CreatedOnUtc.ToLocalTime()
            })
            .ToList();

        var expenseRows = expenses
            .OrderBy(expense => expense.IncurredDate)
            .Select(expense => new OrderDetailsModel.SettlementExpenseModel
            {
                Id = expense.Id,
                Title = expense.Title,
                Description = expense.Description,
                ExpenseTypeText = GetExpenseTypeText((ExpenseType)expense.ExpenseType),
                Amount = expense.Amount,
                IncurredDate = expense.IncurredDate.ToLocalTime()
            })
            .ToList();

        var costRows = model.DeliveryNotes
            .Where(deliveryNote => deliveryNote.Status != (int)DeliveryNoteStatus.Cancelled)
            .SelectMany(deliveryNote => deliveryNote.Items.Select(item => new OrderDetailsModel.SettlementCostModel
            {
                DeliveryNoteId = deliveryNote.Id,
                DeliveryNoteCode = deliveryNote.Code,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitCost = item.CostAtDispatch,
                TotalCost = item.TotalCost,
                DispatchedOn = deliveryNote.CreatedOn,
                IsDirectShip = deliveryNote.IsDirectShip
            }))
            .OrderBy(row => row.DispatchedOn)
            .ToList();

        var revenue = model.Items
            .Select(item => (item, deliveredQty: item.GetDeliveredToCustomerQuantity(model.DeliveryNotes), item.UnitPrice))
            .Where(info => info.deliveredQty > 0)
            .Sum(info => info.deliveredQty * info.UnitPrice);
        var totalCost = costRows.Sum(row => row.TotalCost ?? 0);
        var totalExpenses = expenseRows.Sum(row => row.Amount);

        return new OrderDetailsModel.SettlementModel
        {
            Revenue = revenue,
            TotalCost = totalCost,
            TotalExpenses = totalExpenses,
            Profit = revenue - totalCost - totalExpenses,
            IsProfitFinal = costRows.Count > 0 && costRows.All(row => row.UnitCost.HasValue),
            Debts = debtRows,
            Expenses = expenseRows,
            Costs = costRows
        };
    }

    private static IList<OrderDetailsModel.TimelineEventModel> BuildTimeline(
        OrderDetailsModel model,
        IDictionary<Guid, IList<GoodsReceiptAppDto>> relatedReceipts,
        IEnumerable<CustomerDebtAppDto> debts)
    {
        var timeline = new List<OrderDetailsModel.TimelineEventModel>
        {
            new()
            {
                OccurredOn = model.CreatedOn,
                Title = "Khách hàng đặt hàng",
                Description = $"{model.CustomerName} - {model.Items.Count} mặt hàng",
                Icon = "bi-receipt",
                Tone = "primary",
                Stage = OrderDetailsModel.WorkflowStage.Order
            }
        };

        if (model.AllocatedPurchaseOrders?.Items.Count > 0)
        {
            foreach (var purchaseOrder in model.AllocatedPurchaseOrders.Items)
            {
                timeline.Add(new OrderDetailsModel.TimelineEventModel
                {
                    OccurredOn = purchaseOrder.PlacedOn,
                    Title = "Tạo đơn nhập",
                    Description = $"{purchaseOrder.PurchaseOrderCode} - {purchaseOrder.VendorName} - {purchaseOrder.StatusName}",
                    Icon = "bi-box-arrow-in-down",
                    Tone = purchaseOrder.StatusClass,
                    Stage = OrderDetailsModel.WorkflowStage.Preparation
                });

                if (!relatedReceipts.TryGetValue(purchaseOrder.PurchaseOrderId, out var receipts))
                    continue;

                foreach (var receipt in receipts)
                {
                    timeline.Add(new OrderDetailsModel.TimelineEventModel
                    {
                        OccurredOn = receipt.ReceivedOnUtc.ToLocalTime(),
                        Title = "Nhận hàng từ đơn nhập",
                        Description = $"{receipt.Code} - {purchaseOrder.PurchaseOrderCode}",
                        Icon = "bi-clipboard2-check",
                        Tone = receipt.IsPendingCosting ? "warning" : "success",
                        Stage = OrderDetailsModel.WorkflowStage.Preparation
                    });
                }
            }
        }

        foreach (var deliveryNote in model.DeliveryNotes)
        {
            timeline.Add(new OrderDetailsModel.TimelineEventModel
            {
                OccurredOn = deliveryNote.CreatedOn,
                Title = "Tạo phiếu giao",
                Description = $"{deliveryNote.Code} - {GetDeliveryNoteStatusText((DeliveryNoteStatus)deliveryNote.Status)}",
                Icon = "bi-truck",
                Tone = GetDeliveryNoteStatusClass((DeliveryNoteStatus)deliveryNote.Status),
                Stage = OrderDetailsModel.WorkflowStage.Delivery
            });

            if (deliveryNote.DeliveredOn.HasValue)
            {
                timeline.Add(new OrderDetailsModel.TimelineEventModel
                {
                    OccurredOn = deliveryNote.DeliveredOn.Value,
                    Title = "Giao xong",
                    Description = deliveryNote.Code,
                    Icon = "bi-check-circle",
                    Tone = "success",
                    Stage = OrderDetailsModel.WorkflowStage.Delivery
                });
            }
        }

        foreach (var debt in debts)
        {
            timeline.Add(new OrderDetailsModel.TimelineEventModel
            {
                OccurredOn = debt.CreatedOnUtc.ToLocalTime(),
                Title = "Ghi nhận công nợ",
                Description = $"{debt.Code} - {debt.RemainingAmount.DisplayCurrencyWithSymbol()} còn lại",
                Icon = "bi-cash-coin",
                Tone = debt.RemainingAmount > 0 ? "warning" : "success",
                Stage = OrderDetailsModel.WorkflowStage.Settlement
            });
        }

        foreach (var expense in model.Settlement.Expenses)
        {
            timeline.Add(new OrderDetailsModel.TimelineEventModel
            {
                OccurredOn = expense.IncurredDate,
                Title = "Thêm chi phí phát sinh",
                Description = $"{expense.Title} - {expense.Amount.DisplayCurrencyWithSymbol()}",
                Icon = "bi-wallet2",
                Tone = "danger",
                Stage = OrderDetailsModel.WorkflowStage.Settlement
            });
        }

        if (model.CompletedOn.HasValue)
        {
            timeline.Add(new OrderDetailsModel.TimelineEventModel
            {
                OccurredOn = model.CompletedOn.Value,
                Title = "Hoàn thành đơn",
                Description = "Đơn đã được kiểm tra và kết sổ",
                Icon = "bi-lock",
                Tone = "success",
                Stage = OrderDetailsModel.WorkflowStage.Settlement
            });
        }
        else if (model.Status == (int)OrderStatus.Cancelled)
        {
            timeline.Add(new OrderDetailsModel.TimelineEventModel
            {
                OccurredOn = model.CreatedOn,
                Title = "Đơn đã hủy",
                Description = "Chưa có ngày hủy riêng trong dữ liệu hiện tại",
                Icon = "bi-x-circle",
                Tone = "danger",
                Stage = OrderDetailsModel.WorkflowStage.Settlement
            });
        }

        return timeline
            .OrderBy(item => item.OccurredOn)
            .ToList();
    }

    private static string GetDeliverySummaryText(OrderDetailsModel.OrderDeliverySummaryStatus status)
        => status switch
        {
            OrderDetailsModel.OrderDeliverySummaryStatus.Pending => "Chưa giao",
            OrderDetailsModel.OrderDeliverySummaryStatus.Shipping => "Đang giao",
            OrderDetailsModel.OrderDeliverySummaryStatus.PartialDelivered => "Giao một phần",
            OrderDetailsModel.OrderDeliverySummaryStatus.Delivered => "Đã giao đủ",
            _ => status.ToString()
        };

    private static string GetDeliverySummaryClass(OrderDetailsModel.OrderDeliverySummaryStatus status)
        => status switch
        {
            OrderDetailsModel.OrderDeliverySummaryStatus.Pending => "secondary",
            OrderDetailsModel.OrderDeliverySummaryStatus.Shipping => "info",
            OrderDetailsModel.OrderDeliverySummaryStatus.PartialDelivered => "warning",
            OrderDetailsModel.OrderDeliverySummaryStatus.Delivered => "success",
            _ => "secondary"
        };

    private static string GetDeliveryNoteStatusText(DeliveryNoteStatus status)
        => status switch
        {
            DeliveryNoteStatus.Draft => "Bản nháp",
            DeliveryNoteStatus.Confirmed => "Đã xác nhận",
            DeliveryNoteStatus.Delivering => "Đang giao",
            DeliveryNoteStatus.Delivered => "Đã giao",
            DeliveryNoteStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };

    private static string GetDeliveryNoteStatusClass(DeliveryNoteStatus status)
        => status switch
        {
            DeliveryNoteStatus.Draft => "secondary",
            DeliveryNoteStatus.Confirmed => "info",
            DeliveryNoteStatus.Delivering => "warning",
            DeliveryNoteStatus.Delivered => "success",
            DeliveryNoteStatus.Cancelled => "danger",
            _ => "secondary"
        };

    private static string GetDirectShipStatusText(IList<OrderDetailsModel.DirectShipAllocationModel> allocations)
    {
        if (allocations.Count == 0)
            return string.Empty;

        if (allocations.Any(allocation => allocation.Status == (int)AllocationStatus.DeliveryConfirmed))
            return "Đã giao thẳng";
        if (allocations.Any(allocation => allocation.Status == (int)AllocationStatus.DeliveryPending))
            return "Chờ khách xác nhận";
        if (allocations.Any(allocation => allocation.Status == (int)AllocationStatus.FullyReceived))
            return "Đã nhận từ NCC";
        if (allocations.Any(allocation => allocation.Status == (int)AllocationStatus.PartiallyReceived))
            return "Nhận một phần";
        if (allocations.Any(allocation => allocation.Status == (int)AllocationStatus.Cancelled))
            return "Đã hủy";

        return "Đã phân bổ giao thẳng";
    }

    private static string GetDebtStatusText(DebtStatus status)
        => status switch
        {
            DebtStatus.Outstanding => "Chưa thanh toán",
            DebtStatus.PartiallyPaid => "Thanh toán một phần",
            DebtStatus.FullyPaid => "Đã thanh toán",
            _ => status.ToString()
        };

    private static string GetExpenseTypeText(ExpenseType type)
        => type switch
        {
            ExpenseType.Payroll => "Lương thưởng",
            ExpenseType.Rent => "Mặt bằng",
            ExpenseType.Marketing => "Tiếp thị",
            ExpenseType.Utilities => "Điện nước",
            ExpenseType.ReturnCost => "Chi phí trả hàng",
            _ => "Khác"
        };
}
