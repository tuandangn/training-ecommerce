# Plan — Phiếu xuất thu tiền theo trạng thái đã thanh toán + chặn công nợ dương khách lẻ

## Vấn đề (bug đang gặp)

Khi tạo phiếu xuất hàng, số tiền phải thu (`AmountToCollect`) **không trừ phần khách đã thanh toán cho đơn** → hệ thống cho thu lại bất kể đã thu hay chưa.

Nguyên nhân gốc (xác nhận trong code):
- **JS** `Views/DeliveryNote/Create.cshtml:325` → mặc định `amountToCollect = totalSelected + surcharge` (luôn đề xuất thu trọn).
- **Server** `DeliveryNoteManager.cs:74-82` → cap theo `remaining = OrderTotal + Surcharge − existingCollect`, với `existingCollect` chỉ là tổng `AmountToCollect` của các phiếu xuất khác. Không trừ `CustomerPayment` gắn `OrderId`.

`Order` không lưu trạng thái thanh toán; tiền đã thu nằm ở chứng từ `CustomerPayment.OrderId`.

## Phát hiện then chốt — không cần denormalize

"Đã thu theo đơn" **đã có sẵn ở cấp chứng từ**, keyed theo `OrderId`, KHÔNG dùng chung như balance ledger:

```
paidForOrder(orderId) = Σ CustomerPayment.Amount WHERE OrderId == orderId
```

Đúng cho cả khách lẻ lẫn khách thường — chỉ cần query, cùng pattern `existingCollect` đang dùng.

**Không** thêm field/danh sách tham chiếu lên `Order` (tránh migration + dual-write + lệch số). Single source of truth là chứng từ `CustomerPayment`.

**Lưu ý double-count:** QR intent khi `Confirmed/ManuallyConfirmed` đã tạo sẵn 1 `CustomerPayment(OrderId)` (xem `FastSaleAppService:175-216`). Vì vậy **chỉ** cộng `CustomerPayment`, KHÔNG cộng thêm `BankTransferPaymentIntent` → tránh tính tiền 2 lần.

## Mục tiêu

1. Số tiền phải thu khi tạo phiếu xuất tự trừ phần đã thanh toán cho đơn (server cap + JS default), áp dụng mọi loại khách.
2. Đơn khách lẻ (`CustomerKind.RetailWalkIn && IsSystem`) **không được để lại công nợ dương**: lúc hoàn tất giao, tổng đã thu phải phủ hết giá trị hàng đã giao, nếu không → chặn.

## Ngoài phạm vi

- Không thêm `OrderStatus.Processing` (đã thống nhất bỏ — tránh phình enum qua nhiều layer).
- Không khóa sửa item theo đơn (tách thành việc khác nếu sau này cần).
- Không thêm migration DB.
- Không đổi mô hình ledger.

## Quyết định thiết kế

- `paidForOrder` = `Σ CustomerPayment.Amount WHERE OrderId == orderId`, derive bằng query, không lưu trên `Order`.
- Domain nhận diện khách lẻ qua `IEntityDataReader<Customer>` → `Kind == CustomerKind.RetailWalkIn && IsSystem`.
- Guard khách lẻ áp dụng **bất kể** luồng (shipper hoàn tất / admin hoàn tất / duyệt thu hụt). Thu hụt tạo công nợ → không cho với khách lẻ.
- Hàng bị từ chối (rejected/returns) làm giảm số phải thu — không tính là công nợ.

---

## Phase 1 — Thu tiền theo số đã thanh toán (fix bug gốc)

### 1a. Domain — cap server có trừ đã thanh toán

`DeliveryNoteManager`:
- Inject thêm `IEntityDataReader<CustomerPayment> customerPaymentReader`.
- Trong `CreateFromOrderAsync`, sửa block `dto.AmountToCollect > 0`:
  ```
  var paidForOrder = customerPaymentReader.DataSource
      .Where(p => p.OrderId == dto.OrderId)
      .Sum(p => p.Amount);
  var remaining = order.OrderTotal + dto.Surcharge - existingCollect - paidForOrder;
  if (dto.AmountToCollect > remaining)
      throw new AmountToCollectExceedsOrderRemainingException(dto.AmountToCollect, Math.Max(0m, remaining));
  ```

### 1b. Web — mặc định form đúng

- `Models/DeliveryNotes/CreateDeliveryNoteModel.cs`: thêm `public decimal AmountAlreadyPaidForOrder { get; set; }`.
- `Services/DeliveryNotes/DeliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync`: tính `AmountAlreadyPaidForOrder` bằng pattern sẵn có (`_customerDebtAppService.GetPaymentsAsync(customerId, ...)` filter `OrderId == orderId`, như `OrderModelFactory:411-412`) — hoặc thêm 1 query gọn `GetTotalPaidByOrderQuery` nếu trang sợ phân trang sót. Quyết định lúc implement, ghi vào implement doc.
- `Views/DeliveryNote/Create.cshtml`:
  - JS: `amountToCollect = Math.max(0, totalSelected + surcharge - amountAlreadyPaidForOrder)`.
  - Hiện dòng gợi ý "Đã thanh toán cho đơn: {DisplayCurrency}" khi `AmountAlreadyPaidForOrder > 0`.

### TodoList 1
- [ ] `DeliveryNoteManager`: inject reader + sửa công thức cap
- [ ] `CreateDeliveryNoteModel` + model factory: tính `AmountAlreadyPaidForOrder`
- [ ] `Create.cshtml`: JS default + dòng gợi ý
- [ ] Build `NamEcommerce.Web.csproj`

---

## Phase 2 — Chặn công nợ dương với khách lẻ

### 2a. Exception + resource

- `Domain.Shared/Exceptions/DeliveryNotes/RetailOrderCannotLeaveDebtException.cs` (kế thừa `NamEcommerceDomainException`, code `Error.RetailOrderCannotLeaveDebt`).
- Thêm chuỗi vào `SharedResource.resx` + `SharedResource.vi-VN.resx`.

### 2b. Domain — guard lúc hoàn tất giao

`DeliveryNoteManager`:
- Inject thêm `IEntityDataReader<Customer> customerReader`.
- Helper `IsRetailWalkInAsync(order)` → tra `customerReader` theo `order.CustomerId`, true nếu `Kind == CustomerKind.RetailWalkIn && IsSystem`.
- Trong luồng hoàn tất giao (chỗ đã có `acceptance` + `cashCollected`, quanh `DeliveryNoteManager.cs:212-238` và luồng `MarkReceivedByCustomer`):
  ```
  if (isRetail)
  {
      var covered = paidForOrder + cashCollected;
      var owed = acceptance.AmountToCollect; // hàng đã nhận + phụ phí + agreed charge, đã trừ hàng từ chối
      if (covered + epsilon < owed)
          throw new RetailOrderCannotLeaveDebtException();
  }
  ```
- Áp dụng cho cả admin hoàn tất hộ và shipper. Với **duyệt thu hụt** (`ApproveSettlement`/`RequestSettlementApproval`): nếu đơn là khách lẻ và số duyệt để lại công nợ → chặn ngay tại bước request/approve (fail sớm, không đẩy xuống tận MarkDelivered).

### 2c. Web (UX)

- Trên form tạo phiếu xuất / màn settlement của đơn khách lẻ: hiện cảnh báo "Đơn khách lẻ phải thu đủ, không để công nợ" khi số phải thu > đã thanh toán và không có thao tác thu tại giao. (Hiển thị; chặn cứng vẫn ở domain.)

### TodoList 2
- [ ] Exception + 2 resource string
- [ ] `DeliveryNoteManager`: inject `IEntityDataReader<Customer>` + helper nhận diện khách lẻ
- [ ] Guard ở hoàn tất giao (shipper + admin) và ở request/approve thu hụt
- [ ] Web: cảnh báo trên UI khách lẻ
- [ ] Build `NamEcommerce.Web.csproj`

---

## Verification

- Build `NamEcommerce.Web.csproj` sau mỗi phase (không build .sln, không viết test — theo quy ước dự án).
- Smoke tay:
  1. Khách thường: thanh toán trước 1 phần đơn → tạo phiếu xuất → `AmountToCollect` mặc định = tổng − đã thanh toán; nhập vượt → bị chặn.
  2. Khách thường: chưa thanh toán → hành vi như cũ.
  3. Khách lẻ: chưa thanh toán đủ, hoàn tất giao mà không thu đủ → bị chặn (`Error.RetailOrderCannotLeaveDebt`).
  4. Khách lẻ: đã thanh toán đủ qua QR/tiền mặt (CustomerPayment theo OrderId) → tạo phiếu xuất `AmountToCollect = 0`, hoàn tất giao OK.
  5. QR confirmed: không bị tính 2 lần (chỉ đếm CustomerPayment).

## Migration

Không có. (Báo user nếu phát sinh field mới — hiện không.)
