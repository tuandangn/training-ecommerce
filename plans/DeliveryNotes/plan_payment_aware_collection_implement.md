# Implement — Phiếu xuất thu tiền theo trạng thái đã thanh toán + chặn công nợ dương khách lẻ

Trạng thái: ĐÃ LÀM (build `NamEcommerce.Web.csproj` xanh). Migration: không có.

## Phase 1 — Thu tiền trừ phần đã thanh toán

- `DeliveryNoteManager`:
  - Inject `IEntityDataReader<CustomerPayment>`, `IEntityDataReader<Customer>`.
  - `CreateFromOrderAsync`: `remaining = OrderTotal + Surcharge − existingCollect − paidForOrder`; cap dùng `Math.Max(0, remaining)`. `paidForOrder = Σ CustomerPayment.Amount WHERE OrderId == orderId`.
- Query đã thu theo đơn (chọn phương án (b) — query gọn, tránh phân trang sót):
  - `ICustomerDebtManager.GetTotalPaidByOrderAsync` + impl ở `CustomerDebtManager` (sum `paymentReader` theo `OrderId`).
  - `ICustomerDebtAppService.GetTotalPaidByOrderAsync` + impl.
  - `GetOrderPaidAmountQuery` (Web.Contracts/Queries/Models/Orders) + `GetOrderPaidAmountHandler` (Web.Framework).
- Web form:
  - `CreateDeliveryNoteModel`: thêm `AmountAlreadyPaidForOrder`, `IsRetailWalkInCustomer`.
  - `DeliveryNoteModelFactory`: inject `ICachedValuesService`; set `AmountAlreadyPaidForOrder` (qua query) và `IsRetailWalkInCustomer = order.CustomerId == DefaultCustomerId`.
  - `Create.cshtml`: JS trừ `amountAlreadyPaidForOrder` khỏi số phải thu (`Math.max(0, …)`) + ghi chú "− Đã thanh toán (…)".

## Phase 2 — Chặn công nợ dương khách lẻ

- Exception `RetailOrderCannotLeaveDebtException` (`Error.RetailOrderCannotLeaveDebt`) + 2 resource string (en + vi).
- `DeliveryNoteManager.IsRetailWalkInCustomer(customerId)` = `Customer.Kind == RetailWalkIn && IsSystem`.
- Guard trong `MarkDeliveredAsync`: nếu khách lẻ và `cashCollected < acceptance.AmountToCollect` → throw. Bao phủ shipper, admin hoàn tất hộ, và `CompleteApprovedSettlementAsync` (đều đi qua `MarkDeliveredAsync`).
- `Create.cshtml`: cảnh báo vàng khi `IsRetailWalkInCustomer`.

## Quyết định phạm vi & ghi chú

- **FastSale không bị ảnh hưởng:** DeliverNow tạo DN trước khi `RecordPayment` (nên Phase 1 cap không chặn), và hoàn tất qua `MarkReceivedByCustomerAsync` (KHÔNG phải `MarkDeliveredAsync`) → guard Phase 2 không áp. Giữ nguyên có chủ đích.
- **FastSale đã chặn bán chịu khách lẻ:** `FastSaleAppService.ValidateQuickSaleAsync` — nếu `paymentTiming == Unpaid` và khách là retail walk-in (`Kind==RetailWalkIn && IsSystem`) → trả lỗi `Error.RetailOrderCannotLeaveDebt` (fail-fast, trước khi tạo record). Áp cho cả DeliverNow lẫn OrderOnly. Vì vậy đường `MarkReceivedByCustomerAsync` (do `QuickSaleDeliverRequested` gọi) chỉ chạy khi đã PayNow → không sinh nợ khách lẻ.
- **UI FastSale (tùy chọn, chưa làm):** có thể ẩn/disable lựa chọn "bán chịu" khi khách đang chọn là khách lẻ mặc định, tránh user chọn rồi mới báo lỗi.
- **Edge ShowPrice + khách lẻ trả trước qua admin thủ công:** `acceptance.AmountToCollect` nhánh ShowPrice = full giá trị hàng (không trừ trả trước) → guard có thể *chặn nhầm* (đòi thu lại). Đây là chặn nhầm (recover được: thu hoặc tắt ShowPrice), KHÔNG phải tạo nợ ngầm — đúng hướng an toàn theo yêu cầu "không được để công nợ".

## Verify

- Build `NamEcommerce.Web.csproj`: Build succeeded, 0 errors.
- Smoke tay (chưa chạy): theo checklist trong plan gốc.
