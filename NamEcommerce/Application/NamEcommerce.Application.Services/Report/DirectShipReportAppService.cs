using NamEcommerce.Application.Contracts.Dtos.Report;
using NamEcommerce.Application.Contracts.Report;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Application.Services.Report;

public sealed class DirectShipReportAppService(
    IEntityDataReader<PurchaseOrderItemAllocation> allocationReader,
    IEntityDataReader<PurchaseOrder> poReader,
    IEntityDataReader<Vendor> vendorReader,
    IEntityDataReader<Product> productReader,
    IEntityDataReader<DeliveryNote> deliveryNoteReader) : IDirectShipReportAppService
{
    public async Task<DirectShipReportAppDto> GetReportAsync(DateTime? fromDate, DateTime? toDate)
    {
        var fromUtc = fromDate?.ToUniversalTime();
        var toUtc = toDate.HasValue
            ? toDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime()
            : (DateTime?)null;

        var allocQuery = allocationReader.DataSource.Where(a => a.IsDirectShip && a.Status != AllocationStatus.Cancelled);
        if (fromUtc.HasValue) allocQuery = allocQuery.Where(a => a.CreatedOnUtc >= fromUtc.Value);
        if (toUtc.HasValue) allocQuery = allocQuery.Where(a => a.CreatedOnUtc <= toUtc.Value);
        var allocations = allocQuery
            .Select(a => new { a.PurchaseOrderItemId, a.AllocatedQuantity })
            .ToList();

        var poItemIds = allocations.Select(a => a.PurchaseOrderItemId).Distinct().ToList();

        var posWithItems = poItemIds.Count == 0
            ? []
            : poReader.DataSource
                .Where(po => po.Items.Any(item => poItemIds.Any(itemId => itemId.SecondaryId == item.Id)))
                .Select(po => new
                {
                    po.Id,
                    po.VendorId,
                    Items = po.Items.Select(item => new { item.Id, item.ProductId }).ToList()
                })
                .ToList();

        var itemMap = posWithItems
            .SelectMany(po => po.Items.Select(item => new
            {
                item.Id,
                po.VendorId,
                item.ProductId
            }))
            .ToDictionary(x => x.Id);

        var vendorIds = itemMap.Values.Select(x => x.VendorId).Distinct().ToList();
        var vendorDict = vendorReader.DataSource
            .Where(v => vendorIds.Contains(v.Id))
            .Select(v => new { v.Id, v.Name })
            .ToList()
            .ToDictionary(v => v.Id, v => v.Name);

        var productIds = itemMap.Values.Select(x => x.ProductId).Distinct().ToList();
        var productDict = productReader.DataSource
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToList()
            .ToDictionary(p => p.Id, p => p.Name);

        var dnQuery = deliveryNoteReader.DataSource
            .Where(dn => dn.SourceType == DeliveryNoteSourceType.DirectShipToCustomer);
        if (fromUtc.HasValue) dnQuery = dnQuery.Where(dn => dn.CreatedOnUtc >= fromUtc.Value);
        if (toUtc.HasValue) dnQuery = dnQuery.Where(dn => dn.CreatedOnUtc <= toUtc.Value);
        var dns = dnQuery
            .Select(dn => new
            {
                dn.Code,
                dn.CustomerInfo.FullName,
                dn.TotalAmount,
                dn.CreatedOnUtc,
                dn.Status,
                dn.ConfirmedNote
            })
            .ToList();

        var byVendor = allocations
            .GroupBy(a => itemMap.TryGetValue(a.PurchaseOrderItemId.SecondaryId, out var info) ? info.VendorId : Guid.Empty)
            .Where(g => g.Key != Guid.Empty)
            .Select(g => new DirectShipByVendorAppDto
            {
                VendorName = vendorDict.TryGetValue(g.Key, out var name) ? name : "(Không xác định)",
                AllocationCount = g.Count(),
                TotalQuantity = g.Sum(a => a.AllocatedQuantity)
            })
            .OrderByDescending(x => x.AllocationCount)
            .ToList();

        var byCustomer = dns
            .GroupBy(dn => dn.FullName)
            .Select(g => new DirectShipByCustomerAppDto
            {
                CustomerName = g.Key,
                DeliveryCount = g.Count(),
                TotalAmount = g.Sum(dn => dn.TotalAmount)
            })
            .OrderByDescending(x => x.TotalAmount)
            .Take(10)
            .ToList();

        var byProduct = allocations
            .GroupBy(a => itemMap.TryGetValue(a.PurchaseOrderItemId.SecondaryId, out var info) ? info.ProductId : Guid.Empty)
            .Where(g => g.Key != Guid.Empty)
            .Select(g => new DirectShipByProductAppDto
            {
                ProductName = productDict.TryGetValue(g.Key, out var name) ? name : "(Không xác định)",
                AllocationCount = g.Count(),
                TotalQuantity = g.Sum(a => a.AllocatedQuantity)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .ToList();

        var cutoff = DateTime.UtcNow.AddDays(-7);
        var pendingAlerts = deliveryNoteReader.DataSource
            .Where(dn => dn.SourceType == DeliveryNoteSourceType.DirectShipToCustomer
                      && dn.Status == DeliveryNoteStatus.Confirmed
                      && dn.CreatedOnUtc < cutoff)
            .Select(dn => new { dn.Code, dn.CustomerInfo.FullName, dn.TotalAmount, dn.CreatedOnUtc })
            .ToList()
            .Select(dn => new DirectShipPendingAlertAppDto
            {
                DeliveryNoteCode = dn.Code,
                CustomerName = dn.FullName,
                TotalAmount = dn.TotalAmount,
                DaysPending = (int)(DateTime.UtcNow - dn.CreatedOnUtc).TotalDays
            })
            .OrderByDescending(x => x.DaysPending)
            .ToList();

        var rejectRate = new DirectShipRejectRateAppDto
        {
            TotalDeliveries = dns.Count,
            TotalConfirmed = dns.Count(dn => dn.Status == DeliveryNoteStatus.Delivered),
            TotalRejected = dns.Count(dn => dn.Status == DeliveryNoteStatus.Cancelled),
            TotalPending = dns.Count(dn => dn.Status == DeliveryNoteStatus.Confirmed),
            RejectReasons = dns
                .Where(dn => dn.Status == DeliveryNoteStatus.Cancelled
                          && !string.IsNullOrEmpty(dn.ConfirmedNote))
                .GroupBy(dn => dn.ConfirmedNote!)
                .Select(g => new DirectShipRejectReasonAppDto { Reason = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count)
                .ToList()
        };

        return new DirectShipReportAppDto
        {
            ByVendor = byVendor,
            ByCustomer = byCustomer,
            ByProduct = byProduct,
            PendingAlerts = pendingAlerts,
            RejectRate = rejectRate
        };
    }
}
