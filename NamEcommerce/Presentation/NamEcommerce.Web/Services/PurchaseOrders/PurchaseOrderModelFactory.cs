using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.Returns;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Web.Models.PurchaseOrders;

namespace NamEcommerce.Web.Services.PurchaseOrders;

public sealed class PurchaseOrderModelFactory : IPurchaseOrderModelFactory
{
    private readonly IMediator _mediator;
    private readonly AppConfig _appConfig;
    private readonly IDirectShipAppService _directShipAppService;
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;
    private readonly IPurchaseOrderAuditAppService _purchaseOrderAuditAppService;
    private readonly IVendorDebtAppService _vendorDebtAppService;

    public PurchaseOrderModelFactory(
        IMediator mediator,
        AppConfig appConfig,
        IDirectShipAppService directShipAppService,
        IPurchaseOrderAppService purchaseOrderAppService,
        IPurchaseOrderAuditAppService purchaseOrderAuditAppService,
        IVendorDebtAppService vendorDebtAppService)
    {
        _mediator = mediator;
        _appConfig = appConfig;
        _directShipAppService = directShipAppService;
        _purchaseOrderAppService = purchaseOrderAppService;
        _purchaseOrderAuditAppService = purchaseOrderAuditAppService;
        _vendorDebtAppService = vendorDebtAppService;
    }

    public async Task<PurchaseOrderListModel> PreparePurchaseOrderListModel(PurchaseOrderListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;
        if (_appConfig.PageSizeOptions.Contains(pageSize)) pageSize = _appConfig.DefaultPageSize;

        var model = await _mediator.Send(new GetPurchaseOrderListQuery
        {
            Keywords = searchModel?.Keywords,
            Status = (int?)searchModel?.Status,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return model;
    }

    public async Task<CreatePurchaseOrderModel> PrepareCreatePurchaseOrderModel(CreatePurchaseOrderModel? oldModel = null)
    {
        var model = oldModel ?? new CreatePurchaseOrderModel
        {
            PlacedOn = DateTime.Now
        };

        if (model.VendorId.HasValue)
        {
            var vendor = await _mediator.Send(new GetVendorQuery
            {
                Id = model.VendorId.Value
            }).ConfigureAwait(false);
            if (vendor is not null)
            {
                model.VendorName = vendor.Name;
                model.VendorPhone = vendor.PhoneNumber;
                model.VendorAddress = vendor.Address;
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
                    item.ProductDisplayPicture = product.PictureUrl;
                    item.AvailableVendors = product.AvailableVendors;
                }
            }
            else
            {
                model.Items.Clear();
            }
        }

        return model;
    }

    public async Task<QuickCreatePurchaseOrderModel> PrepareQuickCreatePurchaseOrderModel()
    {
        var warehouses = await _mediator.Send(new GetWarehouseOptionListQuery()).ConfigureAwait(false);
        return new QuickCreatePurchaseOrderModel
        {
            ReceivedOn = DateTime.Now,
            AvailableWarehouses = warehouses.Options
        };
    }

    public async Task<PurchaseOrderDetailsModel?> PreparePurchaseOrderDetailsModel(Guid id)
    {
        var purchaseOrderInfo = await _mediator.Send(new GetPurchaseOrderQuery { Id = id }).ConfigureAwait(false);
        if (purchaseOrderInfo == null)
            return null;

        var warehouseTask = _mediator.Send(new GetWarehouseOptionListQuery());
        var receiptsTask = _mediator.Send(new GetRelatedGoodsReceiptsByPurchaseOrderQuery { PurchaseOrderId = id });
        var returnsTask = _mediator.Send(new GetRelatedVendorReturnsByPurchaseOrderQuery { PurchaseOrderId = id });
        var auditsTask = _purchaseOrderAuditAppService.GetPurchaseOrderItemChangeAuditsAsync(id);
        var vendorDebtsTask = _vendorDebtAppService.GetDebtsByVendorIdAsync(purchaseOrderInfo.VendorId);
        await Task.WhenAll(warehouseTask, receiptsTask, returnsTask, auditsTask, vendorDebtsTask).ConfigureAwait(false);

        var availableWarehouses = await warehouseTask;
        var relatedReceipts = await receiptsTask;
        var relatedReturns = await returnsTask;
        var itemChangeAudits = await auditsTask;
        var vendorDebts = await vendorDebtsTask;

        var model = new PurchaseOrderDetailsModel
        {
            Info = purchaseOrderInfo,
            AvailableWarehouses = availableWarehouses,
            RelatedGoodsReceipts = relatedReceipts,
            RelatedVendorReturns = relatedReturns
        };
        model.CanModifyInfo = purchaseOrderInfo.CanModifyInfo;
        model.CanAllocateItems = purchaseOrderInfo.Status <= 30;
        if (model.CanModifyInfo)
        {
            model.ModifyInfo = new EditPurchaseOrderModel
            {
                Id = purchaseOrderInfo.Id,
                PlacedOn = purchaseOrderInfo.PlacedOn,
                VendorId = purchaseOrderInfo.VendorId,
                VendorName = purchaseOrderInfo.VendorName,
                VendorAddress = purchaseOrderInfo.VendorAddress,
                VendorPhone = purchaseOrderInfo.VendorPhone,
                WarehouseId = purchaseOrderInfo.WarehouseId,
                AvailableWarehouses = availableWarehouses,
                Note = purchaseOrderInfo.Note,
                ExpectedDeliveryDate = purchaseOrderInfo.ExpectedDeliveryDate,
                ShippingAmount = purchaseOrderInfo.ShippingAmount,
                TaxAmount = purchaseOrderInfo.TaxAmount,
                CanChangeVendor = purchaseOrderInfo.CanChangeVendor,
                CanChangeDate = purchaseOrderInfo.CanChangeDate,
                CanChangeFees = purchaseOrderInfo.CanChangeFees,
                TotalAmount = purchaseOrderInfo.TotalAmount,
                CreatedOn = purchaseOrderInfo.CreatedOn
            };
        }
        if (model.Info.CanAddItems)
        {
            model.AddItemModel = new AddPurchaseOrderItemModel
            {
                PurchaseOrderId = purchaseOrderInfo.Id
            };
        }
        else if (model.Info.CanReceiveGoods)
        {
            foreach (var item in model.Info.Items)
            {
                if (item.RemainingQuantity <= 0)
                    continue;

                model.ReceiveItemModels.Add(new ReceivePurchaseOrderItemModel
                {
                    PurchaseOrderItemId = item.Id,
                    PurchaseOrderId = purchaseOrderInfo.Id,
                    RemainingQuantity = item.RemainingQuantity,
                    WarehouseRequired = item.TrackInventory,
                    WarehouseId = purchaseOrderInfo.WarehouseId,
                    CurrentUnitPrice = item.CurrentUnitPrice,
                    UnitCost = item.UnitCost,
                    SellingPrice = item.CurrentUnitPrice
                });
            }
        }

        var poItemIds = purchaseOrderInfo.Items.Select(i => i.Id).ToList();
        if (poItemIds.Count > 0)
        {
            var allocations = await _purchaseOrderAppService
                .GetAllocationsForPurchaseOrderItemsAsync(poItemIds)
                .ConfigureAwait(false);

            foreach (var allocation in allocations)
            {
                if (!model.AllocationsPerItem.TryGetValue(allocation.PurchaseOrderItemId, out var list))
                {
                    list = [];
                    model.AllocationsPerItem[allocation.PurchaseOrderItemId] = list;
                }

                list.Add(new PurchaseOrderDetailsModel.AllocationForPoModel
                {
                    AllocationId = allocation.AllocationId,
                    OrderId = allocation.OrderId,
                    OrderItemId = allocation.OrderItemId,
                    OrderCode = allocation.OrderCode,
                    CustomerName = allocation.CustomerName,
                    CustomerPhone = allocation.CustomerPhone,
                    ShippingAddress = allocation.ShippingAddress,
                    AllocatedQuantity = allocation.AllocatedQuantity,
                    ReceivedQuantity = allocation.ReceivedQuantity,
                    Status = allocation.Status,
                    IsDirectShip = allocation.IsDirectShip
                });
            }

            var dsAllocations = await _directShipAppService
                .GetDirectShipAllocationsForPoItemsAsync(poItemIds)
                .ConfigureAwait(false);

            foreach (var alloc in dsAllocations)
            {
                if (!model.DirectShipAllocationsPerItem.TryGetValue(alloc.PurchaseOrderItemId, out var list))
                {
                    list = [];
                    model.DirectShipAllocationsPerItem[alloc.PurchaseOrderItemId] = list;
                }
                list.Add(new PurchaseOrderDetailsModel.DirectShipAllocationForPoModel
                {
                    AllocationId = alloc.AllocationId,
                    DirectShipAddress = alloc.DirectShipAddress,
                    DirectShipContactName = alloc.DirectShipContactName,
                    DirectShipContactPhone = alloc.DirectShipContactPhone,
                    AllocatedQuantity = alloc.AllocatedQuantity,
                    ReceivedQuantity = alloc.ReceivedQuantity,
                    Status = alloc.Status,
                    DeliveryStatus = alloc.DeliveryStatus,
                    DeliveryNoteId = alloc.DeliveryNoteId,
                    DeliveryNoteCode = alloc.DeliveryNoteCode
                });
            }
        }

        PrepareWorkflow(model, itemChangeAudits, vendorDebts);

        return model;
    }

    private static void PrepareWorkflow(
        PurchaseOrderDetailsModel model,
        IReadOnlyList<PurchaseOrderItemChangeAuditAppDto> itemChangeAudits,
        VendorDebtsByVendorAppDto? vendorDebts)
    {
        var status = (PurchaseOrderStatus)model.Info.Status;
        var activeStage = CalculateActiveStage(model, status);
        var receivingStatus = GetReceivingStatus(model);

        model.Ordering = BuildOrdering(model);
        model.Receiving = BuildReceiving(model);
        model.Returns = BuildReturns(model);
        model.Settlement = BuildSettlement(model, vendorDebts);
        model.Workflow = BuildWorkflow(model, status, receivingStatus, activeStage);
        model.Timeline = BuildTimeline(model, itemChangeAudits, vendorDebts);
    }

    private static PurchaseOrderDetailsModel.WorkflowStage CalculateActiveStage(
        PurchaseOrderDetailsModel model,
        PurchaseOrderStatus status)
    {
        if (status is PurchaseOrderStatus.Completed or PurchaseOrderStatus.Cancelled)
            return PurchaseOrderDetailsModel.WorkflowStage.Settlement;
        if (model.RelatedVendorReturns.Count > 0 || model.Info.Items.Any(item => item.QuantityReceived > 0) || model.RelatedGoodsReceipts.Count > 0)
            return PurchaseOrderDetailsModel.WorkflowStage.Receiving;

        return PurchaseOrderDetailsModel.WorkflowStage.Ordering;
    }

    private static PurchaseOrderDetailsModel.OrderingModel BuildOrdering(PurchaseOrderDetailsModel model)
    {
        var allocations = model.AllocationsPerItem.Values.SelectMany(item => item).ToList();

        return new PurchaseOrderDetailsModel.OrderingModel
        {
            ItemCount = model.Info.Items.Count,
            TotalQuantity = model.Info.Items.Sum(item => item.QuantityOrdered),
            Subtotal = model.Info.Items.Sum(item => item.TotalCost),
            AllocationCount = allocations.Count,
            AllocatedQuantity = allocations.Sum(allocation => allocation.AllocatedQuantity)
        };
    }

    private static PurchaseOrderDetailsModel.ReceivingModel BuildReceiving(PurchaseOrderDetailsModel model)
    {
        var receiptValue = model.RelatedGoodsReceipts
            .Where(receipt => receipt.TotalValue.HasValue)
            .Sum(receipt => receipt.TotalValue!.Value);

        return new PurchaseOrderDetailsModel.ReceivingModel
        {
            OrderedQuantity = model.Info.Items.Sum(item => item.QuantityOrdered),
            ReceivedQuantity = model.Info.Items.Sum(item => item.QuantityReceived),
            RemainingQuantity = model.Info.Items.Sum(item => item.RemainingQuantity),
            ReceiptCount = model.RelatedGoodsReceipts.Count,
            ReceiptTotalQuantity = model.RelatedGoodsReceipts.Sum(receipt => receipt.TotalQuantity),
            ReceiptTotalValue = receiptValue > 0 ? receiptValue : null,
            HasPendingCosting = model.RelatedGoodsReceipts.Any(receipt => receipt.IsPendingCosting)
        };
    }

    private static PurchaseOrderDetailsModel.ReturnModel BuildReturns(PurchaseOrderDetailsModel model)
        => new()
        {
            ReturnCount = model.RelatedVendorReturns.Count,
            RequestedQuantity = model.RelatedVendorReturns.SelectMany(item => item.Items).Sum(item => item.RequestedQuantity),
            AcceptedQuantity = model.RelatedVendorReturns.SelectMany(item => item.Items).Sum(item => item.AcceptedQuantity),
            TotalAmount = model.RelatedVendorReturns.Sum(item => item.TotalAmount),
            AdditionalCost = model.RelatedVendorReturns.Sum(item => item.AdditionalCost)
        };

    private static PurchaseOrderDetailsModel.SettlementModel BuildSettlement(
        PurchaseOrderDetailsModel model,
        VendorDebtsByVendorAppDto? vendorDebts)
    {
        var debts = GetVendorDebtsForPurchaseOrder(model, vendorDebts);
        var payments = debts.SelectMany(debt => debt.Payments).ToList();
        var purchaseTotal = model.Info.TotalAmount;
        var returnTotal = model.RelatedVendorReturns.Sum(item => item.TotalAmount);

        return new PurchaseOrderDetailsModel.SettlementModel
        {
            PurchaseTotal = purchaseTotal,
            ReturnTotal = returnTotal,
            NetPayable = purchaseTotal - returnTotal,
            DebtTotal = debts.Sum(debt => debt.TotalAmount),
            PaidTotal = debts.Sum(debt => debt.PaidAmount),
            RemainingDebt = debts.Sum(debt => debt.RemainingAmount),
            DebtCount = debts.Count,
            PaymentCount = payments.Count
        };
    }

    private static PurchaseOrderDetailsModel.WorkflowModel BuildWorkflow(
        PurchaseOrderDetailsModel model,
        PurchaseOrderStatus status,
        (string Text, string Class) receivingStatus,
        PurchaseOrderDetailsModel.WorkflowStage activeStage)
    {
        var receivingStepText = model.Returns.ReturnCount > 0
            ? $"{receivingStatus.Text} · {model.Returns.ReturnCount} trả"
            : receivingStatus.Text;

        var allocationCount = 0;
        foreach (var item in model.Info.Items)
        {
            var regular = model.AllocationsPerItem.TryGetValue(item.Id, out var a) ? a.Count : 0;
            var ds = model.DirectShipAllocationsPerItem.TryGetValue(item.Id, out var d) ? d.Count : 0;
            allocationCount += regular > 0 ? regular : ds;
        }
        var allocationText = allocationCount > 0 ? $"{allocationCount} phân bổ" : "Chưa phân bổ";

        return new()
        {
            ActiveStage = activeStage,
            StatusText = status.GetDisplayText(),
            StatusClass = status.GetDisplayColor(),
            ReceivingStatusText = receivingStatus.Text,
            ReceivingStatusClass = receivingStatus.Class,
            Steps =
            [
                BuildStep(PurchaseOrderDetailsModel.WorkflowStage.Ordering, "Đặt hàng", "bi-bag-plus", model.Info.Items.Count > 0 ? "Đã có hàng" : "Chưa có hàng", activeStage, model.Info.Items.Count > 0),
                BuildStep(PurchaseOrderDetailsModel.WorkflowStage.Receiving, "Nhận hàng", "bi-box-arrow-in-down", receivingStepText, activeStage, model.Receiving.ReceivedQuantity >= model.Receiving.OrderedQuantity && model.Receiving.OrderedQuantity > 0),
                BuildStep(PurchaseOrderDetailsModel.WorkflowStage.Returning, "Phân bổ", "bi-diagram-3", allocationText, activeStage, false),
                BuildStep(PurchaseOrderDetailsModel.WorkflowStage.Settlement, "Kết sổ", "bi-journal-check", GetSettlementStatusText(model, status), activeStage, status == PurchaseOrderStatus.Completed)
            ]
        };
    }

    private static PurchaseOrderDetailsModel.WorkflowStepModel BuildStep(
        PurchaseOrderDetailsModel.WorkflowStage stage,
        string title,
        string icon,
        string statusText,
        PurchaseOrderDetailsModel.WorkflowStage activeStage,
        bool isDone)
        => new()
        {
            Stage = stage,
            Title = title,
            Icon = icon,
            StatusText = statusText,
            IsActive = stage == activeStage,
            IsDone = isDone
        };

    private static (string Text, string Class) GetReceivingStatus(PurchaseOrderDetailsModel model)
    {
        var ordered = model.Info.Items.Sum(item => item.QuantityOrdered);
        var received = model.Info.Items.Sum(item => item.QuantityReceived);
        if (ordered <= 0)
            return ("Chưa nhận", "secondary");
        if (received >= ordered)
            return ("Đã nhận đủ", "success");
        if (received > 0)
            return ("Nhận một phần", "warning");

        return ("Chưa nhận", "secondary");
    }

    private static string GetSettlementStatusText(PurchaseOrderDetailsModel model, PurchaseOrderStatus status)
    {
        if (status == PurchaseOrderStatus.Completed)
            return "Đã hoàn tất";
        if (model.Settlement.RemainingDebt > 0)
            return "Còn nợ";
        if (model.Settlement.PaidTotal > 0)
            return "Đã thanh toán";

        return "Chưa kết sổ";
    }

    private static IList<PurchaseOrderDetailsModel.TimelineEventModel> BuildTimeline(
        PurchaseOrderDetailsModel model,
        IEnumerable<PurchaseOrderItemChangeAuditAppDto> itemChangeAudits,
        VendorDebtsByVendorAppDto? vendorDebts)
    {
        var timeline = new List<PurchaseOrderDetailsModel.TimelineEventModel>
        {
            new()
            {
                OccurredOn = model.Info.CreatedOn,
                Title = "Tạo đơn nhập",
                Description = $"{model.Info.Code} - {model.Info.VendorName}",
                Icon = "bi-file-earmark-plus",
                Tone = "primary",
                Stage = PurchaseOrderDetailsModel.WorkflowStage.Ordering
            }
        };

        foreach (var audit in itemChangeAudits)
        {
            timeline.Add(new PurchaseOrderDetailsModel.TimelineEventModel
            {
                OccurredOn = audit.CreatedOnUtc.ToLocalTime(),
                Title = GetPurchaseOrderItemChangeTitle(audit),
                Description = GetPurchaseOrderItemChangeDescription(audit),
                Icon = GetPurchaseOrderItemChangeIcon(audit),
                Tone = GetPurchaseOrderItemChangeTone(audit),
                Stage = PurchaseOrderDetailsModel.WorkflowStage.Ordering
            });
        }

        foreach (var receipt in model.RelatedGoodsReceipts)
        {
            timeline.Add(new PurchaseOrderDetailsModel.TimelineEventModel
            {
                OccurredOn = receipt.ReceivedOn,
                Title = "Nhận hàng",
                Description = $"{receipt.Code} - {receipt.TotalQuantity.DisplayQuantity()} hàng hóa",
                Icon = "bi-clipboard2-check",
                Tone = receipt.IsPendingCosting ? "warning" : "success",
                Stage = PurchaseOrderDetailsModel.WorkflowStage.Receiving
            });
        }

        foreach (var vendorReturn in model.RelatedVendorReturns)
        {
            timeline.Add(new PurchaseOrderDetailsModel.TimelineEventModel
            {
                OccurredOn = vendorReturn.ReturnDate,
                Title = "Trả hàng NCC",
                Description = $"{vendorReturn.Code} - {vendorReturn.TotalQuantity.DisplayQuantity()} hàng hóa",
                Icon = "bi-arrow-counterclockwise",
                Tone = vendorReturn.Status == (int)VendorReturnStatus.Confirmed ? "success" : "warning",
                Stage = PurchaseOrderDetailsModel.WorkflowStage.Returning
            });
        }

        var debts = GetVendorDebtsForPurchaseOrder(model, vendorDebts);
        foreach (var debt in debts)
        {
            timeline.Add(new PurchaseOrderDetailsModel.TimelineEventModel
            {
                OccurredOn = debt.CreatedOnUtc.ToLocalTime(),
                Title = "Ghi công nợ",
                Description = $"{debt.Code} - {debt.TotalAmount.DisplayCurrency()}",
                Icon = "bi-receipt",
                Tone = debt.RemainingAmount > 0 ? "warning" : "success",
                Stage = PurchaseOrderDetailsModel.WorkflowStage.Settlement
            });

            foreach (var payment in debt.Payments)
            {
                timeline.Add(new PurchaseOrderDetailsModel.TimelineEventModel
                {
                    OccurredOn = payment.PaidOnUtc.ToLocalTime(),
                    Title = "Thanh toán NCC",
                    Description = $"{payment.Code} - {payment.Amount.DisplayCurrency()}",
                    Icon = "bi-cash-coin",
                    Tone = "success",
                    Stage = PurchaseOrderDetailsModel.WorkflowStage.Settlement
                });
            }
        }

        return timeline
            .OrderByDescending(item => item.OccurredOn)
            .ThenBy(item => item.Title)
            .ToList();
    }

    private static IList<VendorDebtAppDto> GetVendorDebtsForPurchaseOrder(
        PurchaseOrderDetailsModel model,
        VendorDebtsByVendorAppDto? vendorDebts)
    {
        if (vendorDebts is null)
        {
            return [];
        }

        var receiptIds = model.RelatedGoodsReceipts.Select(receipt => receipt.Id).ToHashSet();
        return vendorDebts.Debts
            .Where(debt =>
                debt.PurchaseOrderId == model.Info.Id ||
                (debt.GoodsReceiptId.HasValue && receiptIds.Contains(debt.GoodsReceiptId.Value)))
            .GroupBy(debt => debt.Id)
            .Select(group => group.First())
            .ToList();
    }

    private static string GetPurchaseOrderItemChangeTitle(PurchaseOrderItemChangeAuditAppDto audit)
        => (PurchaseOrderItemChangeAuditAction)audit.Action switch
        {
            PurchaseOrderItemChangeAuditAction.Added => "Thêm hàng hóa",
            PurchaseOrderItemChangeAuditAction.Updated => "Sửa hàng hóa",
            PurchaseOrderItemChangeAuditAction.Removed => "Bớt hàng hóa",
            _ => "Cập nhật hàng hóa"
        };

    private static string GetPurchaseOrderItemChangeDescription(PurchaseOrderItemChangeAuditAppDto audit)
    {
        var productName = string.IsNullOrWhiteSpace(audit.ProductName) ? "Hàng hóa" : audit.ProductName;
        var description = (PurchaseOrderItemChangeAuditAction)audit.Action switch
        {
            PurchaseOrderItemChangeAuditAction.Added =>
                $"{productName} - thêm {FormatQuantity(audit.NewQuantity)}, đơn giá {FormatCurrency(audit.NewUnitCost)}",
            PurchaseOrderItemChangeAuditAction.Updated =>
                $"{productName} - {GetUpdatedPurchaseOrderItemChangeText(audit)}",
            PurchaseOrderItemChangeAuditAction.Removed =>
                $"{productName} - bớt {FormatQuantity(audit.OldQuantity)}, đơn giá {FormatCurrency(audit.OldUnitCost)}",
            _ => productName
        };

        var note = GetNoteChangeText(audit);
        return string.IsNullOrWhiteSpace(note) ? description : $"{description}. {note}";
    }

    private static string GetUpdatedPurchaseOrderItemChangeText(PurchaseOrderItemChangeAuditAppDto audit)
    {
        var changes = new List<string>();
        if (audit.OldQuantity != audit.NewQuantity)
            changes.Add($"SL {FormatQuantity(audit.OldQuantity)} -> {FormatQuantity(audit.NewQuantity)}");
        if (audit.OldUnitCost != audit.NewUnitCost)
            changes.Add($"Giá {FormatCurrency(audit.OldUnitCost)} -> {FormatCurrency(audit.NewUnitCost)}");

        return changes.Count == 0 ? "cập nhật ghi chú" : string.Join(", ", changes);
    }

    private static string? GetNoteChangeText(PurchaseOrderItemChangeAuditAppDto audit)
    {
        if ((PurchaseOrderItemChangeAuditAction)audit.Action == PurchaseOrderItemChangeAuditAction.Added
            && !string.IsNullOrWhiteSpace(audit.NewNote))
            return $"Ghi chú: {audit.NewNote}";
        if ((PurchaseOrderItemChangeAuditAction)audit.Action == PurchaseOrderItemChangeAuditAction.Removed
            && !string.IsNullOrWhiteSpace(audit.OldNote))
            return $"Ghi chú: {audit.OldNote}";
        if (!string.Equals(audit.OldNote, audit.NewNote, StringComparison.Ordinal))
            return $"Ghi chú: {FormatNote(audit.OldNote)} -> {FormatNote(audit.NewNote)}";

        return null;
    }

    private static string GetPurchaseOrderItemChangeIcon(PurchaseOrderItemChangeAuditAppDto audit)
        => (PurchaseOrderItemChangeAuditAction)audit.Action switch
        {
            PurchaseOrderItemChangeAuditAction.Added => "bi-plus-circle",
            PurchaseOrderItemChangeAuditAction.Updated => "bi-pencil-square",
            PurchaseOrderItemChangeAuditAction.Removed => "bi-dash-circle",
            _ => "bi-pencil"
        };

    private static string GetPurchaseOrderItemChangeTone(PurchaseOrderItemChangeAuditAppDto audit)
        => (PurchaseOrderItemChangeAuditAction)audit.Action switch
        {
            PurchaseOrderItemChangeAuditAction.Added => "success",
            PurchaseOrderItemChangeAuditAction.Updated => "warning",
            PurchaseOrderItemChangeAuditAction.Removed => "danger",
            _ => "secondary"
        };

    private static string FormatQuantity(decimal? value)
        => value.HasValue ? value.Value.DisplayQuantity() : "-";

    private static string FormatCurrency(decimal? value)
        => value.HasValue ? value.Value.DisplayCurrency() : "-";

    private static string FormatNote(string? value)
        => string.IsNullOrWhiteSpace(value) ? "trống" : value;
}
