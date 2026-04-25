using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Settings;

namespace NamEcommerce.Domain.Entities.GoodsReceipts;

[Serializable]
public sealed record GoodsReceiptItem : AppEntity
{
    private GoodsReceiptItem(Guid productId, Guid? warehouseId, decimal quantity, decimal? unitCost) : base(Guid.Empty)
    {
        (ProductId, WarehouseId, Quantity) = (productId, warehouseId, quantity);

        if (unitCost.HasValue)
            SetUnitCost(unitCost.Value);
    }
    private readonly Guid goodsReceiptId;
    public Guid GoodsReceiptId => goodsReceiptId;

    public Guid ProductId { get; private set; }
    internal string ProductName { get; private set; } = "";

    public Guid? WarehouseId { get; private set; }
    internal string? WarehouseName { get; private set; }

    public decimal Quantity
    {
        get; internal set
        {
            if (value <= 0)
                throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.Item.QuantityLessThanOrEqualToZero");

            field = value;
        }
    }
    public decimal? UnitCost { get; private set; }

    #region Methods

    public bool IsPendingCosting() => !UnitCost.HasValue;

    internal void SetUnitCost(decimal unitCost)
    {
        if (unitCost < 0)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.Item.UnitCostLessThanZero");

        UnitCost = unitCost;
    }

    internal static async Task<GoodsReceiptItem> CreateAsync(Guid productId, Guid? warehouseId, decimal quantity, decimal? unitCost,
        IGetByIdService<Product> productByIdGetter, WarehouseSettings warehouseSettings, IGetByIdService<Warehouse>? warehouseByIdGetter)
    {
        ArgumentNullException.ThrowIfNull(productByIdGetter);
        ArgumentNullException.ThrowIfNull(warehouseSettings);
        if (!warehouseSettings.AllowNonWarehouse || warehouseId.HasValue)
            ArgumentNullException.ThrowIfNull(warehouseByIdGetter);

        var product = await productByIdGetter.GetByIdAsync(productId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        if (!warehouseSettings.AllowNonWarehouse && !warehouseId.HasValue)
            throw new WarehouseIsRequiredException();

        Warehouse? warehouse = null;
        if (warehouseId.HasValue)
        {
            warehouse = await warehouseByIdGetter!.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
                throw new WarehouseIsNotFoundException(warehouseId.Value);
        }

        return new GoodsReceiptItem(productId, warehouseId, quantity, unitCost)
        {
            ProductName = product.Name,
            WarehouseName = warehouse?.Name
        };
    }

    #endregion
}
