# Spec: Phiếu Chuyển Kho (Stock Transfer Note)

> Phiên bản: 2026-05-18
> Branch đích: `dev-assistant`

---

## 1. Mục tiêu

Cho phép nhân viên kho tạo **phiếu chuyển kho** để di chuyển hàng hóa từ kho này sang kho khác
(ví dụ: sửa lỗi nhập nhầm kho khi nhận hàng). Quy trình: **Draft → Approved / Cancelled**.
Khi phê duyệt, hệ thống tự động điều chỉnh `InventoryStock` của cả hai kho và ghi `StockMovementLog`.

---

## 2. Phân tích codebase hiện tại

### Đã có sẵn (không cần làm lại)

| Thành phần | Trạng thái |
|---|---|
| `StockMovementType.Transfer` | ✅ enum đã có |
| `StockReferenceType.StockTransfer = 4` | ✅ enum đã có |
| `IInventoryStockManager.TransferStockAsync(...)` | ✅ domain method đã implement |
| `WarehouseType` enum (Main, SubWarehouse, ReturnWarehouse, DirectShipTransit) | ✅ có |
| `InventoryStock`, `StockMovementLog` entities | ✅ có |

### Hoàn toàn thiếu (cần build mới)

| Thành phần | Cần tạo |
|---|---|
| `StockTransferNote` entity + `StockTransferNoteItem` | ❌ |
| `IStockTransferNoteManager` + `StockTransferNoteManager` | ❌ |
| `IStockTransferNoteAppService` + `StockTransferNoteAppService` | ❌ |
| Commands / Query handlers (Web.Framework) | ❌ |
| `StockTransferController` | ❌ |
| Views: List, Create, Details | ❌ |

### Pattern tham chiếu

Toàn bộ code mới **follow 100% pattern của `StockAdjustmentNote`** (entity, manager, appservice, handlers, controller, views).

---

## 3. Quy trình nghiệp vụ

```
Tạo phiếu (Draft)
    ↓
Xem lại thông tin (kho nguồn, kho đích, danh sách hàng hóa, số lượng)
    ↓
Phê duyệt (Approve)  ←→  Hủy (Cancel)
    ↓
TransferStockAsync chạy cho từng item:
    - Trừ InventoryStock tại kho nguồn
    - Cộng InventoryStock tại kho đích
    - Ghi 2 StockMovementLog (Out + In)
```

**Ràng buộc:**
- Kho nguồn ≠ kho đích
- Số lượng chuyển > 0
- Số lượng chuyển ≤ `QuantityAvailable` tại kho nguồn (kiểm tra lúc Approve)
- Không chuyển vào/ra kho `DirectShipTransit`
- Chỉ có thể Cancel khi Status = Draft (giống StockAdjustmentNote)
- Giá vốn: tự động lấy `AverageCost` từ `InventoryStock` tại kho nguồn (lúc Approve)

---

## 4. Kiến trúc & File cần tạo

### 4.1 Domain — Entities

**File: `NamEcommerce.Domain/Entities/StockTransfer/StockTransferNote.cs`**
```
StockTransferNote : AppAggregateEntity
  - Code: string
  - FromWarehouseId: Guid
  - FromWarehouseName: string?
  - ToWarehouseId: Guid
  - ToWarehouseName: string?
  - Note: string?
  - Status: StockTransferStatus (Draft=0, Approved=1, Cancelled=2)
  - ApprovedOnUtc: DateTime?
  - CreatedByUserId: Guid?
  - CreatedOnUtc: DateTime
  - UpdatedOnUtc: DateTime?
  - Items: IReadOnlyCollection<StockTransferNoteItem>

Methods:
  - AddItem(productId, productName, quantity)
  - Approve() → raise StockTransferNoteApproved event
  - Cancel()
```

**File: `NamEcommerce.Domain/Entities/StockTransfer/StockTransferNoteItem.cs`**
```
StockTransferNoteItem : AppAggregateEntity
  - NoteId: Guid
  - ProductId: Guid
  - ProductName: string
  - Quantity: decimal
  - UnitCost: decimal (set lúc Approve từ AverageCost kho nguồn)
```

### 4.2 Domain Shared — Enums, DTOs, Events, Exceptions

**File: `NamEcommerce.Domain.Shared/Enums/StockTransfer/StockTransferStatus.cs`**
```csharp
public enum StockTransferStatus { Draft = 0, Approved = 1, Cancelled = 2 }
```

**File: `NamEcommerce.Domain.Shared/Events/StockTransfer/StockTransferNoteEvents.cs`**
```csharp
public sealed record StockTransferNoteApproved(Guid NoteId, Guid FromWarehouseId, Guid ToWarehouseId) : INotification;
```

**File: `NamEcommerce.Domain.Shared/Dtos/StockTransfer/StockTransferNoteDtos.cs`**
```
StockTransferNoteDto { Id, Code, FromWarehouseId, FromWarehouseName, ToWarehouseId, ToWarehouseName, Note, Status, ApprovedOnUtc, CreatedByUserId, CreatedOnUtc, Items[] }
StockTransferNoteItemDto { Id, NoteId, ProductId, ProductName, Quantity, UnitCost }
CreateStockTransferNoteDto { FromWarehouseId, ToWarehouseId, Note, Items[] }
CreateStockTransferNoteItemDto { ProductId, ProductName, Quantity }
```

**File: `NamEcommerce.Domain.Shared/Exceptions/StockTransfer/StockTransferNoteExceptions.cs`**
```
StockTransferNoteNotFoundException
StockTransferNoteCannotChangeStatusException
StockTransferInsufficientStockException
StockTransferSameWarehouseException
```

**File: `NamEcommerce.Domain.Shared/Services/StockTransfer/IStockTransferNoteManager.cs`**
```csharp
public interface IStockTransferNoteManager
{
    Task<StockTransferNoteDto> CreateAsync(CreateStockTransferNoteDto dto);
    Task ApproveAsync(Guid id);
    Task CancelAsync(Guid id);
    Task<StockTransferNoteDto?> GetByIdAsync(Guid id);
    Task<IPagedDataDto<StockTransferNoteDto>> GetListAsync(int pageIndex, int pageSize, string? keywords, Guid? warehouseId, StockTransferStatus? status);
}
```

### 4.3 Domain Services

**File: `NamEcommerce.Domain.Services/StockTransfer/StockTransferNoteManager.cs`**

Logic chính:
- `GenerateCode()` → prefix `"PCT-{yyyyMMdd}-"` (Phiếu Chuyển Tồn)
- `CreateAsync()` → validate 2 kho ≠ nhau + ≠ DirectShipTransit, tạo note Draft
- `ApproveAsync()` → với mỗi item:
  1. Lấy `AverageCost` từ kho nguồn → set `item.UnitCost`
  2. Validate `QuantityAvailable ≥ quantity`
  3. Gọi `_stockManager.TransferStockAsync(productId, fromWarehouseId, toWarehouseId, quantity, unitCost, noteId, userId)`
  4. `note.Approve()` → raise event

### 4.4 Application Layer

**File: `NamEcommerce.Application.Contracts/Dtos/StockTransfer/StockTransferNoteAppDtos.cs`**
(Mirror của domain DTOs nhưng dùng `int Status` thay enum)

**File: `NamEcommerce.Application.Contracts/StockTransfer/IStockTransferNoteAppService.cs`**
```csharp
public interface IStockTransferNoteAppService
{
    Task<CreateStockTransferNoteResultAppDto> CreateAsync(CreateStockTransferNoteAppDto dto);
    Task<StockTransferNoteResultAppDto> ApproveAsync(Guid id);
    Task<StockTransferNoteResultAppDto> CancelAsync(Guid id);
    Task<StockTransferNoteAppDto?> GetByIdAsync(Guid id);
    Task<IPagedDataAppDto<StockTransferNoteListAppDto>> GetListAsync(int pageIndex, int pageSize, string? keywords, Guid? fromWarehouseId, int? status);
}
```

**File: `NamEcommerce.Application.Services/StockTransfer/StockTransferNoteAppService.cs`**
Delegate sang `IStockTransferNoteManager`, wrap exception thành result objects.

### 4.5 Presentation — Web.Contracts

**File: `NamEcommerce.Web.Contracts/Commands/StockTransfer/StockTransferNoteCommands.cs`**
```
CreateStockTransferNoteCommand : IRequest<CreateStockTransferNoteResultModel>
ApproveStockTransferNoteCommand(Guid id) : IRequest<StockTransferNoteResultModel>
CancelStockTransferNoteCommand(Guid id) : IRequest<StockTransferNoteResultModel>
```

**File: `NamEcommerce.Web.Contracts/Queries/StockTransfer/StockTransferNoteQueries.cs`**
```
GetStockTransferNoteByIdQuery(Guid id) : IRequest<StockTransferNoteModel>
GetStockTransferNoteListQuery : IRequest<StockTransferNoteListModel>
```

**File: `NamEcommerce.Web.Contracts/Models/StockTransfer/StockTransferNoteModels.cs`**

### 4.6 Presentation — Web.Framework (Command/Query Handlers)

**File: `NamEcommerce.Web.Framework/Commands/Handlers/StockTransfer/StockTransferNoteHandlers.cs`**
**File: `NamEcommerce.Web.Framework/Queries/Handlers/StockTransfer/StockTransferNoteQueryHandlers.cs`**

### 4.7 Presentation — Web (Controller, Views, Models, ModelFactory)

**File: `NamEcommerce.Web/Controllers/StockTransferController.cs`**
```
Actions: Index, List, Create (GET+POST), Details, Approve (POST), Cancel (POST)
```

**File: `NamEcommerce.Web/Models/StockTransfer/CreateStockTransferNoteModel.cs`**
```
FromWarehouseId, ToWarehouseId, Note, Items[]
AvailableFromWarehouses, AvailableToWarehouses (loại trừ DirectShipTransit)
```

**File: `NamEcommerce.Web/Services/StockTransfer/IStockTransferNoteModelFactory.cs`**
**File: `NamEcommerce.Web/Services/StockTransfer/StockTransferNoteModelFactory.cs`**

**Views:**
- `Views/StockTransfer/List.cshtml` — bảng danh sách phiếu, filter theo kho + status
- `Views/StockTransfer/Create.cshtml` — form: chọn kho nguồn, kho đích, thêm dòng hàng hóa (dynamic table giống StockAdjustment)
- `Views/StockTransfer/Details.cshtml` — xem chi tiết, nút Approve/Cancel

### 4.8 Migration

> ⚠️ **AI KHÔNG tự chạy migration.** Báo Tuấn chạy:
> ```
> Add-Migration AddStockTransferNote
> Update-Database
> ```

Tables cần tạo: `StockTransferNotes`, `StockTransferNoteItems`

---

## 5. Điểm còn thiếu trong quy trình quản lý kho

Kiểm tra toàn bộ codebase, đây là các phần còn thiếu theo mức độ ưu tiên:

### Mức cao (cần làm sớm)

| Thiếu gì | Hiện trạng | Ghi chú |
|---|---|---|
| **Phiếu chuyển kho** | ❌ (spec này) | Domain method đã có, UI chưa có |
| **Phiếu xuất kho thủ công** (Stock Issue) | ❌ | `DispatchStockAsync` đã có nhưng không có UI phiếu xuất độc lập (ngoài SO) |

### Mức trung bình

| Thiếu gì | Hiện trạng | Ghi chú |
|---|---|---|
| **Cảnh báo tồn kho thấp** | ⚠️ Partial | `IStockAlertService` interface có nhưng chưa implement. `ReorderLevel` lưu DB nhưng không có UI cảnh báo. |
| **Stocktake / Kiểm kê định kỳ** | ⚠️ Partial | `StockAdjustmentNote` đang kiêm vai trò này nhưng thiếu: (1) snapshot số liệu hệ thống lúc mở phiếu, (2) workflow nhiều người kiểm đếm cùng lúc. |

### Mức thấp / tương lai

| Thiếu gì | Ghi chú |
|---|---|
| **Báo cáo tồn kho theo thời điểm** | Chỉ có log, chưa có snapshot lịch sử |
| **Quản lý vị trí trong kho** (zone/bin) | `WarehouseZoneId` trường đã có trên `InventoryStock` nhưng không có entity Zone |
| **Phiếu trả hàng về kho từ khách** | `CustomerReturnController` đã có nhưng cần kiểm tra flow nhập kho đầy đủ |

---

## 6. Acceptance Criteria

- [ ] Tạo phiếu chuyển kho với nhiều sản phẩm, kho nguồn ≠ kho đích
- [ ] Hệ thống báo lỗi nếu kho nguồn = kho đích
- [ ] Hệ thống báo lỗi nếu số lượng chuyển > tồn khả dụng tại kho nguồn (khi Approve)
- [ ] Kho `DirectShipTransit` không xuất hiện trong dropdown chuyển kho
- [ ] Phê duyệt → `InventoryStock` kho nguồn giảm, kho đích tăng
- [ ] Phê duyệt → 2 `StockMovementLog` được ghi (Transfer/Out và Transfer/In)
- [ ] Giá vốn chuyển kho = `AverageCost` kho nguồn tại thời điểm phê duyệt
- [ ] Cancel chỉ được khi Status = Draft
- [ ] List view có filter theo kho nguồn + status
- [ ] Code phiếu tự sinh theo format `PCT-YYYYMMDD-NNN`

---

## 7. Thứ tự implement (Build Order)

```
Task 1: Domain Entity (StockTransferNote + Item + enums + exceptions + events)
Task 2: IStockTransferNoteManager + StockTransferNoteManager
Task 3: Domain DTOs + IStockTransferNoteManager interface
Task 4: IStockTransferNoteAppService + StockTransferNoteAppService
Task 5: Web.Contracts (Commands + Queries + Models)
Task 6: Web.Framework (Handlers)
Task 7: Web (Controller + ModelFactory + Views)
Task 8: [Tuấn chạy] Add-Migration + Update-Database
Task 9: Smoke test
```

> Tasks 1–3 phải xong trước khi làm Task 4.
> Task 4 phải xong trước Task 5–7.
> Tasks 5–7 có thể làm song song.
