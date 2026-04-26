# TodoList — VLXD Tuấn Khôi / NamEcommerce

> File theo dõi các hạng mục cần thực thi. Cập nhật trạng thái khi bắt đầu / hoàn thành.

---

## Module Inventory — GoodsReceipt

**Cấp độ:** Trung bình

### [PRIORITY: HIGH] Sửa 4 lỗi nghiệp vụ: tồn kho, giá vốn, và xóa phiếu

---

#### Vấn đề 1 — Tạo phiếu nhập không cộng tồn kho ✅ DONE 2026-04-25

`GoodsReceiptManager.CreateGoodsReceiptAsync` chỉ publish `EntityCreated` nhưng không có handler nào gọi `ReceiveStockAsync`. Hệ quả: nhập hàng xong tồn kho vẫn = 0.

**Việc cần làm:**
- [x] Thêm `StockReferenceType.GoodsReceipt = 6` vào enum trong `StockMovementLog.cs`
- [x] Tạo `GoodsReceiptCreatedHandler` tại `Application.Services/Events/GoodsReceipts/`
  - Implements `INotificationHandler<EntityCreatedNotification<GoodsReceipt>>`
  - Lặp qua `Items`, với mỗi item có `WarehouseId` → gọi `IInventoryStockManager.ReceiveStockAsync(..., referenceType: StockReferenceType.GoodsReceipt, referenceId: goodsReceipt.Id)`

---

#### Vấn đề 2 — Set giá vốn không cập nhật AverageCost ✅ DONE 2026-04-26

`SetGoodsReceiptItemUnitCostAsync` chỉ lưu `UnitCost` trên item, không cập nhật `InventoryStock.AverageCost`. Báo cáo giá vốn hàng bán sẽ sai.

**Thiết kế đã được xác nhận — Full Recalculation:**

Mỗi khi một item được định giá lần đầu, **tính lại AverageCost từ toàn bộ GoodsReceiptItems đã có giá** cho cùng `(ProductId, WarehouseId)`:

```
AverageCost = SUM(item.Quantity × item.UnitCost)   ← chỉ items đã có UnitCost
              ─────────────────────────────────
                       SUM(item.Quantity)
```

Lý do **không dùng cập nhật tăng dần (incremental)**:
- Nhiều phiếu nhập được tạo không biết giá trước, giá được điền vào sau ở các ngày khác nhau và không theo thứ tự
- Incremental sẽ cho kết quả sai tùy theo thứ tự set giá
- Full Recalculation luôn cho kết quả đúng bất kể thứ tự

**Lưu ý:** AverageCost trong giai đoạn còn item chưa định giá chỉ phản ánh phần đã biết giá — dùng `IsPendingCosting()` ở UI để cảnh báo người dùng.

**Việc cần làm:**
- [x] Thêm property `AverageCost decimal` vào `InventoryStock` entity *(2026-04-26 — Tuấn cần chạy `Add-Migration AddAverageCostToInventoryStock`)*
- [x] Thêm method `UpdateAverageCostAsync(Guid productId, Guid warehouseId, decimal newAverageCost)` vào `IInventoryStockManager` + implement trong `InventoryStockManager` *(2026-04-26 — kèm 5 unit tests TDD: negative, not found, unchanged-idempotent, valid, zero. Thêm error key `Error.StockAverageCostCannotBeNegative` vào cả 2 .resx)*
- [x] Trong `GoodsReceiptManager.SetGoodsReceiptItemUnitCostAsync`: pass `item.Id` vào `AdditionalData` khi publish `EntityUpdated` để handler phân biệt được đây là UnitCost update *(2026-04-26 — kèm unit test mới `SetGoodsReceiptItemUnitCostAsync_ValidDto_PublishesEntityUpdatedWithItemIdAsAdditionalData` verify AdditionalData == itemId)*
- [x] Cập nhật `GoodsReceiptUpdatedHandler` *(2026-04-26 — refactor toàn bộ handler. Inject `IInventoryStockManager` + `IEntityDataReader<GoodsReceipt>`. Bỏ deps cũ `IRepository<Picture>`/`IEntityDataReader<Picture>` vì logic xoá ảnh cũ đã bị comment-out từ trước. Dispatch theo pattern `notification.AdditionalData is Guid itemId` — chỉ kích hoạt khi là Guid (SetUnitCost flow), bỏ qua các flow khác (UpdateGoodsReceiptAsync truyền `IEnumerable<Guid>`). Defensive: kiểm tra null entity / item / WarehouseId / UnitCost. Query LINQ qua EF: `DataSource.SelectMany(gr => gr.Items).Where(i => i.ProductId == ... && i.WarehouseId == ... && i.UnitCost.HasValue)` rồi tính `Σ(qty × cost) / Σ(qty)`. Guard `totalQuantity <= 0` để tránh chia 0)*
  - Kiểm tra `notification.AdditionalData is Guid itemId` → là lần set giá
  - Lấy item từ `notification.Entity.Items.FirstOrDefault(i => i.Id == itemId)`
  - Nếu `item.WarehouseId == null` hoặc `item.UnitCost == null` → bỏ qua
  - Query toàn bộ `GoodsReceipt` có items cùng `(ProductId, WarehouseId)` đã có `UnitCost`
  - Tính `newAvg = Σ(qty × cost) / Σ(qty)`
  - Gọi `UpdateAverageCostAsync(productId, warehouseId, newAvg)`

---

#### Vấn đề 3 — Xóa phiếu không hoàn nguyên tồn kho ✅ DONE 2026-04-26

`DeleteGoodsReceiptAsync` xóa phiếu nhưng không trừ tồn đã cộng → dữ liệu tồn kho ảo.

**Quyết định thiết kế: Cấm xóa phiếu đã cộng tồn**

Thay vì reverse stock (phức tạp, dễ âm tồn nếu đã bán một phần), hệ thống sẽ block xóa nếu đã có `StockMovementLog` tham chiếu đến phiếu này.

**Việc cần làm:**
- [x] Tạo exception `GoodsReceiptHasStockMovementsException` tại `Domain.Shared/Exceptions/GoodsReceipts/`
- [x] Inject `IEntityDataReader<StockMovementLog>` vào `GoodsReceiptManager`
- [x] Trong `DeleteGoodsReceiptAsync`: kiểm tra tồn tại `StockMovementLog` nào có `ReferenceType == GoodsReceipt && ReferenceId == dto.GoodsReceiptId` → nếu có thì throw exception
- [x] Thêm localization key `"Error.GoodsReceipt.CannotDeleteHasStockMovements"` vào `Resources/SharedResource.vi-VN.resx` và `Resources/SharedResource.resx` (bản gốc EN). Value VI: `"Không thể xóa phiếu nhập đã phát sinh tồn kho. Hãy điều chỉnh tồn (Adjust) hoặc tạo phiếu xuất bù trừ thay vì xóa."` Value EN: `"Cannot delete a goods receipt that has stock movements. Please use stock adjustment or create an offsetting issue note instead."`

> ⚠️ **Cần test sau khi thêm DI:** `GoodsReceiptManager` constructor đã thêm tham số `IEntityDataReader<StockMovementLog>`. `IEntityDataReader<>` đăng ký bằng generic open type ở `Program.cs` nên không cần register thêm — nhưng nên build verify ngay sau khi merge để chắc.

---

**Files chính cần đụng:**
- `NamEcommerce.Domain/Entities/Inventory/StockMovementLog.cs` — thêm enum value
- `NamEcommerce.Domain/Entities/Inventory/InventoryStock.cs` — thêm `AverageCost`
- `NamEcommerce.Domain.Shared/Services/Inventory/IInventoryStockManager.cs` — thêm `UpdateAverageCostAsync`
- `NamEcommerce.Domain.Services/Inventory/InventoryStockManager.cs` — implement method trên
- `NamEcommerce.Domain.Shared/Exceptions/GoodsReceipts/GoodsReceiptHasStockMovementsException.cs` — file mới
- `NamEcommerce.Domain.Services/GoodsReceipts/GoodsReceiptManager.cs` — inject StockMovementLog reader, sửa Delete + Set UnitCost
- `NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptCreatedHandler.cs` — file mới
- `NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptUpdatedHandler.cs` — sửa để handle AverageCost

**Phụ thuộc:**
- Vấn đề 3 phụ thuộc vào vấn đề 1 (cần có `StockReferenceType.GoodsReceipt` trước)
- Vấn đề 2 phụ thuộc vào vấn đề 1 (cần handler created chạy đúng trước mới có stock để tính avg)
- Cần tạo migration cho `InventoryStock.AverageCost` trước khi deploy vấn đề 2

---

### 📌 Trạng thái hiện tại (cập nhật 2026-04-26)

**Đã làm xong session này:**
- ✅ Toàn bộ module **UI/UX Notification** (Phase 1 → 4) — đã hoàn tất cả phần JS module migrate optional ở Phase 4
- ✅ GoodsReceipt **Vấn đề 1** (Tạo phiếu nhập cộng tồn kho) — handler đã sẵn sàng nhận event
- ✅ GoodsReceipt **Vấn đề 2** (Full Recalculation AverageCost) — `GoodsReceiptUpdatedHandler` đã refactor xong 2026-04-26
- ✅ GoodsReceipt **Vấn đề 3** (Cấm xóa) — đã thêm localization key vào cả `SharedResource.vi-VN.resx` và `SharedResource.resx` ngày 2026-04-26
- ✅ JS module migrate apiPost/apiGet (`order.details.js`, `CreatePurchaseOrderController.js`, `OrderController.js`) — 2026-04-26 (scheduled task)
- ✅ **Vendor + Sinh công nợ tự động** — Phase 1 Domain + Phase 2 Application + Phase 3 backend & UI — **2026-04-26 scheduled task (3 lần làm)**. UI Views Create.cshtml + Details.cshtml + VendorPicker integration đều đã hoàn thành.

**Lượt sau bắt đầu từ:**
1. **Build verify** toàn bộ solution — `dotnet build` để chắc:
   - DI cho `IEntityDataReader<StockMovementLog>` + `IEntityDataReader<GoodsReceipt>` + `IVendorDebtManager` (đã có sẵn trong DI) không vỡ
   - `VendorDebtManagerTests.cs` compile sạch sau khi fix helper 7→8 params
   - Các thay đổi Notification + Vendor + Handler module compile sạch (sandbox không có dotnet, Tuấn tự chạy)
2. **Smoke test** flow nghiệp vụ:
   - **AverageCost flow:**
     - Tạo phiếu nhập có WarehouseId + UnitCost → tồn kho cộng + AverageCost cập nhật
     - Tạo phiếu nhập KHÔNG có UnitCost → tồn cộng, AverageCost không thay đổi
     - Set UnitCost cho item của phiếu nhập đó → AverageCost cập nhật theo Full Recalculation
     - Tạo phiếu nhập thứ 2 cùng (Product, Warehouse) khác giá → set giá → AverageCost = trung bình cộng có trọng số
   - **Xóa phiếu:**
     - Thử xóa phiếu đã sinh tồn → exception `GoodsReceiptHasStockMovementsException` (key VI hiển thị đúng)
   - **Vendor + sinh công nợ tự động (mới):**
     - Tạo phiếu KHÔNG vendor + items có UnitCost đầy đủ → KHÔNG sinh nợ
     - Tạo phiếu CÓ vendor + items có UnitCost đầy đủ → sinh 1 phiếu `VendorDebt` với `GoodsReceiptId = phiếu.Id`, `TotalAmount = Σ(qty × cost)`
     - Tạo phiếu CÓ vendor nhưng items chưa định giá → KHÔNG sinh nợ; sau khi set UnitCost cho item cuối → sinh nợ
     - Phiếu CÓ items định giá đầy đủ nhưng KHÔNG vendor → POST `/GoodsReceipt/SetVendor` với `vendorId` → sinh nợ
     - Gọi POST `/GoodsReceipt/SetVendor` 2 lần liên tiếp với cùng vendor → CHỈ 1 phiếu nợ (idempotency)
3. **Sau cùng** — Module **System - Event Refactor** (Khó + HIGH, ~1 tuần) — bắt đầu từ Phase 1 Foundation

**Files đã chạm session 2026-04-26:**
- `Presentation/NamEcommerce.Web/Resources/SharedResource.vi-VN.resx` — thêm key `Error.GoodsReceipt.CannotDeleteHasStockMovements` + `Error.StockAverageCostCannotBeNegative` (VI)
- `Presentation/NamEcommerce.Web/Resources/SharedResource.resx` — thêm 2 keys tương ứng (EN)
- `Domain/NamEcommerce.Domain/Entities/Inventory/InventoryStock.cs` — thêm property `AverageCost decimal` (default 0, internal set)
- `Infrastructure/NamEcommerce.Data.SqlServer/Mappings/InventoryStockMapping.cs` — thêm `decimal(18,2)` mapping cho `AverageCost`
- `Domain/NamEcommerce.Domain.Shared/Services/Inventory/IInventoryStockManager.cs` — thêm signature `UpdateAverageCostAsync`
- `Domain/NamEcommerce.Domain.Services/Inventory/InventoryStockManager.cs` — implement `UpdateAverageCostAsync` (validate negative, throw StockNotFound, idempotent skip nếu không đổi)
- `Tests/NamEcommerce.Domain.Services.Test/Helpers/InventoryStockDataReader.cs` — file mới (test helper)
- `Tests/NamEcommerce.Domain.Services.Test/Helpers/InventoryStockRepository.cs` — file mới (test helper)
- `Tests/NamEcommerce.Domain.Services.Test/Services/InventoryStockManagerTests.cs` — file mới, 5 test cases TDD cho `UpdateAverageCostAsync`
- `Domain/NamEcommerce.Domain.Services/GoodsReceipts/GoodsReceiptManager.cs` — `SetGoodsReceiptItemUnitCostAsync` pass `item.Id` qua `AdditionalData` của `EntityUpdated`
- `Tests/NamEcommerce.Domain.Services.Test/Services/GoodsReceiptManagerTests.cs` — thêm 1 test verify AdditionalData == itemId + import `Events.Entities` namespace
- `Application/NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptUpdatedHandler.cs` — refactor toàn bộ. Bỏ deps `IRepository<Picture>`/`IEntityDataReader<Picture>` (logic cũ đã comment-out). Inject `IInventoryStockManager` + `IEntityDataReader<GoodsReceipt>`. Khi `AdditionalData is Guid itemId` → tính `newAvg = Σ(qty × cost) / Σ(qty)` từ tất cả `GoodsReceiptItem` cùng `(ProductId, WarehouseId)` đã có `UnitCost` rồi gọi `UpdateAverageCostAsync`. Defensive: skip nếu Entity null, item null, WarehouseId null, UnitCost null hoặc totalQuantity <= 0 (tránh chia 0)
- `Presentation/NamEcommerce.Web/wwwroot/modules/order.details.js` — thay tất cả `fetch(...)` bằng `apiPost(...)` từ `ajax-helper.js`. Thêm helper `submitFormAsync(form)` ở module scope; dùng `FormData` cho request POST (server vẫn nhận form-binding default, không phá `[FromBody]`-less controllers). `RemoveOrderItem` chuyển sang FormData (đã verify controller không có `[FromBody]`). `CreateFromPreparation` giữ JSON object (controller có `[FromBody]`)
- `Presentation/NamEcommerce.Web/wwwroot/modules/CreatePurchaseOrderController.js` — import `apiGet`, thay `fetch('/PurchaseOrder/RecentPurchasePrices...')` bằng `apiGet(...)`. Endpoint trả mảng thuần → đọc qua `result.data ?? result` (tương thích cả khi shape có/không có `success`)
- `Presentation/NamEcommerce.Web/wwwroot/modules/OrderController.js` — import `apiGet`, thay `fetch('/Product/PriceHistory...')` bằng `apiGet(...)`. Đọc payload qua `result.data ?? result`

**Vendor + Sinh công nợ tự động (Phase 2 + Phase 3 backend) — 2026-04-26:**
- `Application/NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptUpdatedHandler.cs` — refactor lần 2: tách `RecalculateAverageCostAsync` + `TryCreateVendorDebtAsync` thành private helpers. `switch` trên `AdditionalData`: `Guid itemId` → recalc AverageCost + try sinh công nợ; `"vendor-updated"` → chỉ try sinh công nợ. Inject thêm `IVendorDebtManager`. Idempotency của `CreateDebtFromGoodsReceiptAsync` đảm bảo gọi nhiều lần chỉ tạo 1 phiếu nợ.
- `Application/NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptCreatedHandler.cs` — mở rộng: sau cộng tồn kho, gọi `TryCreateVendorDebtAsync` để xử lý edge case phiếu tạo với đủ `VendorId` + `UnitCost` cho mọi item ngay từ đầu (`AddGoodsReceiptItemDto` cho phép set UnitCost lúc tạo). Inject thêm `IVendorDebtManager`.
- `Domain/NamEcommerce.Domain.Shared/Services/Debts/IVendorDebtManager.cs` — thêm signature `GetDebtByGoodsReceiptIdAsync(Guid)` trả `VendorDebtDto?`. Idempotency của `CreateDebtFromGoodsReceiptAsync` đảm bảo có tối đa 1 debt / 1 phiếu nhập.
- `Domain/NamEcommerce.Domain.Services/Debts/VendorDebtManager.cs` — implement `GetDebtByGoodsReceiptIdAsync`: filter `DataSource.FirstOrDefault(d => d.GoodsReceiptId == goodsReceiptId)` + load payments theo `debt.Id`.
- `Application/NamEcommerce.Application.Contracts/Debts/IVendorDebtAppService.cs` — thêm signature `GetDebtByGoodsReceiptIdAsync(Guid)` trả `VendorDebtAppDto?`.
- `Application/NamEcommerce.Application.Services/Debts/VendorDebtAppService.cs` — implement: gọi `_debtManager.GetDebtByGoodsReceiptIdAsync` rồi `.ToDto()`.
- `Presentation/NamEcommerce.Web.Contracts/Models/GoodsReceipts/GoodsReceiptModel.cs` — thêm 3 properties cho UI badge: `HasVendorDebt bool`, `VendorDebtId Guid?`, `VendorDebtTotalAmount decimal?`.
- `Presentation/NamEcommerce.Web.Framework/Queries/Handlers/GoodsReceipts/GetGoodsReceiptHandler.cs` — inject `IVendorDebtAppService`. Sau khi load `goodsReceipt`, gọi `GetDebtByGoodsReceiptIdAsync(request.Id)` để map `HasVendorDebt`/`VendorDebtId`/`VendorDebtTotalAmount`.
- `Tests/NamEcommerce.Domain.Services.Test/Services/VendorDebtManagerTests.cs` — fix pre-existing compile bug: thêm `Mock<IEntityDataReader<GoodsReceipt>>? goodsReceiptReader = null` vào helper `CreateManager` (constructor cần 8 params, helper trước đó passing 7). Thêm 3 unit tests TDD cho `GetDebtByGoodsReceiptIdAsync`: NotSinhYet → null; DebtExistsForGoodsReceipt → DTO + Payments; OtherGoodsReceiptHasDebt → null cho ID không liên quan.

**Vendor + Sinh công nợ tự động (Phase 3 UI Views) — 2026-04-26:**
- `Presentation/NamEcommerce.Web/Models/GoodsReceipts/CreateGoodsReceiptModel.cs` — thêm `VendorId Guid?` + 3 display fields (`VendorDisplayName/Phone/Address`) marked `[ValidateNever]` để re-render khi ModelState fail.
- `Presentation/NamEcommerce.Web.Contracts/Commands/Models/GoodsReceipts/CreateGoodsReceiptCommand.cs` — thêm `VendorId Guid?`.
- `Presentation/NamEcommerce.Web.Framework/Commands/Handlers/GoodsReceipts/CreateGoodsReceiptHandler.cs` — pass `VendorId` xuống `CreateGoodsReceiptAppDto` (AppDto đã có sẵn field này).
- `Presentation/NamEcommerce.Web/Controllers/GoodsReceiptController.cs` — pass `model.VendorId` vào `CreateGoodsReceiptCommand`.
- `Presentation/NamEcommerce.Web/Models/GoodsReceipts/GoodsReceiptDetailsModel.cs` — thêm 4 vendor snapshot fields + 3 vendor debt fields (`HasVendorDebt`, `VendorDebtId`, `VendorDebtTotalAmount`).
- `Presentation/NamEcommerce.Web/Services/GoodsReceipts/GoodsReceiptModelFactory.cs` — `PrepareGoodsReceiptDetailsModel` map vendor + debt fields từ `GoodsReceiptModel`. `PrepareCreateGoodsReceiptModel` re-query vendor info qua `GetVendorQuery` khi `model.VendorId.HasValue` mà display fields trống (round-trip fail từ form submit).
- `Presentation/NamEcommerce.Web/Views/GoodsReceipt/Create.cshtml` — thêm section "Nhà cung cấp" trong cột thông tin phiếu (giữa WarehouseId và TruckDriverName). Hidden `VendorId` + `<div id="vendorPicker">` data-* attributes pre-populated. Script khởi tạo `VendorPicker` từ `/modules/VendorPicker.js`, wire `select`/`remove` event vào hidden input. Initial vendor render bằng plain object (bypass Vendor constructor — pre-existing constructor không destructure `address`).
- `Presentation/NamEcommerce.Web/Views/GoodsReceipt/Details.cshtml` — thêm card "Nhà cung cấp" giữa "Thông tin phiếu" và "Ảnh chụp": hiển thị tên/SĐT/địa chỉ + badge "Đã ghi nợ [TotalAmount]" link sang `VendorDebt/Details?vendorId=...` hoặc badge "Sẽ ghi sau khi định giá xong". Modal `#editVendorModal` chứa VendorPicker + nút Lưu submit AJAX qua `apiPost('/GoodsReceipt/SetVendor', FormData{goodsReceiptId, vendorId?})`. Toast success rồi `location.reload()` để badge cập nhật. No-op khi vendor không đổi.

⚠️ **Tuấn cần chạy thủ công sau khi merge session 2026-04-26:**
- `Add-Migration AddAverageCostToInventoryStock` (project Data.SqlServer)
- `Add-Migration AddVendorToGoodsReceiptAndDebt` — schema cho 4 cột vendor trên `GoodsReceipt` + đổi `PurchaseOrderId/Code` của `VendorDebt` thành nullable + thêm `GoodsReceiptId` (Phase 1 Vendor)
- `Update-Database`
- `dotnet build NamEcommerce.sln` để verify toàn bộ compile sạch (sandbox không có dotnet). Đặc biệt verify `VendorDebtManagerTests.cs` (đã fix pre-existing helper bug 7→8 params) compile thành công.

**Files đã chạm session trước (chưa build verify):**
- `Domain/Entities/Inventory/StockMovementLog.cs` — thêm `GoodsReceipt = 6`
- `Domain.Shared/Exceptions/GoodsReceipts/GoodsReceiptHasStockMovementsException.cs` — file mới
- `Domain.Services/GoodsReceipts/GoodsReceiptManager.cs` — inject `IEntityDataReader<StockMovementLog>` + check trong Delete
- `Application.Services/Events/GoodsReceipts/GoodsReceiptCreatedHandler.cs` — file mới

---

### [PRIORITY: HIGH] Thêm nhà cung cấp (Vendor) vào phiếu nhập + sinh công nợ tự động

**Mục tiêu:**
- `GoodsReceipt` có thể gắn 1 nhà cung cấp (optional, cập nhật được sau).
- Khi toàn bộ items được định giá (`IsPendingCosting() == false`) **VÀ** đã có VendorId → tự động tạo `VendorDebt` với tổng tiền = Σ(Quantity × UnitCost).
- Idempotent: gọi lại nhiều lần không tạo thêm phiếu nợ trùng.

---

#### Phase 1 — Domain: Mở rộng GoodsReceipt & VendorDebt

**`GoodsReceipt` entity** (`Domain/Entities/GoodsReceipts/GoodsReceipt.cs`): ✅ DONE
- [x] Thêm 4 properties (tất cả nullable, snapshot thông tin NCC tại thời điểm nhập):
  ```csharp
  public Guid?   VendorId      { get; internal set; }
  public string? VendorName    { get; internal set; }
  public string? VendorPhone   { get; internal set; }
  public string? VendorAddress { get; internal set; }
  ```
- [x] Thêm method `internal void SetVendor(Guid vendorId, string vendorName, string? phone, string? address)`
- [x] Thêm method `internal void ClearVendor()` (xoá vendor nếu cần huỷ liên kết)

**`GoodsReceiptDtos`** (`Domain.Shared/Dtos/GoodsReceipts/GoodsReceiptDtos.cs`): ✅ DONE
- [x] Thêm `VendorId?`, `VendorName?`, `VendorPhone?`, `VendorAddress?` vào `BaseGoodsReceiptDto`
- [x] Thêm record `SetGoodsReceiptVendorDto(Guid GoodsReceiptId)` với `VendorId?` (null = xoá vendor)

**`IGoodsReceiptManager`** + `GoodsReceiptManager`: ✅ DONE
- [x] Cập nhật `CreateGoodsReceiptAsync` / `UpdateGoodsReceiptAsync` để set vendor khi có trong dto
  - Cần inject `IEntityDataReader<Vendor>` để validate vendor tồn tại trước khi set
- [x] Thêm method `SetGoodsReceiptVendorAsync(SetGoodsReceiptVendorDto dto)`:
  - Nếu `dto.VendorId` có giá trị: get Vendor by Id → throw nếu không tồn tại → gọi `goodsReceipt.SetVendor(...)`
  - Nếu `dto.VendorId == null`: gọi `goodsReceipt.ClearVendor()`
  - Publish `EntityUpdated(goodsReceipt, additionalData: "vendor-updated")` để handler nhận biết

**`VendorDebt` entity** (`Domain/Entities/Debts/VendorDebt.cs`) — **schema breaking change**: ✅ DONE
- [x] Đổi `public Guid PurchaseOrderId { get; private set; }` → `public Guid? PurchaseOrderId { get; private set; }`
- [x] Đổi `public string PurchaseOrderCode { get; private set; }` → `public string? PurchaseOrderCode { get; private set; }`
- [x] Thêm `public Guid? GoodsReceiptId { get; private set; }`
- [x] Thêm internal constructor mới cho GoodsReceipt-based debt:
  ```csharp
  internal VendorDebt(string code, Guid vendorId, string vendorName,
      Guid goodsReceiptId, decimal totalAmount, DateTime? dueDateUtc, Guid? createdByUserId)
  ```
  → Set `GoodsReceiptId = goodsReceiptId`, `PurchaseOrderId = null`, `PurchaseOrderCode = null`
- [x] Giữ constructor PurchaseOrder cũ **nguyên vẹn** — chỉ thêm constructor mới

**`IVendorDebtManager`** + `VendorDebtManager`: ✅ DONE
- [x] Thêm `CreateDebtFromGoodsReceiptAsync(CreateVendorDebtFromGoodsReceiptDto dto)` — idempotent:
  - Check tồn tại `VendorDebt` có `GoodsReceiptId == dto.GoodsReceiptId` → return existing nếu đã có
  - Nếu chưa → tạo mới với constructor GoodsReceipt
- [x] Thêm `CreateVendorDebtFromGoodsReceiptDto`:
  ```csharp
  public record CreateVendorDebtFromGoodsReceiptDto {
      public required Guid GoodsReceiptId  { get; init; }
      public required Guid VendorId        { get; init; }
      public required string VendorName    { get; init; }
      public string? VendorPhone           { get; init; }
      public string? VendorAddress         { get; init; }
      public required decimal TotalAmount  { get; init; }
      public DateTime? DueDateUtc          { get; init; }
      public Guid? CreatedByUserId         { get; init; }
  }
  ```

> ⚠️ **Migration cần thiết:**
> - `GoodsReceipt` table: thêm 4 columns `VendorId`, `VendorName`, `VendorPhone`, `VendorAddress` (nullable)
> - `VendorDebt` table: đổi `PurchaseOrderId` + `PurchaseOrderCode` thành nullable, thêm column `GoodsReceiptId` (nullable FK)
> - Tuấn chạy `Add-Migration AddVendorToGoodsReceiptAndDebt` sau khi merge phase này

---

#### Phase 2 — Application: Tự động sinh công nợ khi định giá xong ✅ DONE 2026-04-26

Mở rộng `GoodsReceiptUpdatedHandler` (đã có logic AverageCost):

- [x] Sau bước tính AverageCost, kiểm tra thêm điều kiện sinh công nợ:
  ```
  AdditionalData is Guid itemId           → là lần set UnitCost
  !goodsReceipt.IsPendingCosting()        → tất cả items đã có giá
  goodsReceipt.VendorId.HasValue          → đã gắn nhà cung cấp
  ```
  Nếu thoả tất cả → gọi `IVendorDebtManager.CreateDebtFromGoodsReceiptAsync(...)`:
  - `TotalAmount = goodsReceipt.Items.Sum(i => i.Quantity * i.UnitCost!.Value)`
  - `VendorId`, `VendorName`, `VendorPhone`, `VendorAddress` lấy từ `goodsReceipt`

- [x] Thêm case xử lý khi `AdditionalData is "vendor-updated"` (từ `SetGoodsReceiptVendorAsync`):
  - Kiểm tra `!goodsReceipt.IsPendingCosting()` → nếu đã đủ giá → cũng trigger sinh công nợ
  - Cho phép trường hợp: nhập trước, định giá xong, sau đó mới chọn vendor → vẫn tạo được nợ
- [x] **Bonus:** mở rộng `GoodsReceiptCreatedHandler` để xử lý edge case: phiếu nhập tạo với đủ vendor + UnitCost cho mọi item ngay từ đầu → sinh nợ luôn (do `AddGoodsReceiptItemDto` cho phép set UnitCost lúc tạo)

> **Idempotency đảm bảo:** `CreateDebtFromGoodsReceiptAsync` check `GoodsReceiptId` trước khi tạo — gọi 2 lần chỉ sinh 1 phiếu nợ.

---

#### Phase 3 — Presentation: UI + API

**Endpoint mới trong `GoodsReceiptController`:** ✅ DONE
- [x] `POST /GoodsReceipt/SetVendor` — AJAX, nhận `{ id, vendorId? }`:
  - Gọi `SetGoodsReceiptVendorAsync`
  - Return `JsonOk()` / `JsonError()`

**`GET /Vendor/Options`** (kiểm tra xem đã có chưa): ✅ DONE
- [x] Đã có sẵn trong `VendorController.Options(string? q)` — trả `[{ value, label }]`

**Create page** (`Views/GoodsReceipt/Create.cshtml`): ✅ DONE 2026-04-26
- [x] Thêm section chọn vendor (optional) — dùng `VendorPicker.js` (search live qua `/Vendor/Search`)
- [x] Hidden input `VendorId` + hiển thị tên/SĐT/địa chỉ vendor đã chọn
- [x] Nút "Xoá nhà cung cấp" (clear) — built-in trong VendorPicker.js
- [x] Re-populate vendor display khi ModelState fail (server query lại Vendor info qua `GetVendorQuery`)

**Details page** (`Views/GoodsReceipt/Details.cshtml`): ✅ DONE 2026-04-26
- [x] Hiển thị card "Nhà cung cấp" riêng biệt:
  - Nếu chưa có vendor: text mờ "— Chưa có nhà cung cấp" + nút "Gắn NCC"
  - Nếu đã có vendor: tên + SĐT + địa chỉ + nút "Thay đổi"
- [x] Inline vendor picker (modal Bootstrap) → AJAX `POST /GoodsReceipt/SetVendor` qua `apiPost` (helper từ `ajax-helper.js`, tự xử lý antiforgery token)
- [x] Sau khi set vendor thành công: toast "Đã cập nhật nhà cung cấp" rồi `location.reload()` (cần reload để render badge "Đã ghi nợ" nếu trigger sinh nợ)
- [x] Nếu đã sinh công nợ: badge `"Đã ghi nợ [tổng tiền]"` link sang `VendorDebt/Details?vendorId=...` (không phải debtId — controller hiện liệt kê toàn bộ debts theo vendorId)
- [x] Nếu chưa sinh nhưng có vendor + còn item chưa định giá: badge "Sẽ ghi sau khi định giá xong" (UX hint)

**Cập nhật View Models + Handlers:** ✅ DONE 2026-04-26
- [x] Thêm `VendorId?`, `VendorName?`, `VendorPhone?`, `VendorAddress?` vào `GoodsReceiptModel` (Details). `GoodsReceiptListModel.ItemModel` chưa bổ sung — có thể làm lúc Views nếu cần.
- [x] Thêm `HasVendorDebt bool` + `VendorDebtId Guid?` + `VendorDebtTotalAmount decimal?` vào `GoodsReceiptModel` để hiển thị badge
- [x] Cập nhật `GetGoodsReceiptHandler` — inject `IVendorDebtAppService.GetDebtByGoodsReceiptIdAsync` để map vendor fields + check VendorDebt tham chiếu GoodsReceiptId

---

**Phụ thuộc & thứ tự thực hiện:**
1. Phase 1 (Domain) trước — Phase 2 + 3 phụ thuộc vào
2. `IVendorDebtManager.CreateDebtFromGoodsReceiptAsync` cần có trước khi Phase 2 có thể gọi
3. Migration phải chạy trước khi test end-to-end
4. Phase 3 UI có thể làm song song Phase 2 sau khi Phase 1 xong

**Files chính cần đụng:**
- `Domain/Entities/GoodsReceipts/GoodsReceipt.cs`
- `Domain.Shared/Dtos/GoodsReceipts/GoodsReceiptDtos.cs`
- `Domain.Shared/Services/GoodsReceipts/IGoodsReceiptManager.cs`
- `Domain.Services/GoodsReceipts/GoodsReceiptManager.cs`
- `Domain/Entities/Debts/VendorDebt.cs` ← **schema change, cần migration**
- `Domain.Shared/Dtos/Debts/VendorDebtDtos.cs` — thêm `CreateVendorDebtFromGoodsReceiptDto`
- `Domain.Shared/Services/Debts/IVendorDebtManager.cs`
- `Domain.Services/Debts/VendorDebtManager.cs`
- `Application.Services/Events/GoodsReceipts/GoodsReceiptUpdatedHandler.cs`
- `Data.SqlServer/Mappings/GoodsReceiptMapping.cs` + `VendorDebtMapping.cs`
- `Web/Controllers/GoodsReceiptController.cs` + `VendorController.cs`
- `Web.Contracts/Models/GoodsReceipts/GoodsReceiptModel.cs`
- `Web.Framework/Queries/Handlers/GoodsReceipts/GetGoodsReceiptHandler.cs`
- `Views/GoodsReceipt/Create.cshtml` + `Details.cshtml`

---

## System - Event

**Cấp độ:** Khó

### [PRIORITY: HIGH] Refactor Event System theo DDD đúng chuẩn

**Mục tiêu:** Thay thế mô hình CRUD-style notification (`EntityCreatedEvent<T>`, `EntityUpdatedEvent<T>`, `EntityDeletedEvent<T>`) bằng Domain Events đúng nghĩa DDD — event mang ngữ nghĩa nghiệp vụ, aggregate tự raise event, dispatch sau SaveChanges, tách Domain Event vs Integration Event với Outbox pattern.

**Lý do:**
- Event hiện tại không kể được "vì sao update" → handler `OrderUpdatedEventHandler` rỗng vì không phân biệt được 7 hành vi nghiệp vụ khác nhau cùng publish chung một event.
- `object? AdditionalData` mất type safety, dễ break runtime.
- Manager raise event hộ aggregate → vi phạm encapsulation DDD.
- Publish event sau `SaveChanges` riêng lẻ → không atomic, race condition với external system (n8n).
- `EventPublisher` dùng `switch` + `throw NotSupportedException()` → vi phạm Open/Closed.
- Handler nhận nguyên Entity → leak internal state.
- `BaseEvent` rỗng, thiếu `EventId`, `OccurredOnUtc`, `CorrelationId` → không trace, không replay được.

---

#### Phase 1 — Foundation (1-2 ngày)

- [ ] Tạo `IDomainEvent : INotification` trong `NamEcommerce.Domain.Shared/Events/`
- [ ] Tạo `abstract record DomainEvent : IDomainEvent` với `EventId`, `OccurredOnUtc`
- [ ] Cập nhật `AppAggregateEntity`:
  - Thêm `private readonly List<IDomainEvent> _domainEvents`
  - Expose `IReadOnlyCollection<IDomainEvent> DomainEvents` với `[NotMapped]`
  - Method `protected void RaiseDomainEvent(IDomainEvent)` và `public void ClearDomainEvents()`
- [ ] Viết `DomainEventDispatchInterceptor : SaveChangesInterceptor` trong `NamEcommerce.Data.SqlServer/Interceptors/`
- [ ] Đăng ký Interceptor trong `Program.cs` / `DbContext` configuration
- [ ] Unit test cho Interceptor (mock `IPublisher`, verify dispatch + clear events)
- [ ] Giữ song song `IEventPublisher` cũ — không xoá ngay để tránh break code hiện tại

#### Phase 2 — Migrate Orders + DeliveryNotes (2-3 ngày)

- [ ] Định nghĩa concrete events cho Orders:
  - `OrderPlaced(OrderId, CustomerId, TotalAmount, WarehouseId?)`
  - `OrderItemAdded(OrderId, OrderItemId, ProductId, Quantity)`
  - `OrderItemRemoved(OrderId, OrderItemId)`
  - `OrderLocked(OrderId)`
  - `OrderCancelled(OrderId, Reason)`
  - Thêm các event khác tương ứng với 7 chỗ gọi `EntityUpdated` hiện tại
- [ ] Định nghĩa concrete events cho DeliveryNotes:
  - `DeliveryNoteCreated(DeliveryNoteId, OrderId, CustomerId)`
  - `DeliveryNoteConfirmed(DeliveryNoteId)` — đã có, chuẩn hoá lại
  - `DeliveryNoteDelivered(DeliveryNoteId, CustomerId, TotalAmount)`
  - `DeliveryNoteCancelled(DeliveryNoteId)`
- [ ] Refactor `Order` aggregate: gọi `RaiseDomainEvent(...)` trong constructor + các method nghiệp vụ
- [ ] Refactor `DeliveryNote` aggregate tương tự
- [ ] Xoá `eventPublisher.EntityCreated/Updated/Deleted(...)` trong `OrderManager` và `DeliveryNoteManager`
- [ ] Refactor handler hiện tại theo từng concrete event:
  - `OrderPlacedReserveStockHandler` (thay cho `OrderCreatedEventHandler` đang trống)
  - `OrderCancelledReleaseStockHandler`
  - `DeliveryNoteDeliveredCreateDebtHandler` (refactor từ `DeliveryNoteDeliveredEventHandler`)
- [ ] Update unit test Manager: assert `aggregate.DomainEvents` chứa event mong đợi thay vì verify `eventPublisher.EntityCreated(...)`
- [ ] Smoke test end-to-end: tạo order → confirm delivery note → mark delivered → verify công nợ tự động tạo

#### Phase 3 — Migrate các module còn lại (2 ngày)

- [ ] Catalog: `Product`, `Category`, `Vendor`, `UnitMeasurement`
- [ ] Inventory: `InventoryStock`, `Warehouse`, stock movements
- [ ] PurchaseOrders: `PurchaseOrder` (note: `PurchaseOrderUpdatedEventHandler` hiện đang gọi `VerifyStatusAsync` — cần thay bằng concrete event như `PurchaseOrderItemReceived`)
- [ ] GoodsReceipts: `GoodsReceipt` (dọn handler trống `GoodsReceiptUpdatedHandler`)
- [ ] Debts: `CustomerDebt`, `CustomerPayment`, `VendorDebt`
- [ ] Customers: `Customer`
- [ ] Media: `Picture` (event `PictureOrphaned` thay cho logic xoá ảnh hiện đang nằm trong `ProductDeletedEventHandler`, `ProductUpdatedEventHandler`, `GoodsReceiptDeletedEventHandler`)

#### Phase 4 — Outbox Pattern cho Integration Event (1-2 ngày)

- [ ] Tạo entity `OutboxMessage` (Id, Type, Payload JSON, OccurredOnUtc, ProcessedOnUtc?, Error?)
- [ ] EF Configuration + DbSet trong `DbContext` (KHÔNG tự chạy migration — báo Tuấn tự làm)
- [ ] Tạo `IOutbox` interface + implementation lưu vào DbContext (cùng transaction với SaveChanges)
- [ ] Tạo `IIntegrationEvent` marker
- [ ] Tách integration event:
  - `DeliveryNoteConfirmedIntegrationEvent` — gửi notify n8n (thay cho call trực tiếp `_n8nAppService` hiện tại)
- [ ] `BackgroundService` đọc bảng `OutboxMessages` chưa processed → publish qua MediatR → mark processed; retry nếu lỗi
- [ ] Test failure scenarios: n8n down, DB transaction rollback, duplicate dispatch (idempotency)

#### Phase 5 — Cleanup (0.5 ngày)

- [ ] Xoá `EntityCreatedEvent<T>`, `EntityUpdatedEvent<T>`, `EntityDeletedEvent<T>`
- [ ] Xoá `EntityCreatedNotification<T>`, `EntityUpdatedNotification<T>`, `EntityDeletedNotification<T>`
- [ ] Xoá `IEventPublisher` cũ + `EventPublisher` implementation
- [ ] Xoá `EventPublisherExtensions.EntityCreated/Updated/Deleted`
- [ ] Xoá các handler trống / TODO comment đã được thay thế
- [ ] Update skill `namcommerce` — thay phần "Publish events qua `IEventPublisher`" bằng hướng dẫn raise domain event mới
- [ ] Update `SYSTEM_DOCUMENTATION.md` — ghi rõ pattern Domain Event mới

---

**Tổng effort ước tính:** ~1 tuần làm cẩn thận có test.

**Files chính cần đụng:**
- `NamEcommerce.Domain.Shared/Events/` — toàn bộ event types
- `NamEcommerce.Domain.Shared/Common/AppAggregateEntity.cs`
- `NamEcommerce.Domain.Services/**/*Manager.cs` — bỏ inject `IEventPublisher`, dọn các call publish
- `NamEcommerce.Domain/Entities/**/*.cs` — thêm `RaiseDomainEvent` trong methods nghiệp vụ
- `NamEcommerce.Application.Services/Events/**/*Handler.cs` — split theo concrete event
- `NamEcommerce.Data.SqlServer/Interceptors/DomainEventDispatchInterceptor.cs` — file mới
- `NamEcommerce.Data.SqlServer/Outbox/` — thư mục mới (Phase 4)
- `NamEcommerce.Web/Program.cs` — đăng ký Interceptor + Outbox BackgroundService

**Rủi ro cần lưu ý:**
- `AppAggregateEntity` là `record` — `_domainEvents` (List reference type) hoạt động bình thường nhưng phải `[NotMapped]` để EF không persist
- Test fixtures hiện tại verify `IEventPublisher` mock — cần update sang assert `DomainEvents` collection
- Outbox cần idempotency cho handler để tránh duplicate side effect khi retry

---

## UI/UX Notification

**Cấp độ:** Dễ

### [PRIORITY: LOW] Thống nhất hệ thống thông báo người dùng

**Mục tiêu:** Gom 3 cơ chế thông báo đang chạy song song (Bootstrap toast server-side, Bootstrap alert ở Layout, SweetAlert2 client-side) thành 1 quy trình & 1 UI duy nhất. Thay UI bằng **Notyf** cho đẹp, nhẹ, có animation chuẩn.

**Vấn đề hiện tại:**
- Key TempData không khớp giữa Controller và `_Messages.cshtml` → toast biến mất ở `DeliveryNoteController`, `VendorDebtController`, `CustomerDebtController` (set `CustomerErrorMessage` thay vì key tương ứng controller).
- `ViewConstants` hardcode 14 cặp key cho từng module → vi phạm DRY, dễ copy-paste sai.
- 2 UI khác hẳn cho cùng mục đích: Bootstrap toast (server) vs SweetAlert2 (client).
- Bootstrap toast không tự ẩn (thiếu `data-bs-autohide` + `data-bs-delay`), `position-absolute` sai khi page scroll dài.
- `GlobalExceptionFilter` render alert dạng band ngang → kiểu UI thứ 3.

---

#### Phase 1 — Foundation (server-side abstraction)

- [x] Tạo `NotificationModel` (`Type`, `Message`, `Title?`, `DurationMs`) tại `NamEcommerce.Web.Contracts/Models/Common/`
- [x] Tạo enum `NotificationType { Success, Error, Warning, Info }`
- [x] Tạo `INotificationService` với `Success/Error/Warning/Info(message, title?)` + `ConsumeAll()` tại `NamEcommerce.Web/Services/Notifications/`
- [x] Implement `TempDataNotificationService` — lưu `List<NotificationModel>` JSON-serialized vào 1 key duy nhất `"Messages.Notifications"`
- [x] Đăng ký scoped trong DI (`Program.cs`)
- [x] Thêm helper `NotifySuccess(key)` / `NotifyError(key)` vào `BaseController` (tự gọi `_localizer`)
- [x] Tạo `JsonNotificationResult(Success, Message, NotificationType?, Data)` + extension `JsonOk/JsonError` cho controller

#### Phase 2 — Client-side (Notyf)

- [x] Cài Notyf — copy `notyf.min.js` + `notyf.min.css` vào `wwwroot/lib/notyf/` (CDN: `cdn.jsdelivr.net/npm/notyf@3`)
- [x] Đăng ký script + style trong `_Scripts.cshtml` và `_Styles.cshtml`
- [x] Tạo `wwwroot/js/notification-center.js` — wrap Notyf với 4 method `success/error/warning/info`, expose `window.NotificationCenter`
  - Cấu hình `position: { x: 'right', y: 'top' }`, `duration: 4000`, `dismissible: true`, `ripple: true`
  - Thêm types `warning` + `info` qua `notyf.options.types` (mặc định Notyf chỉ có success + error)
- [x] Tạo `Views/Shared/_Notifications.cshtml` — `@inject INotificationService` → render JSON các pending notification → JS chạy `NotificationCenter.show(...)` trong `DOMContentLoaded`
- [x] Thêm `<partial name="_Notifications" />` vào `_Layout.cshtml` *(giữ tạm `_Messages` đến khi xong Phase 3 để không vỡ UX)*
- [x] Bỏ block alert `errorMessage` trong `_Layout.cshtml` (line 53–59)
- [x] Alias `toast(...)` cũ trong `wwwroot/modules/modals.js` trỏ vào `NotificationCenter` để JS module hiện có không cần sửa cùng lúc
- [x] Tạo `wwwroot/modules/ajax-helper.js` — wrapper fetch tự đọc `JsonNotificationResult` và gọi `NotificationCenter.show(...)`

#### Phase 3 — Migrate Controller (theo từng module, mỗi commit 1 module)

- [x] Customer
- [x] Vendor
- [x] Category
- [x] Product
- [x] Warehouse
- [x] Inventory
- [x] PurchaseOrder
- [x] Order
- [x] GoodsReceipt
- [x] DeliveryNote ← **fix bug key sai** (đang dùng `CustomerErrorMessage`)
- [x] CustomerDebt ← **fix bug key sai**
- [x] VendorDebt ← **fix bug key sai**
- [x] UnitMeasurement
- [x] Expense *(không có TempData notification — bỏ qua)*

Mỗi controller: thay `TempData[ViewConstants.XxxSuccessMessage] = ...` bằng `NotifySuccess(...)` / `NotifyError(...)`.

#### Phase 4 — Cleanup

- [x] Refactor `GlobalExceptionFilter` dùng `INotificationService.Error(...)` thay vì set `TempData[GlobalErrorMessage]` trực tiếp
- [x] Xoá toàn bộ `XxxSuccessMessage` / `XxxErrorMessage` trong `ViewConstants.cs`
- [x] Xoá `Views/Shared/_Messages.cshtml` *(scheduled task không thể xoá file — đã thay nội dung bằng comment deprecated, Tuấn xoá file thủ công khi merge)*
- [x] Xoá block render TempData toast trong các view List (`Customer/List.cshtml`, `Vendor/List.cshtml`, `Warehouse/List.cshtml`, `Category/List.cshtml`, ...)
- [x] Migrate JS module sang `apiPost` helper (`order.details.js`, `CreatePurchaseOrderController.js`, `OrderController.js`) ✅ DONE 2026-04-26

---

**Files chính cần đụng:**
- `NamEcommerce.Web.Contracts/Models/Common/NotificationModel.cs` — file mới
- `NamEcommerce.Web/Services/Notifications/INotificationService.cs` + `TempDataNotificationService.cs` — file mới
- `NamEcommerce.Web/Controllers/BaseController.cs` — thêm helper `NotifySuccess` / `NotifyError`
- `NamEcommerce.Web/Constants/ViewConstants.cs` — xoá hằng số `XxxSuccessMessage` / `XxxErrorMessage`
- `NamEcommerce.Web/Mvc/Filters/GlobalExceptionFilter.cs` — refactor dùng `INotificationService`
- `NamEcommerce.Web/Views/Shared/_Layout.cshtml` — thay partial, bỏ alert block
- `NamEcommerce.Web/Views/Shared/_Notifications.cshtml` — file mới (thay `_Messages.cshtml`)
- `NamEcommerce.Web/Views/Shared/_Scripts.cshtml` + `_Styles.cshtml` — đăng ký Notyf
- `NamEcommerce.Web/wwwroot/lib/notyf/` — thư mục thư viện mới
- `NamEcommerce.Web/wwwroot/js/notification-center.js` — file mới
- `NamEcommerce.Web/wwwroot/modules/modals.js` — alias `toast` cũ
- `NamEcommerce.Web/wwwroot/modules/ajax-helper.js` — file mới (tuỳ chọn)
- `NamEcommerce.Web/Controllers/*Controller.cs` — refactor theo Phase 3

**Phụ thuộc:**
- Phase 2 phụ thuộc Phase 1 (cần `INotificationService`)
- Phase 3 phụ thuộc Phase 1 + 2 (cần helper `NotifySuccess` và partial `_Notifications`)
- Phase 4 phải làm cuối cùng (xoá legacy chỉ khi tất cả controller đã migrate)

**Rủi ro cần lưu ý:**
- Notyf không có sẵn type `warning` / `info` → phải tự đăng ký qua `notyf.options.types` với màu + icon riêng
- Khi reload trang sau redirect, JSON serialize TempData phải dùng cùng config với client (camelCase / PascalCase) — cần test thử
- Một số JS module hiện có gọi `toast(...)` với 3 đối số `(title, body, type)` — alias phải map đúng thứ tự sang Notyf API
- Sau khi xoá hằng số trong `ViewConstants`, build sẽ fail ở các view List còn dùng → phải migrate Phase 3 trước khi Phase 4
