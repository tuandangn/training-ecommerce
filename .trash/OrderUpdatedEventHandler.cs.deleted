// Deprecated — removed in session 2026-05-02 (Phase 5 prerequisite).
//
// Handler cũ subscribe EntityUpdatedNotification<Order> với body rỗng (return Task.CompletedTask)
// — không có logic nào để mất. Mọi nhu cầu phản ứng với việc Order thay đổi đã được phục vụ
// bởi 2 concrete event:
//   - OrderInfoUpdated     ← khi note / expected shipping date / discount thay đổi
//   - OrderShippingUpdated ← khi shipping address / expected shipping date thay đổi
// (xem Domain.Shared/Events/Orders/OrderEvents.cs).
//
// Lý do file vẫn còn (không bị xoá hoàn toàn): scheduled task session không xoá được file
// trên Windows mount. Tuấn có thể xóa file này thủ công trong lần dọn Phase 5 — Cleanup.
