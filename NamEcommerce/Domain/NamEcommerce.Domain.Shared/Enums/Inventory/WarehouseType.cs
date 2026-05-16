namespace NamEcommerce.Domain.Shared.Enums.Inventory;

public enum WarehouseType
{
    /// <summary>Kho thực (kho chính / kho phụ). Tên alias là Physical.</summary>
    Main = 0,
    Physical = 0,
    SubWarehouse = 1,
    ReturnWarehouse = 2,
    /// <summary>Kho ảo — hàng đang trên đường giao thẳng tới khách. Không hiện trong dropdown chọn kho thường.</summary>
    DirectShipTransit = 3
}
