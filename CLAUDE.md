# CLAUDE.md — Quy tắc làm việc của AI Assistant

> File này được đọc tự động bởi Claude ở đầu mỗi phiên làm việc.
> Chứa toàn bộ quy ước vận hành, KHÔNG chứa nội dung kỹ thuật của dự án (xem `SYSTEM_DOCUMENTATION.md`).

---

## 1. Workflow Phiên Làm Việc

### 1.1 Bắt đầu phiên

**Bước 1 — Kiểm tra uncommitted files từ phiên trước:**
- Chạy `git status --short` trên branch `dev-assistant`
- Nếu có file chưa staged/commit **VÀ** tồn tại file session trong `sessions/`:
  - Commit toàn bộ file chưa commit với message: `[uncompleted] session_N_yyyyMMdd - commit on session start`
  - Đổi tên file session **gần nhất** (số thứ tự cao nhất) thành `session_N_yyyyMMdd_uncompleted.md`
  - Nếu có nhiều file session chưa commit: chỉ đổi tên file gần nhất, các file cũ giữ nguyên
- Nếu chưa có file session nào (khởi đầu hệ thống): bỏ qua bước này

**Bước 2 — Lên kế hoạch đầy đủ trước khi làm:**
- Thảo luận với người dùng, xác định mục tiêu và các bước cụ thể
- Chỉ tạo file session SAU KHI kế hoạch đã hoàn chỉnh

**Bước 3 — Tạo file session:**
- Xác định số phiên tiếp theo: đọc thư mục `sessions/`, lấy số lớn nhất hiện có + 1
- Tạo file: `sessions/session_[N]_[yyyyMMdd].md` (ví dụ: `sessions/session_4_20260501.md`)
- Nội dung file session: xem mục 1.3

### 1.2 Trong phiên làm việc

- Cập nhật file session sau mỗi bước hoàn thành (đánh dấu `[x]`)
- Ghi chú ngắn nếu có thay đổi so với kế hoạch ban đầu
- Cập nhật `TodoList.md` khi hoàn thành hoặc phát sinh hạng mục mới

### 1.3 Kết thúc phiên

- Đảm bảo file session đã cập nhật đủ trạng thái
- Cập nhật `TodoList.md` (chuyển hạng mục hoàn thành sang `CheckList.md` nếu cần)
- Commit lên branch `dev-assistant`:
  ```
  git add -A
  git commit -m "<mô tả ngắn gọn những gì đã thay đổi>"
  ```
- KHÔNG push (người dùng tự push và merge vào `main` sau khi review)

---

## 2. Format File Session

```markdown
# Session [N] — [yyyyMMdd]

## Mục đích
[Tóm tắt 1-3 câu mục tiêu của phiên làm việc này]

## Kế hoạch
- [ ] Bước 1: ...
- [ ] Bước 2: ...
- [ ] Bước 3: ...

## Ghi chú
[Cập nhật trong quá trình làm — thay đổi, phát sinh, quyết định quan trọng]

## Kết quả
[Điền khi kết thúc phiên]
```

---

## 3. Source Control

| Quy tắc | Giá trị |
|---|---|
| Branch làm việc | `dev-assistant` |
| Commit timing | Cuối mỗi phiên làm việc |
| Push | Người dùng tự push sau khi review |
| Merge vào `main` | Người dùng tự làm sau khi kiểm tra |

**Commit message format:**
- Phiên thường: `<động từ ngắn>: <mô tả thay đổi chính>` — ví dụ: `feat: migrate GoodsReceipt sang concrete events`
- Phiên uncompleted bị phát hiện: `[uncompleted] session_N_yyyyMMdd - commit on session start`

---

## 4. Số Thứ Tự Phiên Làm Việc

- **Toàn cục** — không reset theo ngày
- Cách xác định: đọc `sessions/`, lấy số N lớn nhất từ tên file `session_N_*`, cộng thêm 1
- Nếu `sessions/` trống: bắt đầu từ N = 1

---

## 5. Quy tắc Bổ sung

- **Branch**: Mọi thay đổi code đều trên `dev-assistant`, KHÔNG commit thẳng vào `main`
- **Unit test**: Tạm thời KHÔNG viết unit test mới (người dùng tự bổ sung sau)
- **Migration**: KHÔNG tự chạy migration — báo người dùng tự chạy
- **Skills**: Đọc `namcommerce` skill trước khi viết bất kỳ code nào liên quan đến domain
- **CRLF**: Trên Linux sandbox, `git status` có thể báo hàng nghìn file modified do CRLF↔LF — bỏ qua nếu insertions = deletions và không có file session trước đó

---

## 6. Files Quan Trọng

| File | Mục đích |
|---|---|
| `CLAUDE.md` | Quy tắc vận hành AI (file này) |
| `TodoList.md` | Hạng mục cần làm hiện tại |
| `CheckList.md` | Hạng mục đã hoàn thành |
| `SYSTEM_DOCUMENTATION.md` | Kiến trúc kỹ thuật hệ thống |
| `sessions/` | Lịch sử phiên làm việc |
| `.skill-extract/namcommerce/` | Skill rules cho NamEcommerce |
