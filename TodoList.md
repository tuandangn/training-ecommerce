# TodoList — VLXD Tuấn Khôi / NamEcommerce

> File theo dõi các hạng mục còn pending. Các mục đã làm xong đã được xóa để tránh nhiễu.

---

### Quy tắc bắt buộc

- **Unit test**: KHÔNG viết unit test mới, KHÔNG sửa code trong bất kỳ project `*.Test` nào.
- **Migration**: AI KHÔNG tự chạy migration — báo Tuấn tự chạy.
- **Skills**: AI đọc skill `namcommerce` trước khi viết code domain.
- **Comments**: chỉ viết comment khi thật cần thiết.

---

## Pending — Migration / Smoke Test

- [ ] **Tuấn chạy migration cho các thay đổi đã implement**
  - `CustomerReturn.DeliveryNoteId` chuyển sang required.
  - Thêm bảng/cấu hình `ProductReservation`.
  - AI không chạy `Add-Migration` / `Update-Database`.

- [ ] **Smoke test lại P2/P3 sau migration**
  - Tạo Order có đủ tồn global -> Order được tạo thành công.
  - Tạo Order vượt tồn global -> bị chặn bằng `Error.InsufficientStock`.
  - Tạo DeliveryNote từ Order -> Confirm -> global reservation giảm, per-warehouse reservation tăng.
  - Cancel DeliveryNote đã Confirmed -> per-warehouse reservation giảm, global reservation tăng lại.
  - Mark Delivered DeliveryNote -> tồn kho bị trừ, công nợ khách hàng được sinh.

---

## P4 — Sales Workflow Hardening Plan

> Mục tiêu: workflow bán hàng không được giao thiếu, khóa đơn sớm, bỏ qua phiếu xuất, hoặc làm lệch reservation/tồn kho/công nợ.
> Thứ tự làm: chặn đường sai nghiêm trọng trước, sau đó đưa invariant xuống domain, cuối cùng mới xử lý atomic/concurrency.

### P4.1 — Bỏ luồng "Đã giao" trực tiếp trên Preparation ✅ 2026-05-13

**Vấn đề:** Màn Chuẩn bị hàng có nút `Đã giao` mark `OrderItem` delivered trực tiếp, bỏ qua `DeliveryNote`, nên không trừ kho, không snapshot COGS, không sinh công nợ.

**Files dự kiến:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Preparation/List.Customer.cshtml`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Preparation/List.Product.cshtml`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Preparation/List.cshtml`
- Modify/Delete nếu không còn dùng: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/PreparationController.cs`
- Modify/Delete nếu không còn dùng: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/Preparation/MarkOrderItemDeliveredHandler.cs`
- Modify/Delete nếu không còn dùng: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/Preparation/MarkOrderItemDeliveredCommand.cs`

**Steps:**
- [x] Xóa/hide nút `btnMarkDelivered` ở 2 partial Preparation; user chỉ còn tạo DeliveryNote.
- [x] Xóa modal upload proof và JS call `Preparation/MarkDelivered` trong `List.cshtml` nếu không còn reference.
- [x] Chặn endpoint `PreparationController.MarkDelivered` hoặc xóa hẳn flow command/handler nếu không còn nơi gọi.
- [x] Search `MarkOrderItemDeliveredCommand`, `MarkOrderItemDeliveredAsync`, `btnMarkDelivered` để đảm bảo không còn UI path đi vòng qua DeliveryNote.
- [x] Build verify: `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj -p:OutDir=C:\tmp\NamEcommerceP4Build\`
- [ ] Manual verify: vào Preparation -> chỉ tạo được phiếu xuất, không còn nút đánh dấu giao trực tiếp.

### P4.2 — Sửa partial delivery: chỉ lock Order khi đã giao đủ số lượng ✅ 2026-05-13

**Vấn đề:** `DeliveryNoteManager.MarkDeliveredAsync` đang mark `OrderItem.IsDelivered = true` nếu item có trong phiếu xuất, kể cả giao một phần. `Order.TryAutoLock()` có thể khóa đơn sớm.

**Files dự kiến:**
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryNoteManager.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Orders/Order.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Orders/OrderItem.cs`
- Modify nếu cần: `NamEcommerce/Application/NamEcommerce.Application.Services/Events/Orders/OrderReservationEventHandlers.cs`
- Modify nếu cần: `NamEcommerce/Presentation/NamEcommerce.Web/Models/Orders/OrderDetailsModel.cs`

**Steps:**
- [x] Định nghĩa rule: OrderItem chỉ considered delivered khi tổng quantity của DeliveryNote `Delivered` cho item đó >= `OrderItem.Quantity`.
- [x] Trong `DeliveryNoteManager.MarkDeliveredAsync`, sau khi DN delivered, tính delivered quantity theo từng `OrderItemId` bằng tất cả DeliveryNote status `Delivered`.
- [x] Chỉ gọi `order.MarkOrderItemDelivered(...)` cho item đã giao đủ; không mark delivered cho giao một phần.
- [x] `TryAutoLock()` chỉ được chạy sau khi tất cả item đã giao đủ theo delivered quantity, hoặc sau khi các item đã được mark delivered đúng rule mới.
- [x] Kiểm tra `OrderLockedEventHandler`: remaining global release phải dựa trên moved quantity của DN không Draft/Cancelled; nếu item giao một phần thì không release phần còn lại.
- [x] Build verify bằng command P4.1.
- [ ] Manual verify: Order 10 cái -> DN delivered 3 cái -> Order chưa lock, item chưa hiện đã giao đủ; delivered thêm 7 cái -> Order mới auto-lock.

### P4.3 — Đưa validation "không xuất vượt remaining" xuống Domain ✅ 2026-05-13

**Vấn đề:** UI có tính remaining, nhưng `DeliveryNoteManager.CreateFromOrderAsync` chỉ check OrderItem tồn tại. Form/request bất thường có thể tạo Draft DN vượt số lượng còn lại.

**Files dự kiến:**
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryNoteManager.cs`
- Add/Modify exception nếu cần: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Exceptions/DeliveryNotes/*`
- Modify localization nếu thêm error key: `NamEcommerce/Presentation/NamEcommerce.Web/Resources/SharedResource.resx`
- Modify localization nếu thêm error key: `NamEcommerce/Presentation/NamEcommerce.Web/Resources/SharedResource.vi-VN.resx`

**Steps:**
- [x] Trước khi add item vào DeliveryNote, lấy tổng quantity của các DeliveryNote cùng OrderItemId với status `!= Cancelled`.
- [x] Tính `remaining = orderItem.Quantity - alreadyInDeliveryNotes`.
- [x] Nếu `itemDto.Quantity > remaining`, throw domain exception rõ ràng, ví dụ `Error.QuantityExceedsRemaining`.
- [x] Giữ validation trong UI/Controller hiện có, nhưng domain là lớp chặn cuối.
- [x] Build verify bằng command P4.1.
- [ ] Manual verify: sửa request tạo DN vượt remaining -> bị reject, không tạo Draft DN.

### P4.4 — Order delete/cancel không được để lại Draft DeliveryNote mồ côi ✅ 2026-05-13

**Vấn đề:** `OrderManager.DeleteOrderAsync` / `CancelOrderAsync` đang bỏ qua Draft DeliveryNote. Có thể xóa/cancel Order trong khi Draft DN vẫn trỏ về Order đó.

**Files dự kiến:**
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Orders/OrderManager.cs`
- Modify nếu cần expose cancel: `NamEcommerce/Application/NamEcommerce.Application.Services/Orders/OrderAppService.cs`
- Modify nếu cần UI: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/OrderController.cs`

**Steps:**
- [x] Chốt rule: Order có bất kỳ DeliveryNote status `!= Cancelled` thì không delete.
- [x] Áp dụng cùng rule cho `CancelOrderAsync`, hoặc auto-cancel Draft DN nếu Tuấn muốn cancel Order vẫn được phép.
- [x] Đảm bảo delete/cancel release global reservation đúng một lần, không release phần đã move sang per-warehouse.
- [x] Build verify bằng command P4.1.
- [ ] Manual verify: Order có Draft DN -> delete/cancel bị chặn hoặc Draft DN bị auto-cancel theo rule đã chốt.

### P4.5 — Tách dispatch reserved và dispatch non-reserved ✅ 2026-05-13

**Vấn đề:** `InventoryStockManager.DispatchStockAsync` check `QuantityOnHand`, sau đó tự trừ `QuantityReserved` nếu có. Luồng không reserve trước có thể ăn vào hàng đang giữ cho đơn bán.

**Files dự kiến:**
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Inventory/InventoryStockManager.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Inventory/IInventoryStockManager.cs`
- Modify call sites: `NamEcommerce/Application/NamEcommerce.Application.Services/Events/DeliveryNotes/DeliveryNoteDeliveredStockHandler.cs`
- Review call sites: `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryNoteManager.cs`

**Steps:**
- [x] Chốt API: dispatch từ stock đã reserve cho sales DN khác với dispatch non-reserved.
- [x] Sales DeliveryNote delivered: trừ `QuantityOnHand` và release reserved allocation của chính DN.
- [x] VendorReturn / non-order dispatch: check `QuantityAvailable`, không được trừ vào `QuantityReserved` của đơn bán.
- [x] Nếu chưa có reservation reference-level, ít nhất không auto-trừ `QuantityReserved` cho non-order flow.
- [x] Build verify bằng command P4.1.
- [ ] Manual verify: warehouse có OnHand 10, Reserved 8 cho sales; vendor return 5 phải bị reject nếu available chỉ 2.

### P4.6 — Làm reservation transition atomic hơn ✅ 2026-05-13

**Vấn đề:** các bước release global -> reserve warehouse -> update DN đang là nhiều `SaveChanges`; nếu fail giữa chừng sẽ lệch data.

**Files dự kiến:**
- Review/Modify: `NamEcommerce/Infrastructure/NamEcommerce.Data/IDbContext.cs`
- Review/Modify: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/NamEcommerceEfDbContext.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryNoteManager.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Orders/OrderManager.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/Events/*`

**Steps:**
- [x] Map tất cả transition có side-effect nhiều aggregate: Order create/update/delete, DN confirm/cancel/deliver, debt create.
- [x] Chọn cách làm nhỏ nhất: transaction wrapper trong manager/application service, hoặc UnitOfWork gom `SaveChanges`.
- [x] Áp dụng trước cho `DeliveryNoteManager.ConfirmAsync` và `CancelAsync` vì đây là nơi move reservation.
- [x] Sau đó áp dụng cho `MarkDeliveredAsync` để DN status, stock dispatch, order delivered/lock, debt creation không lệch nhau.
- [x] Build verify bằng command P4.1.
- [ ] Manual verify: simulate exception giữa transition, data không bị release/reserve nửa vời với status cũ.

### P4.7 — Future: per-order reservation ledger (chưa implement — cần chốt schema)

**Vấn đề:** `ProductReservation.TotalReservedByOrder` chỉ aggregate theo product, không lưu `OrderId`. Đủ cho quick fix, nhưng khó audit và có thể release nhầm nếu data đã lệch.

**Files dự kiến:**
- Add/Modify entity: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Inventory/ProductReservation*.cs`
- Modify manager: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Inventory/ProductReservationManager.cs`
- Modify mapping: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Configurations/Inventory/*`
- Modify consumers: Order event handlers, `DeliveryNoteManager.ConfirmAsync`, `CancelAsync`, `OrderLockedEventHandler`

**Steps:**
- [ ] Tuấn chốt schema: mỗi row là `(ProductId, OrderId, ReservedQuantity)` hay bảng ledger append-only.
- [ ] Migration do Tuấn chạy sau khi code mapping sẵn sàng.
- [ ] `ReserveAsync/ReleaseAsync/AdjustAsync` phải cập nhật đúng order bucket.
- [ ] `GetGlobalAvailableForProductAsync` sum tất cả reservation buckets.
- [ ] DN Confirm release global của đúng `OrderId`, không chỉ check total product.
- [ ] Build verify bằng command P4.1.
- [ ] Manual verify: 2 order cùng product, confirm/cancel DN của order A không làm giảm reservation của order B.

**Ghi chú 2026-05-13:** P4.1-P4.6 đã giảm rủi ro workflow chính. P4.7 là đổi schema/migration lớn hơn, nên chưa implement cho tới khi Tuấn chốt kiểu ledger.

---

## Ghi chú thực hiện

- Không viết/sửa unit test theo rule hiện tại. Mỗi mục cần có build verify và manual smoke test.
- Nếu build bin Web bị IIS Express lock, dùng output riêng: `-p:OutDir=C:\tmp\NamEcommerceP4Build\`.
- Mỗi bước nên làm xong -> cập nhật checkbox trong file này -> báo Tuấn trước khi sang bước kế tiếp.
