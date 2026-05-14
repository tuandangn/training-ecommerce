# TodoList — VLXD Tuấn Khôi / NamEcommerce

> File theo dõi các hạng mục còn pending. Các mục đã làm xong đã được xóa để tránh nhiễu.

---

### Quy tắc bắt buộc

- **Branch hiện hành**: `dev-assistant` — AI checkout/tạo trước khi sửa code.
- **Unit test**: KHÔNG viết unit test mới, KHÔNG sửa code trong bất kỳ project `*.Test` nào.
- **Migration**: AI KHÔNG tự chạy migration — báo Tuấn tự chạy.
- **Skills**: AI đọc skill `namcommerce` trước khi viết code domain.
- **Comments**: chỉ viết comment khi thật cần thiết (giải thích WHY khi không hiển nhiên).

---

## 🆕 Dashboard mới — Trang Home/Index (kick-off 2026-05-14)

> Thay thế hoàn toàn 8 card điều hướng cũ ở `Views/Home/Index.cshtml` bằng dashboard số liệu vận hành.
> Library chốt: **Chart.js**. "Đơn cần xử lý" = Order chưa giao xong / PO chưa nhập đủ.
> Mức tồn thấp = `QuantityOnHand ≤ ReorderLevel` (cột đã có sẵn trên `InventoryStock` nhưng chưa có UI/Manager để set).

### Phase A — Bổ sung chức năng set `ReorderLevel` / `MaxStockLevel` (prerequisite)

- [x] ~~**A1** — Entity method `InventoryStock.SetStockLevels`~~ **bỏ** — match existing style (Manager set property trực tiếp).
- [x] **A2** — Domain DTO `SetStockLevelsDto` + `Verify()` trong `Domain.Shared/Dtos/Inventory/InventoryDtos.cs`. ✅ 2026-05-14
- [x] **A3** — `IInventoryStockManager.SetStockLevelsAsync(SetStockLevelsDto dto)` + impl trong `InventoryStockManager.cs`. ✅ 2026-05-14
- [x] **A4** — AppDto `SetStockLevelsAppDto` + `SetStockLevelsResultAppDto` + `Validate()` trong `Application.Contracts/Dtos/Inventory/InventoryAppDtos.cs`. ✅ 2026-05-14
- [x] **A5** — `IInventoryAppService.SetStockLevelsAsync(...)` + impl trong `InventoryAppService.cs`. ✅ 2026-05-14
- [x] **A6** — Web Command `SetStockLevelsCommand` + Result model + Validator + `SetStockLevelsHandler` (MediatR). ✅ 2026-05-14
- [x] **A7** — UI: nút "Cấu hình mức tồn" + modal trong `Views/Inventory/StockList.cshtml`, action POST `InventoryController.SetStockLevels` return JSON. ✅ 2026-05-14
- [ ] **A8** — Verify build + smoke test: Tuấn chạy `dotnet build`, mở Inventory → modal → save → reload kiểm tra giá trị lưu đúng.

**File ảnh hưởng Phase A:**
- Domain.Shared: `Dtos/Inventory/InventoryDtos.cs` (thêm `SetStockLevelsDto`, +2 field `ReorderLevel`/`MaxStockLevel` cho `InventoryStockDto`); `Services/Inventory/IInventoryStockManager.cs`.
- Domain.Services: `Inventory/InventoryStockManager.cs` (impl `SetStockLevelsAsync` + select 2 field trong 2 query).
- Application.Contracts: `Dtos/Inventory/InventoryAppDtos.cs` (+`SetStockLevelsAppDto`/`Result`, +2 field cho `InventoryStockAppDto`); `Inventory/IInventoryAppService.cs`.
- Application.Services: `Inventory/InventoryAppService.cs` (impl + map 2 field).
- Web.Contracts: `Commands/Models/Inventory/SetStockLevelsCommand.cs` (new); `Models/Inventory/SetStockLevelsResultModel.cs` (new); `Models/Inventory/InventoryModels.cs` (+2 field).
- Web.Framework: `Commands/Handlers/Inventory/SetStockLevelsHandler.cs` (new); `Queries/Handlers/Inventory/GetInventoryStockListHandler.cs` (map 2 field).
- Web: `Models/Inventory/SetStockLevelsModel.cs` (new); `Controllers/InventoryController.cs` (action POST mới); `Views/Inventory/StockList.cshtml` (cột + modal + JS).

### Phase B — Cài Chart.js

- [x] **B1** — Tải Chart.js vào `wwwroot/lib/chartjs/chart.umd.min.js` (offline, ổn định khi mất mạng). ✅ 2026-05-14
- [x] **B2** — Include vào `_Scripts.cshtml` sau jQuery, trước script page-specific. ✅ 2026-05-14
- [x] **B3** — Verify: `typeof Chart === 'function'` bằng Node VM; F12 kiểm lại khi chạy UI Phase E. ✅ 2026-05-14

### Phase C — Application Layer Dashboard

- [x] **C1** — DTOs trong `Application.Contracts/Dtos/Dashboard/DashboardAppDtos.cs`: ✅ 2026-05-14
  - `DashboardAppDto` (root, gom hết section).
  - `SalesSummaryAppDto` { TodayRevenue, MonthRevenue, QuarterRevenue, YearRevenue, RevenueTrendUtc[] }.
  - `ProfitSummaryAppDto` { TodayProfit, MonthProfit, QuarterProfit, YearProfit }.
  - `PendingOrderAppDto`, `PendingPurchaseOrderAppDto` (5 dòng mỗi loại).
  - `TopCustomerDebtAppDto`, `TopVendorDebtAppDto` (5 dòng mỗi loại).
  - `LowStockProductAppDto` (10 dòng).
- [x] **C2** — Interface `IDashboardAppService.GetDashboardDataAsync()` trong `Application.Contracts/Dashboard/`. ✅ 2026-05-14
- [x] **C3** — Impl `DashboardAppService` trong `Application.Services/Dashboard/`: ✅ 2026-05-14
  - Tái dùng `IFinancialReportAppService.GetProfitLossSummaryAsync` cho 4 mốc thời gian (today / month / quarter / year).
  - `IEntityDataReader<Order>` + `OrderItem` query đơn `Status != Cancelled` AND tồn tại `OrderItem` với `IsDelivered = false`.
  - `IEntityDataReader<PurchaseOrder>` + `PurchaseOrderItem` query đơn `Status != Cancelled && Status != Completed` AND tồn tại item `QuantityReceived < QuantityOrdered`.
  - `IEntityDataReader<CustomerDebt>` / `VendorDebt` sort `TotalRemainingAmount` desc, top 5.
  - `IEntityDataReader<InventoryStock>` filter `ReorderLevel > 0 && QuantityOnHand <= ReorderLevel`, sort tăng dần, top 10.
- [x] **C4** — Extension methods `ToDto()` trong `Application.Services/Extensions/DashboardExtensions.cs`. ✅ 2026-05-14
- [x] **C5** — Register DI: `IDashboardAppService` → `DashboardAppService` trong module DI Application. ✅ 2026-05-14

### Phase D — Presentation Layer Dashboard

- [x] **D1** — `GetDashboardQuery : IRequest<DashboardModel>` trong `Web.Contracts/Queries/Models/Dashboard/`. ✅ 2026-05-14
- [x] **D2** — Models trong `Web.Contracts/Models/Dashboard/` (theo rule presentation, DateTime KHÔNG có hậu tố `Utc`, page index dùng 1-based): ✅ 2026-05-14
  - `DashboardModel` (root) + sub-models: `SalesSummaryModel`, `ProfitSummaryModel`, `PendingOrderModel`, `PendingPurchaseOrderModel`, `TopCustomerDebtModel`, `TopVendorDebtModel`, `LowStockProductModel`.
- [x] **D3** — `GetDashboardHandler` trong `Web.Framework/Queries/Handlers/Dashboard/`: gọi `IDashboardAppService` → map sang Model (`DateTimeHelper.ToLocalTime()`). ✅ 2026-05-14
- [x] **D4** — `IDashboardModelFactory.PrepareDashboardModelAsync()` + impl trong `Web/Services/Dashboard/`. ✅ 2026-05-14
- [x] **D5** — Update `Web/Controllers/HomeController.cs`: inject `IDashboardModelFactory`, `Index()` async, return model. ✅ 2026-05-14
- [x] **D6** — Rewrite hoàn toàn `Web/Views/Home/Index.cshtml`: ✅ 2026-05-14
  - Row 1: 4 KPI card doanh số (ngày / tháng / quý / năm).
  - Row 2: 4 KPI card lợi nhuận (ngày / tháng / quý / năm).
  - Row 3: line chart doanh thu 30 ngày + doughnut cơ cấu lợi nhuận tháng.
  - Row 4: 2 bảng (5 đơn bán + 5 đơn nhập cần xử lý).
  - Row 5: 2 bảng (top 5 KH + top 5 NCC công nợ).
  - Row 6: bảng 10 hàng tồn thấp với progress bar `QuantityOnHand / ReorderLevel`.
- [x] **D7** — `wwwroot/js/dashboard.js` — khởi tạo Chart.js (nhận data qua data-attribute hoặc inline JSON). ✅ 2026-05-14
- [x] **D8** — Register DI: `IDashboardModelFactory` → `DashboardModelFactory` trong module DI Web. ✅ 2026-05-14

### Phase E — Verify cuối

- [ ] **E1** — `dotnet build` toàn solution success, 0 error.
- [ ] **E2** — Chạy app → `/` → 7 section render đầy đủ, F12 console không error.
- [ ] **E3** — Set thử ReorderLevel cho vài InventoryStock → reload → section "hàng tồn thấp" có dữ liệu đúng.
- [ ] **E4** — Kiểm tra responsive (mobile / tablet) — layout không vỡ.

### Lưu ý phạm vi

- **Không cần migration**: `ReorderLevel` / `MaxStockLevel` đã có cột sẵn.
- Tái dùng tối đa AppService có sẵn (`IFinancialReportAppService`), tránh thêm method trên 5 AppService khác chỉ để phục vụ dashboard.

---

## Pending — Migration / Smoke Test (chờ Tuấn)

- [ ] **Tuấn chạy migration cho các thay đổi đã implement**
  - `CustomerReturn.DeliveryNoteId` chuyển sang required.
  - Thêm bảng/cấu hình `ProductReservationLedger` append-only cho global reservation.
  - Backfill ledger cho các Order đang còn giữ hàng bằng `ProductReservationReason.MigrationBackfill`.
  - AI không chạy `Add-Migration` / `Update-Database`.

- [ ] **Smoke test lại P2/P3/P4 sau migration**
  - Tạo Order có đủ tồn global → Order được tạo thành công.
  - Tạo Order vượt tồn global → bị chặn bằng `Error.InsufficientStock`.
  - Tạo DeliveryNote từ Order → Confirm → global reservation giảm, per-warehouse reservation tăng.
  - Cancel DeliveryNote đã Confirmed → per-warehouse reservation giảm, global reservation tăng lại.
  - Mark Delivered DeliveryNote → tồn kho bị trừ, công nợ khách hàng được sinh.
  - Verify thủ công 6 manual-test còn pending trong P4.1 → P4.7.

---

## P4.7 — Append-only per-order reservation ledger (còn pending)

> Code đã viết, chờ migration + manual verify.

- [ ] Migration do Tuấn chạy sau khi code mapping sẵn sàng; cần backfill reservation còn lại theo từng Order đang active.
- [ ] Manual verify: 2 order cùng product, confirm/cancel DN của order A không làm giảm reservation của order B.

**Ghi chú 2026-05-14:** Tuấn đã chọn append-only ledger thay vì bucket `(ProductId, OrderId, ReservedQuantity)`. AI không chạy migration; Tuấn chạy migration sau khi code/mapping sẵn sàng.

---

## Ghi chú thực hiện

- Không viết / sửa unit test theo rule hiện tại. Mỗi mục cần có build verify và manual smoke test.
- Nếu build bin Web bị IIS Express lock, dùng output riêng: `-p:OutDir=C:\tmp\NamEcommerceP4Build\`.
- Mỗi bước nên làm xong → cập nhật checkbox trong file này → báo Tuấn trước khi sang bước kế tiếp.
