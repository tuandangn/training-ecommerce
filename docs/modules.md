# Modules — Listing and Relationships

## Existing Modules

### Catalog
- **Category** — danh mục sản phẩm, có cây thư mục (ParentId)
- **Product** — sản phẩm, liên kết Category + UnitMeasurement + Vendor
- **UnitMeasurement** — đơn vị tính (cái, kg, m²...)
- **Vendor** — nhà cung cấp
- **ProductPriceHistory** — lịch sử giá

### Orders
- **Order** — đơn bán hàng (aggregate root)
- **OrderItem** — dòng sản phẩm trong đơn (child entity, dùng `AppEntity`)

### Inventory
- **InventoryStock** — tồn kho tại warehouse
- **Warehouse** — kho hàng
- **StockMovementLog** — log xuất/nhập kho
- **ProductReservationLedger** — đặt chỗ trên toàn hệ thống (aggregate root)
- **InventoryCostLedgerEntry** — log xuất/nhập kho (aggregate root)

### PurchaseOrders
- **PurchaseOrder** — đơn nhập hàng
- **PurchaseOrderItem** — dòng sản phẩm trong đơn nhập
- **PurchaseOrderItemAllocation** — phần bổ sản phẩm cho đơn đặt hàng

### Returns
- **CustomerReturn** — khách trả hàng
- **VendorReturn** — trả hàng cho nhà cung cấp

### Customers
- **Customer** — khách hàng

### CustomesPortal

### Debts
- **CustomerDebt** — công nợ khách hàng
- **CustomerPayment** — khách hàng thanh toán công nợ
- **VendorDebt** — công nợ nhà cung cấp 
- **VendorPayment** — thanh toán công nợ cho nhà cung cấp

### DeliveryNotes
- **DeliveryNote** — phiếu giao hàng
- **DeliveryNoteItem** — dòng sản phẩm trong phiếu

### GoodsReceipt
- **GoodsReceipt** — phiếu nhập hàng
- **GoodsReceiptItem** — dòng sản phẩm trong phiếu

### Finance
- **Expense** — thu chi
- **InventoryCostingPolicy** - chính sách tính giá vốn
- **InventoryCostAllocation**
- **InventoryCostRebuildRun**

### Media
- **Picture** — ảnh (liên kết với Product)

### Users
- **User** — người dùng
- **Role** — vai trò
- **UserRole** — nhiều-nhiều User ↔ Role

### Security
- **Permission** — quyền hạn
- **RolePermission** — nhiều-nhiều Role ↔ Permission

---

## Key Relationships

```
Product ──────────── Category (N:1)
Product ──────────── UnitMeasurement (N:1)
Product ──────────── Vendor (N:1)
Product ──────────── Picture (1:N)

Order ─────────────── OrderItem (1:N)
Order ─────────────── Customer (N:1)
OrderItem ─────────── Product (N:1)

PurchaseOrder ─────── PurchaseOrderItem (1:N)
PurchaseOrderItem ─── Product (N:1)

InventoryStock ────── Product (N:1)
InventoryStock ────── Warehouse (N:1)

DeliveryNote ──────── DeliveryNoteItem (1:N)
DeliveryNote ──────── Order (N:1)

CustomerDebt ──────── Customer (N:1)
CustomerPayment ───── Customer (N:1)

User ←──────────────── UserRole ──────────────→ Role
Role ←──────────────── RolePermission ─────────→ Permission
```

---

## Namespace pattern

```
NamEcommerce.Domain.Entities.Catalog
NamEcommerce.Domain.Entities.Orders
NamEcommerce.Domain.Shared.Dtos.Catalog
NamEcommerce.Domain.Shared.Services.Catalog
NamEcommerce.Domain.Services.Catalog
NamEcommerce.Application.Contracts.Dtos.Catalog
NamEcommerce.Application.Contracts.Catalog
NamEcommerce.Application.Services.Catalog
NamEcommerce.Web.Controllers (không theo module)
NamEcommerce.Web.Services.Catalog
NamEcommerce.Web.Models.Catalog
NamEcommerce.Web.Contracts.Commands.Models.Catalog
NamEcommerce.Web.Contracts.Queries.Models.Catalog
NamEcommerce.Web.Contracts.Models.Catalog
NamEcommerce.Web.Framework.Commands.Handlers.Catalog
NamEcommerce.Web.Framework.Queries.Handlers.Catalog
```
