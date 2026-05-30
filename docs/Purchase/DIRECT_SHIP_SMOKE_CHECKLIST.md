# Direct-Ship Smoke Checklist

> Checklist tay cho manual smoke test. Không viết test code trong project `*.Test`.
> Tuấn chạy local server + UI thật, đánh dấu pass/fail bên cạnh từng mục.
> Cần chạy đầy đủ sau mỗi lần deploy lớn hoặc sau khi thay đổi flow nhận hàng / giao hàng.

---

## Điều kiện tiên quyết

- [ ] DB đã chạy migration `UpdateDirectShip` (chạy `Update-Database` nếu chưa).
- [ ] Kho "Direct-Ship Transit" tồn tại (tự tạo lần đầu khi allocation direct-ship được nhận).
- [ ] Có ít nhất 1 NCC, 1 khách, 1 sản phẩm trong hệ thống.

---

## Scenario 1 — Happy path: SO 30 + PO 100 giao thẳng đủ

1. [ ] Tạo SO: khách đặt 30 bao xi măng.
2. [ ] Vào Shortage Aggregation → tick "Giao thẳng tới khách" cho item xi măng → nhập địa chỉ + SĐT khách → tạo PO 100 bao.
3. [ ] Vào PO Details: column "Direct-ship?" hiển thị icon + tooltip địa chỉ cho allocation.
4. [ ] Nhập hàng PO: confirm nhận 100 bao.
   - [ ] Kho chính tăng 100 — xem tồn kho.
   - [ ] Transfer 30 sang kho "Direct-Ship Transit" — xem tồn kho kho ảo.
   - [ ] DeliveryNote được tạo tự động, `Status = Confirmed`, `SourceType = DirectShipToCustomer`.
5. [ ] Vào menu Bán hàng → Giao hàng trực tiếp NCC: thấy 1 row DN chờ confirm.
6. [ ] Vào SO Details → tab "Giao thẳng": thấy allocation với "Đã nhận 30 / 30" và "Chờ xác nhận".
7. [ ] Bấm **Confirm** trên trang Pending Deliveries → nhập note + ngày → submit.
   - [ ] DN chuyển sang `Delivered` — biến mất khỏi danh sách pending.
   - [ ] SO Details → tab "Giao thẳng": trạng thái đổi thành "Đã giao".
   - [ ] `CustomerDebt` sinh đúng giá bán (kiểm tra Công nợ khách).
   - [ ] Kho "Direct-Ship Transit" giảm 30.

---

## Scenario 2 — Giao thiếu: NCC giao 20/30

1. [ ] Tạo SO 30 + PO direct-ship 30 (như scenario 1, nhưng chỉ 30 bao).
2. [ ] Nhập hàng: confirm nhận 20 bao (thiếu 10).
   - [ ] Kho chính tăng đúng số nhận (0 nếu tất cả là direct-ship, hoặc phần về kho).
   - [ ] Transfer 20 sang Direct-Ship Transit.
   - [ ] DN tạo với qty = 20, `Status = Confirmed`.
3. [ ] Nhập thêm 10 bao còn lại.
   - [ ] Transfer thêm 10 → Transit = 30.
   - [ ] DN tạo thêm (hoặc DN cũ cập nhật) cho 10 bao còn lại.

---

## Scenario 3 — NCC giao thừa: 110 bao / PO 100

1. [ ] Tạo SO 30 + PO direct-ship 100 bao.
2. [ ] Nhập hàng: nhập 110 → modal xuất hiện "NCC giao thừa 10 đơn vị".
3. [ ] Chọn **Nhập kho chính** → submit lại.
   - [ ] `PurchaseOrderItem.QuantityReceived = 100` (không vượt orderedQty).
   - [ ] Kho chính nhận thêm 10 bao từ GR free stock.
   - [ ] `VendorDebt` tăng theo giá PO × 10.
4. [ ] Kiểm tra lại: chọn **Từ chối phần thừa** → nhập 100 bao → không tạo GR free stock.
5. [ ] Kiểm tra lại: chọn **Hủy GR** → đóng modal → không có gì thay đổi.

---

## Scenario 4 — Khách từ chối (Reject DN)

1. [ ] Tạo SO + PO direct-ship, nhận hàng để DN ở `Status = Confirmed`.
2. [ ] Bấm **Reject** trên DN Details (hoặc trang Pending Deliveries) → nhập lý do.
   - [ ] DN chuyển sang `Cancelled`.
   - [ ] Hàng trong kho "Direct-Ship Transit" chuyển về kho chính (giá vốn = giá PO).
   - [ ] SO Details → tab "Giao thẳng": trạng thái "Đã từ chối".

---

## Scenario 5 — Cancel SO sau khi đã nhận hàng

1. [ ] Tạo SO + PO direct-ship, nhận hàng để Transit có hàng.
2. [ ] Vào SO Details → bấm **Cancel SO**.
   - [ ] Modal cảnh báo xuất hiện: "Có X bao đang ở kho Direct-Ship Transit. Chuyển về kho chính?"
3. [ ] Bấm Cancel trên modal (không hủy) → SO không thay đổi trạng thái.
4. [ ] Bấm Confirm trên modal.
   - [ ] Hàng từ Transit về kho chính, giá vốn = giá PO.
   - [ ] SO chuyển sang `Cancelled`.
   - [ ] DN direct-ship còn `Confirmed` chuyển sang `Cancelled`.

---

## Scenario 6 — Sửa địa chỉ giao sau khi PO đã confirm

1. [ ] Tạo PO có allocation direct-ship đã xác nhận.
2. [ ] Vào PO Details → bấm **Sửa địa chỉ giao** trên allocation đó.
3. [ ] Nhập địa chỉ mới + lý do → save.
   - [ ] `DirectShipAddressChangeLog` có entry mới (kiểm tra qua API hoặc DB).
   - [ ] Banner cảnh báo "Đã có phiếu cũ — gửi lại phiếu mới cho NCC" xuất hiện.

---

## Scenario 7 — N-N allocation: 1 SO chia 2 PO

1. [ ] SO khách đặt 50 bao.
2. [ ] Tạo PO1: 30 bao direct-ship (địa chỉ khách).
3. [ ] Tạo PO2: 20 bao về kho.
4. [ ] Nhận hàng cả 2 PO.
   - [ ] PO1 tạo DN direct-ship qty=30 chờ confirm.
   - [ ] PO2 tạo phiếu nhập kho chính qty=20.
5. [ ] SO Details hiển thị đủ 2 allocation với trạng thái đúng.
6. [ ] Confirm DN của PO1 → `CustomerDebt` đúng.

---

## Checklist DN Details

Sau mỗi scenario tạo DN direct-ship, vào DN Details:
- [ ] Banner highlight "Giao hàng trực tiếp NCC" hiển thị.
- [ ] Hiển thị link PO nguồn + GR nguồn.
- [ ] Button Confirm/Reject hiển thị khi `Status = Confirmed`.

---

## Checklist Báo cáo Direct-Ship

Vào menu Báo cáo → Direct-Ship:
- [ ] Bảng "Theo NCC" có dữ liệu.
- [ ] Bảng "Theo khách" có dữ liệu.
- [ ] Bảng "Theo sản phẩm" có dữ liệu.
- [ ] Mục "Pending > 7 ngày" hiển thị nếu có DN quá hạn.
- [ ] Tỷ lệ Reject hiển thị số liệu đúng sau khi reject 1 DN.

---

## Pass/Fail Notes

| Scenario | Kết quả | Ghi chú |
|----------|---------|---------|
| 1 — Happy path | | |
| 2 — Giao thiếu | | |
| 3 — Giao thừa | | |
| 4 — Reject DN | | |
| 5 — Cancel SO | | |
| 6 — Sửa địa chỉ | | |
| 7 — N-N allocation | | |
| DN Details banner | | |
| Báo cáo | | |
