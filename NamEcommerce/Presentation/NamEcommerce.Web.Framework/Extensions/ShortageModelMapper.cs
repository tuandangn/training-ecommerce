using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Extensions;

public static class ShortageModelMapper
{
    public static ShortageAggregationModel ToModel(this ShortageAggregationAppDto dto)
    {
        return new ShortageAggregationModel
        {
            OrderId = dto.Filter.OrderId,
            DeliveryNoteId = dto.Filter.DeliveryNoteId,
            ProductId = dto.Filter.ProductId,
            IncludeSalesOrderScope = dto.Filter.IncludeSalesOrderScope,
            VendorGroups = dto.VendorGroups.Select(ToModel).ToList()
        };
    }

    public static ShortageInfoModel ToShortageInfoModel(this ShortageAggregationAppDto dto)
    {
        return new ShortageInfoModel
        {
            Items = dto.VendorGroups
                .SelectMany(group => group.Items
                    .Where(item => item.IsFromPrimaryOrder)
                    .Select(item => ToShortageInfoItemModel(item, group)))
                .ToList()
        };
    }

    private static VendorShortageGroupModel ToModel(VendorShortageGroupAppDto dto)
    {
        return new VendorShortageGroupModel
        {
            VendorId = dto.VendorId,
            VendorName = dto.VendorName,
            IsNoVendorGroup = dto.IsNoVendorGroup,
            Items = dto.Items.Select(ToModel).ToList()
        };
    }

    private static ShortageAggregationItemModel ToModel(ShortageAggregationItemAppDto dto)
    {
        return new ShortageAggregationItemModel
        {
            OrderCode = dto.OrderCode,
            OrderId = dto.OrderId,
            OrderItemId = dto.OrderItemId,
            ProductId = dto.ProductId,
            ProductName = dto.ProductName,
            UnitMeasurementName = dto.UnitMeasurementName,
            QuantityDecimalPlaces = dto.QuantityDecimalPlaces,
            RequiredQuantity = dto.RequiredQuantity,
            ShippedQuantity = dto.ShippedQuantity,
            AvailableQuantity = dto.AvailableQuantity,
            ShortageQuantity = dto.ShortageQuantity,
            QuantityToOrder = dto.QuantityToOrder,
            UnitCost = dto.UnitCost,
            IsFromPrimaryOrder = dto.IsFromPrimaryOrder,
            CustomerName = dto.CustomerName,
            CustomerPhone = dto.CustomerPhone,
            CustomerAddress = dto.CustomerAddress,
            SupplierSuggestions = dto.SupplierSuggestions.Select(ToModel).ToList(),
            AllocatedFromPurchaseOrders = dto.AllocatedFromPurchaseOrders.Select(ToModel).ToList()
        };
    }

    private static ShortageInfoItemModel ToShortageInfoItemModel(ShortageAggregationItemAppDto dto, VendorShortageGroupAppDto group)
    {
        return new ShortageInfoItemModel
        {
            OrderCode = dto.OrderCode,
            OrderItemId = dto.OrderItemId,
            ProductId = dto.ProductId,
            ProductName = dto.ProductName,
            UnitMeasurementName = dto.UnitMeasurementName,
            RequiredQuantity = dto.RequiredQuantity,
            ShippedQuantity = dto.ShippedQuantity,
            AvailableQuantity = dto.AvailableQuantity,
            ShortageQuantity = dto.ShortageQuantity,
            UnitCost = dto.UnitCost,
            VendorId = group.VendorId,
            VendorName = group.IsNoVendorGroup ? null : group.VendorName,
            SupplierSuggestions = dto.SupplierSuggestions.Select(ToModel).ToList(),
            AllocatedFromPurchaseOrders = dto.AllocatedFromPurchaseOrders.Select(ToModel).ToList()
        };
    }

    private static SupplierSuggestionModel ToModel(SupplierSuggestionAppDto dto)
    {
        return new SupplierSuggestionModel
        {
            VendorId = dto.VendorId,
            VendorName = dto.VendorName,
            DisplayOrder = dto.DisplayOrder,
            LastPurchaseDate = dto.LastPurchaseDateUtc?.ToLocalTime(),
            LastUnitPrice = dto.LastUnitPrice,
            IsPreferred = dto.IsPreferred
        };
    }

    private static PurchaseOrderShortageAllocationModel ToModel(PurchaseOrderShortageAllocationAppDto dto)
    {
        return new PurchaseOrderShortageAllocationModel
        {
            PurchaseOrderId = dto.PurchaseOrderId,
            PurchaseOrderCode = dto.PurchaseOrderCode,
            AllocatedQuantity = dto.AllocatedQuantity,
            ReceivedQuantity = dto.ReceivedQuantity,
            ExpectedReceiveDate = dto.ExpectedReceiveDateUtc?.ToLocalTime()
        };
    }
}
