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

<!-- rtk-instructions v2 -->
# RTK (Rust Token Killer) - Token-Optimized Commands

## Golden Rule

**Always prefix commands with `rtk`**. If RTK has a dedicated filter, it uses it. If not, it passes through unchanged. This means RTK is always safe to use.

**Important**: Even in command chains with `&&`, use `rtk`:
```bash
# ❌ Wrong
git add . && git commit -m "msg" && git push

# ✅ Correct
rtk git add . && rtk git commit -m "msg" && rtk git push
```

## RTK Commands by Workflow

### Build & Compile (80-90% savings)
```bash
rtk cargo build         # Cargo build output
rtk cargo check         # Cargo check output
rtk cargo clippy        # Clippy warnings grouped by file (80%)
rtk tsc                 # TypeScript errors grouped by file/code (83%)
rtk lint                # ESLint/Biome violations grouped (84%)
rtk prettier --check    # Files needing format only (70%)
rtk next build          # Next.js build with route metrics (87%)
```

### Test (60-99% savings)
```bash
rtk cargo test          # Cargo test failures only (90%)
rtk go test             # Go test failures only (90%)
rtk jest                # Jest failures only (99.5%)
rtk vitest              # Vitest failures only (99.5%)
rtk playwright test     # Playwright failures only (94%)
rtk pytest              # Python test failures only (90%)
rtk rake test           # Ruby test failures only (90%)
rtk rspec               # RSpec test failures only (60%)
rtk test <cmd>          # Generic test wrapper - failures only
```

### Git (59-80% savings)
```bash
rtk git status          # Compact status
rtk git log             # Compact log (works with all git flags)
rtk git diff            # Compact diff (80%)
rtk git show            # Compact show (80%)
rtk git add             # Ultra-compact confirmations (59%)
rtk git commit          # Ultra-compact confirmations (59%)
rtk git push            # Ultra-compact confirmations
rtk git pull            # Ultra-compact confirmations
rtk git branch          # Compact branch list
rtk git fetch           # Compact fetch
rtk git stash           # Compact stash
rtk git worktree        # Compact worktree
```

Note: Git passthrough works for ALL subcommands, even those not explicitly listed.

### GitHub (26-87% savings)
```bash
rtk gh pr view <num>    # Compact PR view (87%)
rtk gh pr checks        # Compact PR checks (79%)
rtk gh run list         # Compact workflow runs (82%)
rtk gh issue list       # Compact issue list (80%)
rtk gh api              # Compact API responses (26%)
```

### JavaScript/TypeScript Tooling (70-90% savings)
```bash
rtk pnpm list           # Compact dependency tree (70%)
rtk pnpm outdated       # Compact outdated packages (80%)
rtk pnpm install        # Compact install output (90%)
rtk npm run <script>    # Compact npm script output
rtk npx <cmd>           # Compact npx command output
rtk prisma              # Prisma without ASCII art (88%)
```

### Files & Search (60-75% savings)
```bash
rtk ls <path>           # Tree format, compact (65%)
rtk read <file>         # Code reading with filtering (60%)
rtk grep <pattern>      # Search grouped by file (75%)
rtk find <pattern>      # Find grouped by directory (70%)
```

### Analysis & Debug (70-90% savings)
```bash
rtk err <cmd>           # Filter errors only from any command
rtk log <file>          # Deduplicated logs with counts
rtk json <file>         # JSON structure without values
rtk deps                # Dependency overview
rtk env                 # Environment variables compact
rtk summary <cmd>       # Smart summary of command output
rtk diff                # Ultra-compact diffs
```

### Infrastructure (85% savings)
```bash
rtk docker ps           # Compact container list
rtk docker images       # Compact image list
rtk docker logs <c>     # Deduplicated logs
rtk kubectl get         # Compact resource list
rtk kubectl logs        # Deduplicated pod logs
```

### Network (65-70% savings)
```bash
rtk curl <url>          # Compact HTTP responses (70%)
rtk wget <url>          # Compact download output (65%)
```

### Meta Commands
```bash
rtk gain                # View token savings statistics
rtk gain --history      # View command history with savings
rtk discover            # Analyze Claude Code sessions for missed RTK usage
rtk proxy <cmd>         # Run command without filtering (for debugging)
rtk init                # Add RTK instructions to CLAUDE.md
rtk init --global       # Add RTK to ~/.claude/CLAUDE.md
```

## Token Savings Overview

| Category | Commands | Typical Savings |
|----------|----------|-----------------|
| Tests | vitest, playwright, cargo test | 90-99% |
| Build | next, tsc, lint, prettier | 70-87% |
| Git | status, log, diff, add, commit | 59-80% |
| GitHub | gh pr, gh run, gh issue | 26-87% |
| Package Managers | pnpm, npm, npx | 70-90% |
| Files | ls, read, grep, find | 60-75% |
| Infrastructure | docker, kubectl | 85% |
| Network | curl, wget | 65-70% |

Overall average: **60-90% token reduction** on common development operations.
<!-- /rtk-instructions -->