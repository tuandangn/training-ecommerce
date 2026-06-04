# Initial Stock Unit Cost

## Vấn đề

Khi tạo sản phẩm mới với tồn kho ban đầu (tồn kho đầu kỳ), hệ thống chỉ ghi số lượng mà không ghi giá vốn. Điều này dẫn đến:

- `InventoryCostLayer` tạo ra với `Status = Pending`, `UnitCost = null`
- Mọi đơn bán hàng từ tồn kho đầu kỳ có `COGS = 0` — **sai về kế toán**
- Phải revaluation sau khi có phiếu nhập hàng → phức tạp, dễ sai
- Không truy vết được giá vốn đầu kỳ

## Quyết định kiến trúc

**Dùng `GoodsReceipt` với `SourceType = OpeningBalance` mới.**

Lý do không dùng `StockAdjustmentNote`:
- StockAdjustmentNote dùng cho kiểm kê định kỳ (physical count variance), không phải nhập hàng có cost
- Event handler của StockAdjustmentNote gọi `RegisterInboundAsync` với `UnitCost = null` — không hỗ trợ cost
- Khó phân biệt khi báo cáo (lẫn với kiểm kê thông thường)

Lý do dùng GoodsReceipt:
- GoodsReceipt là entity đúng nghĩa "nhập hàng" — full chain đã có sẵn
- `InventoryCostLayer` tạo ra với `Status = Final` ngay lập tức (không cần revaluation)
- Dễ theo dõi: filter `SourceType = OpeningBalance`
- Handler `GoodsReceiptCreatedHandler` + `GoodsReceiptItemUnitCostSetHandler` đã xử lý đủ chain

## Ràng buộc đặc biệt của OpeningBalance

`GoodsReceipt.SourceType = OpeningBalance` KHÁC với phiếu nhập hàng thường:

| Thao tác | Phiếu nhập thường | OpeningBalance |
|---|---|---|
| Tạo từ PO | ✅ | ❌ Không áp dụng |
| Hủy phiếu | ✅ | ❌ **Không cho phép** |
| Xóa phiếu | ✅ | ❌ **Không cho phép** |
| Trả hàng nhà cung cấp | ✅ | ❌ Không áp dụng |
| Sửa UnitCost sau khi tạo | ✅ | ❌ Xem xét thêm |
| VendorDebt | ✅ | ❌ Bỏ qua |
| Hiển thị trong danh sách GR | ✅ | ✅ Nhưng badge riêng |

**Tại sao không thể hoàn tác:**
- `InventoryCostLayer.Status = Final` → đã ảnh hưởng AverageCost toàn bộ sản phẩm
- Có thể đã có đơn bán dùng cost này để tính COGS
- Đảo ngược sẽ phá vỡ toàn bộ chain kế toán

**Xử lý trong code:**
- Domain: `GoodsReceipt.CanCancel()` / `CanDelete()` → trả về `false` nếu `SourceType == OpeningBalance`
- AppService: guard clause khi nhận lệnh cancel/delete
- UI: ẩn nút Hủy, nút Xóa cho OpeningBalance receipts

## Business Rule: Confirmation bắt buộc

Trước khi submit form tạo sản phẩm có tồn kho đầu kỳ, hiển thị modal xác nhận:

> **Xác nhận tồn kho đầu kỳ — Không thể hoàn tác**
>
> Bạn đang ghi nhận tồn kho đầu kỳ với thông tin sau:
>
> | Kho | Số lượng | Giá vốn đơn vị | Giá trị |
> |-----|----------|----------------|---------|
> | Kho chính | 100 | 50.000 đ | 5.000.000 đ |
>
> Sau khi xác nhận:
> - Giá vốn trung bình của sản phẩm sẽ được cập nhật ngay lập tức
> - Mọi đơn bán hàng sau đó sẽ dùng giá vốn này
> - **Không thể hủy hoặc xóa phiếu nhập đầu kỳ**
>
> [Quay lại] [Tôi hiểu, xác nhận]

## Validation Rules

- Nếu `Quantity > 0` thì `UnitCost > 0` là **bắt buộc** (client + server)
- Nếu `Quantity = 0` thì bỏ qua (không tạo GoodsReceipt)
- `UnitCost` áp dụng chung cho tất cả kho (tương lai: per warehouse)

## Kế hoạch thực hiện

### Phase 1 — Domain

**1a. Enum `GoodsReceiptSourceType`:**
```csharp
public enum GoodsReceiptSourceType
{
    FromVendor = 0,
    FromCustomerReturn = 1,
    OpeningBalance = 2      // ← mới
}
```

**1b. `GoodsReceipt` entity — guard methods:**
```csharp
public bool CanCancel() => SourceType != GoodsReceiptSourceType.OpeningBalance;
public bool CanDelete() => SourceType != GoodsReceiptSourceType.OpeningBalance;
```

**1c. `GoodsReceiptManager` — method mới:**
```csharp
public async Task<CreateGoodsReceiptResultDto> CreateForOpeningInventoryAsync(
    CreateOpeningInventoryReceiptDto dto)
// - SourceType = OpeningBalance
// - Không cần vendor
// - UnitCost required > 0 (throw nếu không có)
// - MarkCreated() + MarkItemUnitCostSet() ngay
```

**1d. DTO mới:**
```csharp
public sealed record CreateOpeningInventoryReceiptDto
{
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }   // > 0
    public required decimal UnitCost { get; init; }   // > 0, enforced
    public Guid? CreatedByUserId { get; init; }
    public string? ProductName { get; init; }
    public string? WarehouseName { get; init; }
}
```

### Phase 2 — Application: Event handlers

**2a. `GoodsReceiptCreatedHandler`** — thêm guard:
```csharp
// Bỏ qua VendorDebt nếu OpeningBalance
var isOpeningBalance = goodsReceipt.SourceType == GoodsReceiptSourceType.OpeningBalance;
if (!isOpeningBalance && !isPendingCosting && goodsReceipt.VendorId.HasValue)
{
    await vendorDebtManager.CreateDebtFromGoodsReceiptAsync(...);
}
```

**2b. `GoodsReceiptItemUnitCostSetHandler`** — không thay đổi.

**2c. Cancel/Delete AppService** — thêm guard:
```csharp
if (goodsReceipt.SourceType == GoodsReceiptSourceType.OpeningBalance)
    return CommonActionResultDto.CreateError("Error.GoodsReceipt.OpeningBalanceCannotBeModified");
```

### Phase 3 — Presentation

**3a. Models — thêm UnitCost:**
```csharp
// CreateProductModel.ProductStockModel
public sealed class ProductStockModel
{
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }    // > 0 nếu Quantity > 0
}

// CreateProductCommand.ProductStockModel
public sealed record ProductStockModel(Guid WarehouseId, decimal Quantity, decimal UnitCost);
```

**3b. `CreateProductHandler`** — thay StockAdjustmentNote bằng GoodsReceipt:
```csharp
foreach (var stock in command.ProductStocks.Where(s => s.Quantity > 0))
{
    // Server-side validation
    if (stock.UnitCost <= 0)
        return CreateProductResultModel.Error("Error.OpeningInventory.UnitCostRequired");

    await goodsReceiptManager.CreateForOpeningInventoryAsync(new CreateOpeningInventoryReceiptDto
    {
        ProductId = createdProduct.Id,
        WarehouseId = stock.WarehouseId,
        Quantity = stock.Quantity,
        UnitCost = stock.UnitCost,
        ...
    });
}
```

**3c. `Views/Product/Create.cshtml`** — tab "Tồn kho":
- Thêm cột "Giá vốn đơn vị (đ)" bên cạnh "Số lượng ban đầu"
- Input required khi Quantity > 0 (JS enforce)
- Nút submit form → JS intercept nếu có tồn kho đầu kỳ → hiển thị modal xác nhận trước
- Modal xác nhận: bảng tổng hợp + cảnh báo không thể hoàn tác

**3d. GoodsReceipt List/Details UI:**
- Badge "Đầu kỳ" thay vì "Nhập hàng" cho SourceType=OpeningBalance
- Ẩn nút Hủy, Xóa, Trả hàng
- Hiển thị ghi chú: "Phiếu tồn kho đầu kỳ — không thể hoàn tác"

### Phase 4 — Migration

Không cần thêm cột. `GoodsReceiptSourceType` lưu dưới dạng `int` — chỉ thêm enum value `OpeningBalance = 2`.

## Kết quả sau khi implement

Tạo sản phẩm với Qty=100, UnitCost=50.000đ, Kho chính:

| Entity | Giá trị |
|---|---|
| `GoodsReceipt` | SourceType=**OpeningBalance**, không có vendor |
| `GoodsReceiptItem` | Qty=100, UnitCost=50.000, Status=Final |
| `StockMovementLog` | MovementType=Inbound, ReferenceType=GoodsReceipt |
| `InventoryStock` | QuantityOnHand=100 |
| `InventoryCostLedgerEntry` | MovementType=GoodsReceipt, UnitCost=50.000, **Status=Final** |
| `InventoryCostLayer` | Qty=100, UnitCost=50.000, **Status=Final** |
| `AverageCostAfter` | **50.000 ngay lập tức** |
| COGS đơn hàng sau | qty_sold × 50.000 (chính xác, không cần revaluation) |

## Files thay đổi

### Tạo mới / Thêm vào
- `Domain.Shared/Enums/GoodsReceipts/GoodsReceiptSourceType.cs` — thêm `OpeningBalance = 2`
- `Domain.Shared/Dtos/GoodsReceipts/CreateOpeningInventoryReceiptDto.cs` — DTO mới

### Sửa
- `Domain/Entities/GoodsReceipts/GoodsReceipt.cs` — thêm `CanCancel()`, `CanDelete()`
- `Domain.Services/GoodsReceipts/GoodsReceiptManager.cs` — thêm `CreateForOpeningInventoryAsync`
- `Application.Services/Events/GoodsReceipts/GoodsReceiptCreatedHandler.cs` — guard OpeningBalance cho VendorDebt
- `Application.Services/GoodsReceipts/GoodsReceiptAppService.cs` — guard cancel/delete
- `Web.Framework/Commands/Handlers/Catalog/CreateProductHandler.cs` — dùng GoodsReceiptManager thay StockAdjustmentNote
- `Web/Models/Catalog/CreateProductModel.cs` — thêm UnitCost
- `Web.Contracts/Commands/Models/Catalog/CreateProductCommand.cs` — thêm UnitCost
- `Web/Views/Product/Create.cshtml` — thêm cột UnitCost + modal xác nhận
- `Web/Views/GoodsReceipt/` (List, Details) — badge + ẩn actions cho OpeningBalance
