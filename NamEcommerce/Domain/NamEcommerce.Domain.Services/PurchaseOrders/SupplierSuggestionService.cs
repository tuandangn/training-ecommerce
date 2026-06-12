using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Domain.Services.PurchaseOrders;

public sealed class SupplierSuggestionService(
    IEntityDataReader<Product> productReader,
    IEntityDataReader<Vendor> vendorReader,
    IEntityDataReader<PurchaseOrder> purchaseOrderReader) : ISupplierSuggestionService
{
    public async Task<IList<SupplierSuggestionDto>> SuggestVendorsForProductAsync(Guid productId)
    {
        var product = await productReader.GetByIdAsync(productId, default).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        var history = purchaseOrderReader.DataSource
            .Where(po => po.Status != PurchaseOrderStatus.Cancelled && po.Items.Any(item => item.ProductId == productId))
            .SelectMany(po => po.Items
                .Where(item => item.ProductId == productId)
                .Select(item => new
                {
                    po.VendorId,
                    po.PlacedOnUtc,
                    item.UnitCost
                }))
            .GroupBy(item => item.VendorId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(item => item.PlacedOnUtc).First());

        var preferredVendorIds = product.ProductVendors
            .OrderBy(vendor => vendor.DisplayOrder)
            .Select(vendor => vendor.VendorId)
            .ToList();

        var historyVendorIds = history
            .OrderByDescending(item => item.Value.PlacedOnUtc)
            .Select(item => item.Key)
            .Where(vendorId => !preferredVendorIds.Contains(vendorId))
            .ToList();

        var vendorIds = preferredVendorIds.Concat(historyVendorIds).Take(5).ToList();
        if (vendorIds.Count == 0)
            return [];

        var vendors = (await vendorReader.GetByIdsAsync(vendorIds).ConfigureAwait(false))
            .ToDictionary(vendor => vendor.Id);

        return vendorIds
            .Where(vendors.ContainsKey)
            .Select(vendorId =>
            {
                var productVendor = product.ProductVendors.FirstOrDefault(vendor => vendor.VendorId == vendorId);
                history.TryGetValue(vendorId, out var lastPurchase);

                return new SupplierSuggestionDto(vendorId, vendors[vendorId].Name)
                {
                    DisplayOrder = productVendor?.DisplayOrder,
                    LastPurchaseDateUtc = lastPurchase?.PlacedOnUtc,
                    LastUnitPrice = lastPurchase?.UnitCost,
                    IsPreferred = productVendor is not null
                };
            })
            .ToList();
    }
}

