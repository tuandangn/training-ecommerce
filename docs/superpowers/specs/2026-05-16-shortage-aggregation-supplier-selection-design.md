# Chọn nhà cung cấp trên trang "Hàng cần nhập"

**Date:** 2026-05-16
**Status:** Approved (design)
**Scope:** `NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/ShortageAggregation.cshtml` (Razor markup + inline IIFE script). Không thay đổi C#, DTO, hay migration.

## Problem

Trang `PurchaseOrder/ShortageAggregation` ("Hàng cần nhập") hiện gom mỗi mặt hàng thiếu vào **một nhà cung cấp duy nhất** — NCC đầu tiên trong danh sách gợi ý (`primarySuggestion`: NCC ưu tiên, hoặc NCC mua gần nhất). Khi một sản phẩm có nhiều NCC, người dùng không thể chọn NCC khác cho mặt hàng đó trực tiếp trên trang này.

## Key facts (đã xác minh trong codebase)

- `SupplierSuggestionService.SuggestVendorsForProductAsync` trả về tối đa 5 NCC cho mỗi sản phẩm (NCC ưu tiên theo `DisplayOrder` trước, rồi NCC theo lịch sử mua gần nhất).
- `ShortageAggregationAppService` gom mỗi item vào group theo `suggestions.FirstOrDefault()` và bỏ qua các gợi ý còn lại trong việc gom nhóm.
- **Mỗi `ShortageAggregationItemAppDto` đã mang đầy đủ `SupplierSuggestions`** (VendorId, VendorName, LastPurchaseDateUtc, LastUnitPrice, IsPreferred...) xuống view. View hiện chỉ dùng `SupplierSuggestions.FirstOrDefault(s => s.VendorId == group.VendorId)` để hiển thị "Giá lần trước".
- Toàn bộ logic phía sau (xây payload `CreateFromShortage`, modal "Đơn nhập liên quan", tính tổng tiền, checkbox nhóm) đều đọc dữ liệu NCC từ **card chứa dòng đó** (`.shortage-group` với `data-vendor-id` / `data-vendor-name`). Vì vậy chỉ cần di chuyển DOM của dòng sang đúng card là mọi thứ phía sau hoạt động đúng mà không cần sửa.

→ Đây là thay đổi UI thuần trên view. Dữ liệu cần thiết đã có sẵn.

## Chosen approach

**Per-item vendor `<select>` + di chuyển dòng sang card NCC đích.**

Khi sản phẩm có ≥2 NCC gợi ý, hiển thị dropdown chọn NCC ngay trên dòng mặt hàng. Khi đổi NCC: di chuyển DOM của dòng vào card của NCC được chọn (tạo card mới từ template ẩn nếu NCC đó chưa có card), tự điền lại đơn giá theo `LastUnitPrice` của NCC mới, rồi tính lại tổng/bộ lọc.

Không thay đổi server/DTO/migration. Tôn trọng quy tắc dự án: không tự chạy migration, không sửa project `*.Test`.

### Phương án đã loại

- **Payload-only reassignment:** card hiển thị và phiếu nhập thực tế lệch nhau về thị giác — người dùng đã từ chối.
- **Server-side restructure (client-rendered model):** phải viết lại view ~900 dòng, vi phạm nguyên tắc thay đổi tối thiểu so với lợi ích thu được.

## Design

### 1. Markup — bộ chọn NCC trên từng dòng

Trong khối `.shortage-item-inputs` của mỗi dòng mặt hàng, render thêm `<select class="item-vendor">` **chỉ khi `item.SupplierSuggestions.Count > 1`**:

- Mỗi `<option>`: `value` = `VendorId`, label = `VendorName` (kèm gợi ý giá/ngày lần trước nếu có).
- `selected` = `VendorId` của group hiện tại (NCC mặc định = NCC gần nhất hiện hành).
- Khi `Count <= 1`: không render gì (giữ nguyên hành vi tĩnh hiện tại).

Gắn lên phần tử dòng `.shortage-order-item` thuộc tính `data-suggestions` chứa JSON danh sách gợi ý của item đó (`vendorId`, `vendorName`, `lastUnitPrice`, `lastPurchaseDate` đã format `dd/MM`) để script dùng khi đổi NCC mà không cần gọi server.

Thêm một `<template id="vendorCardTemplate">` ẩn chứa khung card NCC rỗng: header (checkbox nhóm, icon, tên NCC, meta), controls (ô "Hẹn nhận" với giá trị mặc định `defaultExpectedDate`, ô "Ghi chú"), và container `.shortage-order-items` rỗng. Dùng để tạo card cho NCC chưa từng là group phía server.

### 2. Script — xử lý khi đổi NCC (`change` trên `.item-vendor`)

1. Đọc `vendorId` được chọn và bản ghi gợi ý tương ứng từ `data-suggestions` của dòng.
2. Tìm card đích: `.shortage-group[data-vendor-id="<chosen>"]`.
   - Nếu **chưa có**: clone `#vendorCardTemplate`, set `data-vendor-id` / `data-vendor-name`, điền tên NCC vào heading, append vào trang; đăng ký card mới vào mảng `groups` đang dùng và thêm `<option>` vào `#vendorFilter`; gắn lại event listener cho `group-check` và các `item-check` của card mới.
3. Di chuyển DOM của dòng `.shortage-order-item` vào `.shortage-order-items` của card đích.
4. Cập nhật ô `.unit-cost` của dòng = `lastUnitPrice` của NCC mới (nếu có); nếu NCC mới không có giá lần trước thì **giữ nguyên** giá hiện tại. Ô vẫn cho phép sửa tay.
5. Cập nhật text meta "Giá lần trước (dd/MM)" của dòng theo NCC mới.
6. Gọi lại `syncGroupCheck` cho card nguồn và card đích, `refreshTotals()`, `applyFilters()`.
7. Ẩn card nào còn 0 dòng sau khi di chuyển (đồng nhất với cơ chế ẩn card rỗng khi lọc hiện có).

### 3. Edge cases

- Mặt hàng nhóm "Chưa có nhà cung cấp" (`IsNoVendorGroup`) không có gợi ý → không render selector (giữ nguyên).
- Dropdown chỉ liệt kê NCC gợi ý **của chính sản phẩm đó** (không phải bộ chọn NCC tùy ý) — đúng với "nếu sản phẩm có nhiều nhà cung cấp".
- Di chuyển dòng cuối cùng ra khỏi một card phía server → card đó bị ẩn.
- Modal "Đơn nhập liên quan" và payload `CreateFromShortage` không cần sửa: chúng đọc NCC từ card chứa dòng, vốn đã được di chuyển đúng.
- Bộ lọc NCC (`#vendorFilter`) và bộ lọc mặt hàng tiếp tục hoạt động; thêm option NCC mới khi tạo card động để filter không bị thiếu lựa chọn.

## Testing & verification

Theo quy tắc dự án: **không viết unit test mới, không sửa project `*.Test`**.

- Verification: `dotnet build` thành công.
- Manual walkthrough: chọn sản phẩm có nhiều NCC → đổi NCC trên dòng → dòng di chuyển sang card NCC đã chọn (card được tạo nếu cần), đơn giá tự cập nhật, tổng tiền từng card và tổng chung tính lại đúng, bấm "Tạo phiếu nhập" → phiếu được tạo dưới đúng NCC đã chọn.
- Kiểm tra trường hợp NCC mới không có giá lần trước (giữ nguyên giá), và di chuyển dòng cuối khiến card nguồn rỗng.

## Out of scope

- Thay đổi logic gợi ý NCC ở `SupplierSuggestionService`.
- Cho phép chọn NCC bất kỳ ngoài danh sách gợi ý của sản phẩm.
- Thay đổi DTO / app service / migration.
