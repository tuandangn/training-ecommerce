using NamEcommerce.Application.Contracts.Dtos.Report;
using NamEcommerce.Application.Contracts.Report;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

namespace NamEcommerce.Application.Services.Report;

public sealed class DirectShipReportAppService : IDirectShipReportAppService
{
    private readonly IEntityDataReader<PurchaseOrderItemAllocation> _allocationReader;
    private readonly IEntityDataReader<PurchaseOrder> _poReader;
    private readonly IEntityDataReader<Vendor> _vendorReader;
    private readonly IEntityDataReader<Product> _productReader;
    private readonly IEntityDataReader<DeliveryNote> _deliveryNoteReader;

    public DirectShipReportAppService(
        IEntityDataReader<PurchaseOrderItemAllocation> allocationReader,
        IEntityDataReader<PurchaseOrder> poReader,
        IEntityDataReader<Vendor> vendorReader,
        IEntityDataReader<Product> productReader,
        IEntityDataReader<DeliveryNote> deliveryNoteReader)
    {
        _allocationReader = allocationReader;
        _poReader = poReader;
        _vendorReader = vendorReader;
        _productReader = productReader;
        _deliveryNoteReader = deliveryNoteReader;
    }

    public Task<DirectShipReportAppDto> GetReportAsync(DateTime? fromDate, DateTime? toDate)
    {
        var fromUtc = fromDate?.ToUniversalTime();
        var toUtc = toDate.HasValue
            ? toDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime()
            : (DateTime?)null;

        // ── 1. Load direct-ship allocations ─────────────────────────────────────
        var allocQuery = _allocationReader.DataSource.Where(a => a.IsDirectShip);
        if (fromUtc.HasValue) allocQuery = allocQuery.Where(a => a.CreatedOnUtc >= fromUtc.Value);
        if (toUtc.HasValue) allocQuery = allocQuery.Where(a => a.CreatedOnUtc <= toUtc.Value);
        var allocations = allocQuery
            .Select(a => new { a.PurchaseOrderItemId, a.AllocatedQuantity })
            .ToList();

        // ── 2. Load POs with items to resolve vendor + product per allocation ────
        var poItemIds = allocations.Select(a => a.PurchaseOrderItemId).Distinct().ToList();

        var posWithItems = poItemIds.Count == 0
            ? []
            : _poReader.DataSource
                .Where(po => po.Items.Any(i => poItemIds.Contains(i.Id)))
                .Select(po => new
                {
                    po.Id,
                    po.VendorId,
                    Items = po.Items.Select(i => new { i.Id, i.ProductId }).ToList()
                })
                .ToList();

        // Build itemId → (VendorId, ProductId) map
        var itemMap = posWithItems
            .SelectMany(po => po.Items.Select(item => new
            {
                item.Id,
                po.VendorId,
                item.ProductId
            }))
            .ToDictionary(x => x.Id);

        // ── 3. Load vendors and products by IDs ─────────────────────────────────
        var vendorIds = itemMap.Values.Select(x => x.VendorId).Distinct().ToList();
        var vendorDict = _vendorReader.DataSource
            .Where(v => vendorIds.Contains(v.Id))
            .Select(v => new { v.Id, v.Name })
            .ToList()
            .ToDictionary(v => v.Id, v => v.Name);

        var productIds = itemMap.Values.Select(x => x.ProductId).Distinct().ToList();
        var productDict = _productReader.DataSource
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToList()
            .ToDictionary(p => p.Id, p => p.Name);

        // ── 4. Load direct-ship delivery notes (with date filter) ────────────────
        var dnQuery = _deliveryNoteReader.DataSource
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

        // ── D9.1 — By vendor ────────────────────────────────────────────────────
        var byVendor = allocations
            .GroupBy(a => itemMap.TryGetValue(a.PurchaseOrderItemId, out var info) ? info.VendorId : Guid.Empty)
            .Where(g => g.Key != Guid.Empty)
            .Select(g => new DirectShipByVendorAppDto
            {
                VendorName = vendorDict.TryGetValue(g.Key, out var name) ? name : "(Không xác định)",
                AllocationCount = g.Count(),
                TotalQuantity = g.Sum(a => a.AllocatedQuantity)
            })
            .OrderByDescending(x => x.AllocationCount)
            .ToList();

        // ── D9.2 — Top customers ────────────────────────────────────────────────
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

        // ── D9.3 — By product ───────────────────────────────────────────────────
        var byProduct = allocations
            .GroupBy(a => itemMap.TryGetValue(a.PurchaseOrderItemId, out var info) ? info.ProductId : Guid.Empty)
            .Where(g => g.Key != Guid.Empty)
            .Select(g => new DirectShipByProductAppDto
            {
                ProductName = productDict.TryGetValue(g.Key, out var name) ? name : "(Không xác định)",
                AllocationCount = g.Count(),
                TotalQuantity = g.Sum(a => a.AllocatedQuantity)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .ToList();

        // ── D9.4 — Pending > 7 days (always current snapshot, no date filter) ───
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var pendingAlerts = _deliveryNoteReader.DataSource
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

        // ── D9.5 — Reject rate ──────────────────────────────────────────────────
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

        return Task.FromResult(new DirectShipReportAppDto
        {
            ByVendor = byVendor,
            ByCustomer = byCustomer,
            ByProduct = byProduct,
            PendingAlerts = pendingAlerts,
            RejectRate = rejectRate
        });
    }
}
