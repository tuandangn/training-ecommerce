# Kế hoạch: Quantity Precision — Hỗ trợ số nguyên và số thập phân theo đơn vị đo

## Vấn đề

Hiện tại toàn bộ ô nhập số lượng (`quantity-input`) đều cho phép 2 chữ số thập phân.  
Nhưng nhiều sản phẩm không thể có số lượng thập phân — ví dụ "1 bộ bàn cầu", "2 cái gương".  
Ngược lại "0,5 m³ cát" hay "0,7 m² gạch men" cần thập phân.

Độ chính xác của số lượng gắn với **đơn vị đo (UnitMeasurement)**, không phải từng sản phẩm riêng lẻ:
- `cái`, `bộ`, `cặp`, `thùng` → integer (0 decimal)
- `m`, `m²`, `m³`, `kg`, `lít` → 2 decimal

---

## Quyết định thiết kế

**Thêm `DecimalPlaces` (int, 0 hoặc 2) vào `UnitMeasurement`.**

- Giá trị cho phép: `0` = chỉ nguyên, `2` = cho phép 2 chữ số thập phân
- Sản phẩm không có UnitMeasurement → fallback = `0` (integer)
- Không thêm trường vào `Product` trực tiếp — đơn vị đo đã là nơi semantics đúng nhất

---

## Assumptions

1. Phạm vi chỉ là **0 hoặc 2** decimal places — không cần 1, 3, 4.  
   Nếu cần mở rộng sau này, tên trường `DecimalPlaces` (int) dễ mở rộng hơn `AllowDecimal` (bool).
2. Các UnitMeasurement hiện có trong DB sẽ được để `DecimalPlaces = 0` (default migration).  
   Admin sẽ cập nhật thủ công những đơn vị nào cần thập phân (m², m³, kg, ...).
3. Server-side validation: Quantity phải là integer khi `DecimalPlaces = 0`.  
   Lỗi trả về user-friendly message.

---

## Phạm vi ảnh hưởng

| Lớp | Thay đổi |
|-----|----------|
| Domain | `UnitMeasurement` + DTOs + Manager |
| Application | `UnitMeasurementAppService` |
| Presentation Contracts | `ProductForOrderModel`, các model có quantity input |
| Presentation Handlers | Handler cho `GetProductListForOrderQuery` và tương tự |
| Web Views | DeliveryNote, PurchaseOrder, GoodsReceipt, Order, Returns |
| JS | `decimal-field.js`, `ProductPicker.js` |
| DB | Migration thêm cột `DecimalPlaces` |

---

## Kế hoạch triển khai

### Phase 1 — Domain: UnitMeasurement

**Files:**
- `NamEcommerce.Domain/Entities/Catalog/UnitMeasurement.cs`
- `NamEcommerce.Domain.Shared/Dtos/Catalog/UnitMeasurementDtos.cs`
- `NamEcommerce.Domain.Services/Catalog/UnitMeasurementManager.cs`
- `NamEcommerce.Data.SqlServer` — EF config + migration

**Thay đổi:**

1. Thêm property vào entity:
   ```csharp
   public int DecimalPlaces { get; private set; } // 0 hoặc 2
   
   internal void SetDecimalPlaces(int decimalPlaces)
   {
       if (decimalPlaces is not (0 or 2))
           throw new ArgumentException("DecimalPlaces phải là 0 hoặc 2.");
       DecimalPlaces = decimalPlaces;
   }
   ```

2. Cập nhật `CreateUnitMeasurementDto` và `UpdateUnitMeasurementDto`:
   ```csharp
   public int DecimalPlaces { get; init; } = 0;
   ```
   Thêm `Verify()` check: `DecimalPlaces is not (0 or 2)` → throw exception.

3. `UnitMeasurementManager`: gọi `SetDecimalPlaces(dto.DecimalPlaces)` khi Create/Update.

4. EF Config: `.Property(x => x.DecimalPlaces).HasDefaultValue(0)` trong `UnitMeasurementConfiguration`.

5. Migration: cột `DecimalPlaces INT NOT NULL DEFAULT 0`.

**Verify:** Existing unit tests vẫn pass; UnitMeasurementManager tests bao gồm case `DecimalPlaces = 1` → exception.

---

### Phase 2 — Application: UnitMeasurementAppService

**Files:**
- `NamEcommerce.Application.Contracts/Dtos/Catalog/UnitMeasurementAppDtos.cs` (nếu có)
- `NamEcommerce.Application.Services/Catalog/UnitMeasurementAppService.cs`
- `NamEcommerce.Application.Contracts/Catalog/IUnitMeasurementAppService.cs`

**Thay đổi:**
- Input DTO `CreateUnitMeasurementInput` / `UpdateUnitMeasurementInput`: thêm `int DecimalPlaces = 0`
- `Validate()`: kiểm tra `DecimalPlaces is not (0 or 2)`
- Map vào Domain DTO khi Create/Update

**Verify:** AppService unit test — create với DecimalPlaces invalid → `Success = false`.

---

### Phase 3 — Presentation Contracts: ProductForOrderModel

Đây là model quan trọng nhất vì `ProductPicker.js` đọc từ đây.

**Files:**
- `NamEcommerce.Web.Contracts/Models/Catalog/ProductForOrderModel.cs`
- Handler: `GetProductListForOrderHandler.cs` (tìm file handler tương ứng)
- Các query result models khác có chứa product + quantity (nếu cần)

**Thay đổi:**

1. `ProductForOrderModel`: thêm
   ```csharp
   public int QuantityDecimalPlaces { get; init; } = 0;
   ```

2. Trong handler, khi populate model:
   ```csharp
   QuantityDecimalPlaces = product.UnitMeasurement?.DecimalPlaces ?? 0
   ```
   (cần JOIN hoặc include UnitMeasurement trong query)

3. JSON response tự động include trường mới — ProductPicker.js đọc được.

**Verify:** Gọi `/Product/Search?q=gạch` → JSON trả về `quantityDecimalPlaces: 2` cho sản phẩm có đơn vị m².

---

### Phase 4 — Frontend: decimal-field.js

**File:** `wwwroot/js/decimal-field.js`

**Thay đổi:**

1. `formatQuantity(raw, decimals)` — thêm param `decimals` (default `2`):
   ```js
   function formatQuantity(raw, decimals) {
       decimals = (decimals === undefined) ? 2 : decimals;
       const n = parseFloat(raw);
       if (isNaN(n)) return raw;
       if (decimals === 0) {
           return Math.round(n).toString().replace(/\B(?=(\d{3})+(?!\d))/g, '.');
       }
       // logic hiện tại cho 2 decimals
       ...
   }
   ```
   **Backward compat:** calls không truyền `decimals` vẫn dùng 2.

2. `createQuantityInput(options)` — thêm `options.decimals` (default `2`):
   ```js
   input.dataset.decimals = String(opts.decimals ?? 2);
   input.placeholder = opts.decimals === 0 ? '0' : '0,00';
   ```

3. `wrapExistingInput(input, type, options)` — đọc `data-decimals` nếu đã set:
   ```js
   // Trước: var decimals = isCurr ? 0 : 2;
   // Sau:
   var decimals = isCurr ? 0 : parseInt(input.dataset.decimals ?? '2', 10);
   ```
   **Quan trọng:** không overwrite `data-decimals` nếu đã có sẵn từ Razor.

4. `blur` handler: khi format, truyền `decimals` vào `formatQuantity`:
   ```js
   this.value = type === 'currency' ? formatCurrency(raw) : formatQuantity(raw, decimals);
   ```

**Verify:** Nhập `1.5` vào input với `data-decimals="0"` → blur hiển thị `2` (làm tròn), không phải `1,50`.

---

### Phase 5 — Frontend: ProductPicker.js

**File:** `wwwroot/modules/ProductPicker.js`

**Thay đổi:**

1. Class `Product`: thêm `decimalPlaces`:
   ```js
   constructor({ ..., decimalPlaces }) {
       ...
       this.decimalPlaces = decimalPlaces ?? 0;
   }
   ```

2. Khi user chọn sản phẩm và callback `onProductSelected(product)` được gọi, caller (view JS) chịu trách nhiệm set `data-decimals` lên quantity input.  
   Hoặc ProductPicker expose `product.decimalPlaces` qua event/callback để view xử lý.

3. `formatQuantity` trong ProductPicker (hiển thị tồn kho): truyền `product.decimalPlaces`:
   ```js
   DecimalFields.formatQuantity(product.availableQty, product.decimalPlaces)
   ```

**Verify:** Search sản phẩm "gạch men" → availableQty hiển thị `"12,50 m²"`, search "gương" → `"5 cái"`.

---

### Phase 6 — Views: Gắn data-decimals trên existing items

Các view hiển thị line items với quantity input cho **sản phẩm đã có** (edit mode) cần truyền `DecimalPlaces` từ model vào attribute.

**Views cần cập nhật:**
- `DeliveryNote/Create.cshtml` — quantity input
- `DeliveryNote/Details.cshtml` — edit quantity
- `PurchaseOrder/Details.cshtml` — receive/order quantity
- `GoodsReceipt/Create.cshtml` / `Details.cshtml`
- `Order/Create.cshtml` / `Details.cshtml`
- `CustomerReturn/Create.cshtml`
- `VendorReturn/Create.cshtml`

**Pattern:**
```html
<!-- Trước: -->
<input asp-for="Items[i].Quantity" data-decimal="quantity" />

<!-- Sau: -->
<input asp-for="Items[i].Quantity" 
       data-decimal="quantity" 
       data-decimals="@item.DecimalPlaces" />
```

Cần thêm `int DecimalPlaces` vào các model tương ứng (view models cho line items), populate từ UnitMeasurement của product.

**Verify:** Mở phiếu xuất hàng chứa "gương" → quantity input chỉ cho nhập số nguyên; chứa "gạch men" → cho nhập 1 chữ số thập phân.

---

### Phase 7 — Server-side validation

Validation integer-only khi `DecimalPlaces = 0`.

**Locations:**
- `CreateDeliveryNoteCommand` handler / AppService
- `ReceivePurchaseOrderItem` handler / AppService
- `CreateOrder` handler / AppService
- `CreateGoodsReceipt` handler / AppService
- `CreateCustomerReturn` / `CreateVendorReturn` handlers
- Inventory adjustment handlers

**Pattern (AppService):**
```csharp
// Trước khi tạo item, lấy product's UnitMeasurement
var product = await _productReader.GetByIdAsync(item.ProductId);
var decimalPlaces = product?.UnitMeasurement?.DecimalPlaces ?? 0;
if (decimalPlaces == 0 && item.Quantity != Math.Floor(item.Quantity))
    return AppServiceResult.Fail($"Sản phẩm '{product.Name}' chỉ nhập số nguyên.");
```

**Verify:** POST tạo phiếu xuất với quantity=1.5 cho sản phẩm integer-only → HTTP 400, message rõ ràng.

---

### Phase 8 — UI: UnitMeasurement Admin

Màn hình quản lý đơn vị đo cần cho phép set `DecimalPlaces`.

**Views:**
- `UnitMeasurement/Create.cshtml` — thêm dropdown/radio "Cho phép thập phân: Không / Có (2 chữ số)"
- `UnitMeasurement/Edit.cshtml` — tương tự
- `UnitMeasurement/List.cshtml` / `Details.cshtml` — hiển thị DecimalPlaces

**Commands:**
- `CreateUnitMeasurementCommand` / `UpdateUnitMeasurementCommand` — thêm `DecimalPlaces`
- Command handlers tương ứng

---

## Thứ tự thực hiện

```
P1 (Domain)  →  P2 (AppService)  →  Migration  →  P3 (Contracts)  
→  P4 (decimal-field.js)  →  P5 (ProductPicker)  →  P6 (Views)  
→  P7 (Server validation)  →  P8 (Admin UI)
```

P4 và P5 có thể làm song song với P2 và P3 sau khi P1 xong.

---

## Risk / Tradeoffs

| Risk | Mitigation |
|------|-----------|
| DB migration với existing data: tất cả UnitMeasurement có `DecimalPlaces=0` | Cần admin thủ công update các đơn vị như m², m³, kg → thêm task onboarding |
| Views nhiều, dễ bỏ sót | Grep toàn bộ `data-decimal="quantity"` để tìm hết các views |
| JS change `formatQuantity` ảnh hưởng hiển thị tồn kho, báo cáo | Backward compat: tham số `decimals` có default = 2, không breaking |
| Server validation strict ở Phase 7 có thể reject data cũ | Chỉ bật validation cho records mới, không retroactive |

---

## Out of scope

- Quantity precision cho `PurchaseOrder` PO Unit vs Inventory Unit (multi-UoM conversion) — task riêng
- Audit trail khi admin đổi DecimalPlaces của một UnitMeasurement đã dùng
