# KẾ HOẠCH TRIỂN KHAI — CÔNG NỢ NHÀ CUNG CẤP

> **Module**: `Debts` (mở rộng module hiện có)
> **Ngày lập kế hoạch**: 2026-04-21
> **Phạm vi**: Thêm công nợ phải trả nhà cung cấp (NCC) vào hệ thống VLXD Tuấn Khôi

---

## 1. Phân tích nghiệp vụ

### 1.1 Luồng nghiệp vụ

```
PurchaseOrder (nhập hàng từ NCC)
   └─> Khi Status = Completed (hoặc Receiving)
        └─> Tạo VendorDebt (công nợ phải trả NCC)
              └─> Ghi nhận VendorPayment (chi tiền cho NCC)
                    ├─ Thanh toán cho 1 phiếu cụ thể
                    ├─ Thanh toán linh động (FIFO)
                    └─ Tiền ứng trước (Advance Payment)
```

### 1.2 Khác biệt với CustomerDebt (đã có)

| Điểm | CustomerDebt (đã có) | VendorDebt (sẽ làm) |
|---|---|---|
| Nguồn phát sinh | `DeliveryNote` (xuất hàng) | `PurchaseOrder` (nhập hàng) |
| Đối tác | `Customer` (khách hàng) | `Vendor` (nhà cung cấp) |
| Chiều tiền | Thu tiền → ta nhận | Chi tiền → ta trả |
| Tiền cọc | Đặt cọc trước khi xuất | Ứng trước cho NCC |
| Gắn với | `OrderId`, `DeliveryNoteId` | `PurchaseOrderId` |

---

## 2. Naming conventions

| Loại | Tên | Ghi chú |
|---|---|---|
| Entity công nợ | `VendorDebt` | Đồng bộ với entity `Vendor` có sẵn |
| Entity thanh toán | `VendorPayment` | Phiếu chi tiền cho NCC |
| Manager | `IVendorDebtManager` / `VendorDebtManager` | |
| AppService | `IVendorDebtAppService` / `VendorDebtAppService` | |
| Module namespace | `NamEcommerce.Domain.Entities.Debts` (giữ chung module Debts) | |
| Mã phiếu công nợ | `CNNCC-yyyyMMdd-###` | Công Nợ Nhà Cung Cấp |
| Mã phiếu chi | `PCNCC-yyyyMMdd-###` | Phiếu Chi Nhà Cung Cấp |
| Enum PaymentType bổ sung | `AdvancePayment`, `VendorDebtPayment` | Ứng trước NCC / Trả nợ NCC |

---

## 3. Checklist files cần tạo & sửa

### 3.1 Domain Layer

#### Entities — `NamEcommerce.Domain/Entities/Debts/`
- [ ] `VendorDebt.cs` — `sealed record : AppAggregateEntity`
  - Constructor `internal`
  - Properties: `Code`, `VendorId`, `VendorName`, `VendorPhone`, `VendorAddress`, `PurchaseOrderId`, `PurchaseOrderCode`, `TotalAmount`, `PaidAmount`, `RemainingAmount`, `Status`, `DueDateUtc`, `CreatedByUserId`, `CreatedOnUtc`, `UpdatedOnUtc`
  - Methods `internal`: `ApplyPayment(decimal amount)`, `MarkAsPaid()`, `ChangeDueDate(DateTime?)`
- [ ] `VendorPayment.cs` — tương tự `CustomerPayment`
  - Properties gắn: `VendorDebtId?`, `PurchaseOrderId?`, `PurchaseOrderCode?`
  - Method `internal MarkAsApplied()`

#### Enums — `NamEcommerce.Domain.Shared/Enums/Debts/`
- [ ] Dùng lại `DebtStatus` (Outstanding / PartiallyPaid / Paid / Overdue) — giá trị giống nhau
- [ ] Mở rộng `PaymentType` trong `Enums/Orders/PaymentMethod.cs`:
  - Thêm `AdvancePayment` (ứng trước NCC)
  - Thêm `VendorDebtPayment` (trả nợ NCC)
- [ ] `PaymentMethod` (Cash / BankTransfer...) dùng lại nguyên

#### Exceptions — `NamEcommerce.Domain.Shared/Exceptions/Debts/`
- [ ] `VendorDebtNotFoundException`
- [ ] `VendorDebtAlreadyExistsForPurchaseOrderException`
- [ ] `VendorPaymentExceedsRemainingException`

#### Domain DTOs — `NamEcommerce.Domain.Shared/Dtos/Debts/VendorDebtDtos.cs`
- [ ] `CreateVendorDebtDto` (có `Verify()`)
- [ ] `VendorDebtDto`
- [ ] `VendorDebtSummaryDto` (gom theo VendorId — cho trang List)
- [ ] `VendorDebtsByVendorDto` (trang Details)
- [ ] `CreateVendorPaymentDto` (có `Verify()`)
- [ ] `VendorPaymentDto`

#### Manager Interface — `NamEcommerce.Domain.Shared/Services/Debts/IVendorDebtManager.cs`
Methods cần có:
- [ ] `CreateDebtFromPurchaseOrderAsync(CreateVendorDebtDto)` — idempotent
- [ ] `RecordPaymentAsync(CreateVendorPaymentDto)` — trả nợ 1 phiếu cụ thể
- [ ] `RecordFlexiblePaymentForVendorAsync(CreateVendorPaymentDto)` — FIFO
- [ ] `RecordAdvancePaymentAsync(CreateVendorPaymentDto)` — ứng trước
- [ ] `GetDebtByIdAsync(Guid)`
- [ ] `GetPaymentByIdAsync(Guid)`
- [ ] `GetVendorDebtSummaryAsync(Guid vendorId)`
- [ ] `GetVendorsWithDebtsAsync(keywords, pageIndex, pageSize)` — trang List
- [ ] `GetDebtsByVendorIdAsync(Guid vendorId)` — trang Details
- [ ] `GetDebtsAsync(vendorId?, keywords, pageIndex, pageSize)`
- [ ] `GetPaymentsAsync(vendorId?, pageIndex, pageSize)`

#### Manager Implementation — `NamEcommerce.Domain.Services/Debts/VendorDebtManager.cs`
- [ ] Copy pattern từ `CustomerDebtManager.cs`
- [ ] Code generators: `GenerateDebtCodeAsync()`, `GeneratePaymentCodeAsync()`
- [ ] Logic auto-apply `AdvancePayment` chưa dùng khi tạo debt (giống auto-apply Deposits)
- [ ] FIFO allocation khi thanh toán linh động
- [ ] Publish events: `.EntityCreated()`, `.EntityUpdated()` qua `IEventPublisher`

#### Extensions — `NamEcommerce.Domain.Services/Extensions/VendorDebtExtensions.cs`
- [ ] `entity.ToDto()` cho `VendorDebt`
- [ ] `entity.ToDto()` cho `VendorPayment`

#### AssemblyAccessibility
- [ ] Kiểm tra `NamEcommerce.Domain/Accessibility/AssemblyAccessibility.cs` đã có `[assembly: InternalsVisibleTo("...Domain.Services")]`

---

### 3.2 Application Layer

#### App DTOs — `NamEcommerce.Application.Contracts/Dtos/Debts/VendorDebtAppDtos.cs`
> **Lưu ý**: AppDto dùng hậu tố `Utc` cho DateTime (`PaidOnUtc`, `DueDateUtc`, `CreatedOnUtc`)
- [ ] `CreateVendorDebtAppDto` + `Validate()` (không throw)
- [ ] `VendorDebtAppDto`
- [ ] `VendorDebtSummaryAppDto`
- [ ] `VendorDebtsByVendorAppDto`
- [ ] `CreateVendorPaymentAppDto` + `Validate()`
- [ ] `VendorPaymentAppDto`
- [ ] Result types: `CreateVendorDebtResultAppDto`, `RecordVendorPaymentResultAppDto` (có `Success`, `ErrorMessage`, optional `CreatedId`)

#### AppService Interface — `NamEcommerce.Application.Contracts/Debts/IVendorDebtAppService.cs`
- [ ] Mirror của `ICustomerDebtAppService`

#### AppService Implementation — `NamEcommerce.Application.Services/Debts/VendorDebtAppService.cs`
- [ ] Inject `IVendorDebtManager`
- [ ] Return result objects với `Success`/`ErrorMessage`, không throw ra ngoài
- [ ] Validate trước khi gọi Manager

#### Extensions — `NamEcommerce.Application.Services/Extensions/VendorDebtExtensions.cs`
- [ ] `domainDto.ToDto()` convert `VendorDebtDto` → `VendorDebtAppDto`
- [ ] Tương tự cho `VendorPaymentDto` → `VendorPaymentAppDto`
- [ ] Nhớ convert DateTime sang UTC bằng `DateTimeHelper.ToUniversalTime()`

---

### 3.3 Presentation Layer

#### Web Contracts — Models (`NamEcommerce.Web.Contracts/Models/Debts/VendorDebtModels.cs`)
> **Lưu ý**: Presentation Model KHÔNG có hậu tố `Utc`; dùng `PaidOn`, `DueDate`, `CreatedOn` (local time)
> `PageNumber` (1-based) ở presentation, convert sang `PageIndex` (0-based) khi gọi xuống App layer
- [ ] `VendorDebtListModel` (trang List)
- [ ] `VendorDebtVendorSummaryModel` (1 dòng trong bảng NCC có công nợ)
- [ ] `VendorDebtDetailsModel` (trang Details theo NCC)
- [ ] `VendorDebtItemModel`
- [ ] `VendorPaymentListItemModel`
- [ ] `VendorPaymentReceiptModel` (in phiếu chi)
- [ ] `RecordVendorPaymentModel` (form submit)
- [ ] `VendorDebtSearchModel`

#### Web Contracts — Commands (`NamEcommerce.Web.Contracts/Commands/Models/Debts/VendorDebtCommands.cs`)
- [ ] `RecordVendorPaymentCommand` (trả nợ 1 phiếu)
- [ ] `RecordFlexibleVendorPaymentCommand` (FIFO)
- [ ] `RecordVendorAdvancePaymentCommand` (ứng trước)

#### Web Contracts — Queries (`NamEcommerce.Web.Contracts/Queries/Models/Debts/VendorDebtQueries.cs`)
- [ ] `GetVendorDebtListQuery` (danh sách NCC có công nợ)
- [ ] `GetVendorDebtDetailsQuery(vendorId)` (chi tiết theo NCC)
- [ ] `GetVendorPaymentReceiptQuery(paymentId)` (in phiếu chi)

#### Web Framework — Command Handlers (`NamEcommerce.Web.Framework/Commands/Handlers/Debts/`)
- [ ] `RecordVendorPaymentHandler.cs`
- [ ] `RecordFlexibleVendorPaymentHandler.cs`
- [ ] `RecordVendorAdvancePaymentHandler.cs`

#### Web Framework — Query Handlers (`NamEcommerce.Web.Framework/Queries/Handlers/Debts/`)
- [ ] `GetVendorDebtListHandler.cs`
- [ ] `GetVendorDebtDetailsHandler.cs`
- [ ] `GetVendorPaymentReceiptHandler.cs`

> **Lưu ý**: Handler CHỈ return `Model` từ `Web.Contracts`, KHÔNG return `AppDto`

#### Web — ModelFactory (`NamEcommerce.Web/Services/Debts/`)
- [ ] Tạo folder `Debts` mới
- [ ] `IVendorDebtModelFactory.cs`
- [ ] `VendorDebtModelFactory.cs` — inject `IMediator`, `AppConfig`
  - `PrepareVendorDebtListModelAsync(searchModel)`
  - `PrepareVendorDebtDetailsModelAsync(vendorId)`
  - `PrepareVendorPaymentReceiptModelAsync(paymentId)`

#### Web — Controller (`NamEcommerce.Web/Controllers/VendorDebtController.cs`)
- [ ] Kế thừa `BaseAuthorizedController`
- [ ] Chỉ inject `IMediator` + `IVendorDebtModelFactory`
- [ ] Actions:
  - `Index()` — List NCC có nợ
  - `Details(vendorId)` — chi tiết công nợ 1 NCC
  - `RecordPayment(RecordVendorPaymentViewModel)` — POST trả nợ phiếu cụ thể
  - `RecordFlexiblePayment(...)` — POST FIFO
  - `RecordAdvance(...)` — POST ứng trước
  - `Receipt(paymentId)` — in phiếu chi

#### Web — ViewModels + Validators (`NamEcommerce.Web/Models/Debts/`)
- [ ] `RecordVendorPaymentViewModel`
- [ ] `RecordVendorPaymentValidator` (FluentValidation)

#### Web — Views (`NamEcommerce.Web/Views/VendorDebt/`)
- [ ] `Index.cshtml` (list NCC có công nợ)
- [ ] `Details.cshtml` (chi tiết + form trả nợ / ứng trước)
- [ ] `Receipt.cshtml` (in phiếu chi)

---

### 3.4 Infrastructure (Data)

#### Mappings (`NamEcommerce.Data.SqlServer/Mappings/`)
- [ ] `VendorDebtMapping.cs` — copy pattern từ `CustomerDebtMapping.cs`
  - Table: `VendorDebt`
  - Index unique `Code`
  - Index `VendorId`, `PurchaseOrderId`
  - `TotalAmount`, `PaidAmount`, `RemainingAmount` — `decimal(18,2)`
- [ ] `VendorPaymentMapping.cs`
  - Table: `VendorPayment`
  - Index unique `Code`
  - Index `VendorId`, `VendorDebtId`, `PurchaseOrderId`

#### DbContext (`NamEcommerceEfDbContext.cs`)
- [ ] Thêm `DbSet<VendorDebt>`
- [ ] Thêm `DbSet<VendorPayment>`
- [ ] Apply configurations

#### Migration
> **KHÔNG tự chạy migration** — sau khi xong Sprint 1, báo user để họ chạy:
> - `Add-Migration AddVendorDebt` (hoặc `dotnet ef migrations add AddVendorDebt`)
> - `Update-Database`

---

### 3.5 Tích hợp với PurchaseOrder (trigger tạo công nợ)

- [ ] Trong `PurchaseOrderAppService` (hoặc `PurchaseOrderManager`) — khi Status chuyển sang `Completed`:
  - Gọi `IVendorDebtManager.CreateDebtFromPurchaseOrderAsync(...)` với `VendorId`, `PurchaseOrderId`, `TotalAmount`
  - Manager auto-apply AdvancePayments chưa dùng (giống auto-apply Deposits)
- [ ] **Khuyến nghị**: Dùng domain event `PurchaseOrderCompletedEvent`, handler riêng trong `Web.Framework` gọi xuống AppService tạo debt → loose coupling hơn

### 3.6 Navigation / Menu

- [ ] Thêm menu "Công nợ NCC" trong `_Layout.cshtml` (hoặc partial nav) song song với "Công nợ khách hàng"
- [ ] Cập nhật `NAVIGATION_ANALYSIS.md` để đảm bảo thống nhất

### 3.7 Permissions

- [ ] Thêm permissions trong `Security` module:
  - `VendorDebt.View`
  - `VendorDebt.RecordPayment`
  - `VendorDebt.RecordAdvance`
- [ ] Cập nhật `RolePermission` seed

### 3.8 Unit Tests (TDD — BẮT BUỘC)

#### `Tests/NamEcommerce.Domain.Test/Debts/VendorDebtManagerTests.cs`
- [ ] `CreateDebtFromPurchaseOrderAsync_ValidInput_CreatesDebt`
- [ ] `CreateDebtFromPurchaseOrderAsync_DebtExistsForPO_ReturnsExisting`
- [ ] `CreateDebtFromPurchaseOrderAsync_VendorNotFound_Throws`
- [ ] `CreateDebtFromPurchaseOrderAsync_PurchaseOrderNotFound_Throws`
- [ ] `CreateDebtFromPurchaseOrderAsync_WithAdvancePayments_AutoApplies`
- [ ] `RecordPaymentAsync_ValidDebt_ReducesRemainingAmount`
- [ ] `RecordPaymentAsync_ExceedsRemaining_Throws`
- [ ] `RecordPaymentAsync_PaysFull_UpdatesStatusToPaid`
- [ ] `RecordFlexiblePaymentForVendorAsync_MultipleDebts_AppliesFifo`
- [ ] `RecordFlexiblePaymentForVendorAsync_OverpayAmount_CreatesAdvance`
- [ ] `RecordAdvancePaymentAsync_CreatesUnappliedPayment`
- [ ] Status transition: `Outstanding` → `PartiallyPaid` → `Paid`

#### `Tests/NamEcommerce.Application.Test/Debts/VendorDebtAppServiceTests.cs`
- [ ] Validation failure → `Success=false`, `ErrorMessage` không rỗng
- [ ] Map DTO đúng field (kiểm tra hậu tố `Utc`)
- [ ] Phân trang: `PageIndex`/`PageSize` truyền xuống đúng

---

## 4. Thứ tự triển khai (Sprint plan)

### Sprint 1 — Domain + Data (2-3 ngày)
1. Entities `VendorDebt` + `VendorPayment`
2. Domain DTOs + Exceptions + Enum extensions
3. `IVendorDebtManager` interface
4. `VendorDebtManager` implementation
5. Extensions `ToDto()`
6. EF Mappings + DbContext
7. **Unit test song song theo TDD** (Manager)
8. **Báo user** chạy `Add-Migration AddVendorDebt` + `Update-Database`

### Sprint 2 — Application (1 ngày)
9. App DTOs + `Validate()`
10. `IVendorDebtAppService` + implementation
11. Extensions AppDto
12. Unit test AppService

### Sprint 3 — Presentation (2 ngày)
13. Commands + Queries + Result Models
14. Command/Query Handlers
15. ModelFactory
16. Controller + ViewModels + Validators
17. Views (`Index`, `Details`, `Receipt`)
18. Menu navigation

### Sprint 4 — Tích hợp & Báo cáo (1-2 ngày)
19. Hook vào `PurchaseOrder` khi hoàn tất → tạo debt
20. Thêm vào báo cáo tài chính cuối tháng/kỳ/năm trong `ReportController`:
    - Tổng công nợ phải trả
    - Công nợ quá hạn
    - Tổng tiền đã chi cho NCC trong kỳ
21. Permissions + Role gán

**Tổng thời gian ước tính**: 6–8 ngày công

---

## 5. Rủi ro & điểm lưu ý

- **Idempotency**: `CreateDebtFromPurchaseOrderAsync` phải check đã có debt cho PO chưa (như `CustomerDebt` kiểm tra `DeliveryNoteId`) để tránh double-create khi PO chuyển status nhiều lần.
- **DateTime convention**: AppDto dùng hậu tố `Utc`, Presentation Model dùng local. Dùng `DateTimeHelper.ToUniversalTime()` / `.ToLocalTime()` khi map.
- **Pagination convention**: Presentation `PageNumber` (1-based) ↔ Application trở đi `PageIndex` (0-based, = `PageNumber - 1`).
- **Web.Contracts isolation**: Query/Command Handler chỉ return `Model` từ `Web.Contracts`, KHÔNG return `AppDto`.
- **KHÔNG tự chạy migration** — sau khi xong Sprint 1, báo user để họ chạy `Add-Migration` và `Update-Database`.
- **Reuse vs duplicate**: Giữ `DebtStatus` dùng chung cho cả CustomerDebt và VendorDebt (cùng semantic: Outstanding / PartiallyPaid / Paid / Overdue).
- **Phiếu chi ↔ nhiều debt**: Design hiện tại là 1 payment → 1 debt (hoặc null nếu advance). FIFO sẽ tạo nhiều `VendorPayment` rows cho 1 lần chi — consistent với `CustomerPayment`.
- **Bảo mật dữ liệu nhạy cảm**: Phiếu chi chứa thông tin tài chính — phải gắn permission `VendorDebt.View` trước khi cho phép truy cập.

---

## 6. Quyết định cần chốt trước khi code

1. **Naming**: `VendorDebt` (đồng bộ với entity `Vendor`) hay `SupplierDebt` (tự nhiên hơn trong tiếng Việt)? → **Đề xuất: `VendorDebt`**
2. **Trigger tạo debt**: Khi PO chuyển sang `Completed` hay khi `Receiving` (nhận hàng từng phần)? → **Đề xuất: `Completed` cho vòng đầu, có thể mở rộng sau**
3. **Tính năng Ứng trước NCC** có làm ở vòng đầu không? → **Đề xuất: CÓ** (vì CustomerDebt đã có deposit, cần parity)
4. **Domain event vs direct call** khi tạo debt từ PO? → **Đề xuất: Domain event** (loose coupling)
5. **Permission** có cần tạo mới hay dùng chung với CustomerDebt? → **Đề xuất: Tạo riêng** (VendorDebt.View, VendorDebt.RecordPayment, VendorDebt.RecordAdvance)
