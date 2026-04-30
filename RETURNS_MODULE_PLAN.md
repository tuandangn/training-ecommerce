# Returns Module — Thiết kế tổng thể

> Tài liệu này mô tả thiết kế nghiệp vụ và kỹ thuật cho module Trả hàng (Returns).
> **Trạng thái: Chưa bắt đầu code.**

---

## 1. Tổng quan

Module Returns xử lý hai luồng đối xứng:

| Luồng | Mô tả | Đối xứng với |
|---|---|---|
| **CustomerReturn** | Khách hàng trả hàng lại cửa hàng | Order → DeliveryNote |
| **VendorReturn** | Cửa hàng trả hàng lại nhà cung cấp | PurchaseOrder → GoodsReceipt |

**Nguyên tắc chung:**
- Cho phép trả **một phần** số lượng.
- Giá trả hàng **mặc định theo giá gốc**, nhưng có thể điều chỉnh.
- **Bắt buộc kiểm tra chất lượng** trước khi cập nhật tồn kho và công nợ.
- Một phiếu trả hàng có thể bao gồm hàng từ **nhiều lần giao/nhập** khác nhau.

---

## 2. CustomerReturn — Khách hàng trả hàng

### 2.1 Liên kết

```
Order (1) ──── (*) CustomerReturn
CustomerReturn (1) ──── (*) CustomerReturnItem
CustomerReturnItem ──── (0..1) DeliveryNoteItem  (tùy chọn, để tra giá gốc)
```

- Liên kết ở cấp **Order** (không bắt buộc chỉ định DeliveryNote cụ thể).
- Khách có thể trả hàng từ nhiều lần giao trong cùng 1 đơn.

### 2.2 Entity: CustomerReturn

| Field | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | PK |
| `OrderId` | Guid | Đơn hàng gốc |
| `CustomerId` | Guid | Khách hàng |
| `WarehouseId` | Guid | Kho nhập hàng trả về |
| `ReturnDate` | DateTime | Ngày tạo phiếu trả |
| `Note` | string? | Lý do / ghi chú |
| `Status` | Enum | Xem mục 2.4 |

### 2.3 Entity: CustomerReturnItem

| Field | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | PK |
| `CustomerReturnId` | Guid | FK |
| `ProductId` | Guid | Sản phẩm |
| `DeliveryNoteItemId` | Guid? | FK tùy chọn — để tra giá bán gốc |
| `RequestedQuantity` | decimal | Khách muốn trả bao nhiêu |
| `AcceptedQuantity` | decimal | Sau kiểm tra: chấp nhận bao nhiêu (≤ RequestedQuantity) |
| `UnitPrice` | decimal | Giá hoàn lại (mặc định = giá bán gốc) |

### 2.4 Status Flow

```
Draft ──→ Inspecting ──→ Confirmed
  │                          │
  └──────── Cancelled ←──────┘
```

| Status | Ý nghĩa |
|---|---|
| `Draft` | Phiếu mới tạo, chưa nhận hàng |
| `Inspecting` | Hàng đã về, đang kiểm tra chất lượng |
| `Confirmed` | Kiểm tra xong, nhập kho + điều chỉnh công nợ |
| `Cancelled` | Hủy phiếu (chỉ được khi còn Draft hoặc Inspecting) |

### 2.5 Hiệu ứng khi Confirmed

- **Tồn kho:** Cộng `AcceptedQuantity` vào kho `WarehouseId`
- **Công nợ khách hàng:** Giảm `Σ(AcceptedQuantity × UnitPrice)`
- **Lưu ý:** Nếu khách đã thanh toán hết → xử lý hoàn tiền/credit sẽ làm ở phase sau

---

## 3. VendorReturn — Cửa hàng trả hàng cho NCC

### 3.1 Liên kết

```
GoodsReceipt (1) ──── (*) VendorReturn
VendorReturn (1) ──── (*) VendorReturnItem
VendorReturnItem ──── (0..1) GoodsReceiptItem  (tùy chọn, để tra giá nhập gốc)
```

### 3.2 Entity: VendorReturn

| Field | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | PK |
| `GoodsReceiptId` | Guid | Phiếu nhập hàng gốc |
| `VendorId` | Guid | Nhà cung cấp |
| `WarehouseId` | Guid | Kho xuất hàng trả đi |
| `ReturnDate` | DateTime | Ngày tạo phiếu trả |
| `Note` | string? | Lý do / ghi chú |
| `Status` | Enum | Xem mục 3.4 |

### 3.3 Entity: VendorReturnItem

| Field | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | PK |
| `VendorReturnId` | Guid | FK |
| `ProductId` | Guid | Sản phẩm |
| `GoodsReceiptItemId` | Guid? | FK tùy chọn — để tra giá nhập gốc |
| `RequestedQuantity` | decimal | Số lượng muốn trả |
| `AcceptedQuantity` | decimal | Sau kiểm tra NCC xác nhận: nhận bao nhiêu |
| `UnitCost` | decimal | Giá hoàn lại (mặc định = giá nhập gốc) |

### 3.4 Status Flow

```
Draft ──→ Inspecting ──→ Confirmed
  │                          │
  └──────── Cancelled ←──────┘
```

| Status | Ý nghĩa |
|---|---|
| `Draft` | Phiếu mới tạo |
| `Inspecting` | Đang kiểm tra hàng cần trả (kiểm tra nội bộ trước khi gửi NCC) |
| `Confirmed` | NCC đã nhận hàng, trừ kho + điều chỉnh công nợ |
| `Cancelled` | Hủy phiếu |

### 3.5 Hiệu ứng khi Confirmed

- **Tồn kho:** Trừ `AcceptedQuantity` từ kho `WarehouseId`
- **Công nợ NCC:** Giảm `Σ(AcceptedQuantity × UnitCost)`

---

## 4. Domain Events

| Event | Trigger | Handler dự kiến |
|---|---|---|
| `CustomerReturnConfirmed` | CustomerReturn → Confirmed | Cộng tồn kho, giảm CustomerDebt |
| `CustomerReturnCancelled` | CustomerReturn → Cancelled | — |
| `VendorReturnConfirmed` | VendorReturn → Confirmed | Trừ tồn kho, giảm VendorDebt |
| `VendorReturnCancelled` | VendorReturn → Cancelled | — |

---

## 5. Các vấn đề chưa quyết định (để làm sau)

| Vấn đề | Ghi chú |
|---|---|
| Hoàn tiền khi khách đã thanh toán | Tạo credit note hay hoàn tiền mặt? Chưa làm. |
| Ảnh hưởng đến AverageCost | Khi trả hàng cho NCC, AverageCost có cần recalculate? |
| Giới hạn số lần trả | Có cho phép trả nhiều lần cho cùng 1 Order/GoodsReceipt không? |
| Report | Báo cáo tỉ lệ trả hàng theo sản phẩm / khách hàng / NCC |

---

## 6. Thứ tự thực hiện (khi bắt đầu code)

1. **Domain Layer:** Entities + Enums + Mark methods + Domain Events
2. **Domain Services:** `CustomerReturnManager`, `VendorReturnManager`
3. **Application Layer:** AppService + DTOs + Contracts
4. **Infrastructure:** EF Configuration + Migration (Tuấn tự chạy)
5. **Presentation:** Controller + ModelFactory + Views

---

## 7. Files cần tạo mới (ước tính)

```
Domain.Shared/
  Enums/Returns/CustomerReturnStatus.cs
  Enums/Returns/VendorReturnStatus.cs
  Events/Returns/CustomerReturnEvents.cs
  Events/Returns/VendorReturnEvents.cs

Domain/
  Entities/Returns/CustomerReturn.cs
  Entities/Returns/CustomerReturnItem.cs
  Entities/Returns/VendorReturn.cs
  Entities/Returns/VendorReturnItem.cs
  Extensions/Returns/CustomerReturnExtensions.cs
  Extensions/Returns/VendorReturnExtensions.cs

Domain.Services/
  Returns/ICustomerReturnManager.cs
  Returns/CustomerReturnManager.cs
  Returns/IVendorReturnManager.cs
  Returns/VendorReturnManager.cs
  Returns/Dtos/CustomerReturnDto.cs
  Returns/Dtos/VendorReturnDto.cs

Application.Contracts/
  Returns/ICustomerReturnAppService.cs
  Returns/IVendorReturnAppService.cs
  Dtos/Returns/CustomerReturnAppDtos.cs
  Dtos/Returns/VendorReturnAppDtos.cs

Application.Services/
  Returns/CustomerReturnAppService.cs
  Returns/VendorReturnAppService.cs
  Events/Returns/CustomerReturnConfirmedEventHandler.cs
  Events/Returns/VendorReturnConfirmedEventHandler.cs

Data.SqlServer/
  Configurations/Returns/CustomerReturnConfiguration.cs
  Configurations/Returns/VendorReturnConfiguration.cs
```
