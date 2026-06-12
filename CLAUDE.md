# NamEcommerce — Claude Code Context

## Architecture

```
Clean Architecture + DDD
Request → Controller → MediatR → Handler → AppService → Manager → Entity → Repository
```

### Projects Structure

| Project | Content |
|---|---|
| `NamEcommerce.Domain` | Entities (`sealed record`, constructor `internal`) |
| `NamEcommerce.Domain.Shared` | Base classes, Enums, Domain DTOs, Interfaces |
| `NamEcommerce.Domain.Services` | Domain Managers + Extensions |
| `NamEcommerce.Application.Contracts` | IAppService interfaces + Application DTOs |
| `NamEcommerce.Application.Services` | AppService implementations + Extensions |
| `NamEcommerce.Data.SqlServer` | EF Core configs, migrations, repositories |
| `NamEcommerce.Web` | Controllers, Views, ModelFactories |
| `NamEcommerce.Web.Contracts` | Commands, Queries, Result Models |
| `NamEcommerce.Web.Framework` | MediatR Command/Query Handlers |

---

## Modules

| Module | Main Entities |
|---|---|
| Catalog | Category, Product, UnitMeasurement, Vendor |
| Orders | Order, OrderItem |
| Inventory | InventoryStock, InventoryCostLedgerEntry, ProductReservationLedger,  Warehouse, StockMovementLog |
| PurchaseOrders | PurchaseOrder, PurchaseOrderItem, PurchaseOrderItemAllocation |
| Returns | CustomerReturn, VendorReturn |
| Customers | Customer |
| Debts | CustomerDebt, CustomerPayment |
| DeliveryNotes | DeliveryNote, DeliveryNoteItem |
| GoodsReceipt | GoodsReceipt, GoodsReceiptItem |
| Finance | Expense, InventoryCostingPolicy, InventoryCostAllocation, InventoryCostRebuildRun |
| Media | Picture |
| Users | User, Role, UserRole |
| Security | Permission, RolePermission |
| CustomerPortals |

---

## Exception Handling
Avoid try/catch unless truly necessary. Prefer:
- Guard clauses with early return
- Null-conditional operators (`?.`, `??`)
- `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrEmpty`

try/catch is acceptable only for external I/O (HTTP, file system, DB) or when catching a specific exception to convert it to a domain result (e.g. in AppService returning `Success = false`).

---

## Design
Khi làm việc với UI/UX hãy áp dụng Hệ màu/Font từ DESIGN.md kết hợp với Tư duy bố cục và Kỷ luật chống slop của SKILL.md.

---

## Quick rules (ONLY for simple tash)

**Entity:** `sealed record`, constructor `internal`, properties `public ... { get; private set; }`, change state methods accessibility is `internal`

**Manager:** inject `IRepository<T>` (write) + `IEntityDataReader<T>` (read). Publish events qua `IEventPublisher`. Input DTO has `Verify()` (throw exception).

**Repository semantics (Unit of Work):** `IRepository<T>` stage changes (no SaveChanges inside). `UnitOfWorkBehavior` commits once at end of every MediatR Command — EXCEPT when response implements `ICommandResult` with `Success = false` (skip commit; opt-in per result model — see `ICommandResult.cs` for exclusions like Casso/RegisterUser where failure still persists data). `IEntityDataReader<T>` is **read-only / untracked** — use `IRepository<T>.GetByIdAsync` when you need a tracked entity for concurrency checks (load-for-write pattern). All domain events go through Outbox (eventual, OutboxProcessor dispatches in a fresh DI scope); handlers must be idempotent. Background services that call AppServices/Managers directly must get `IUnitOfWork` from their DI scope and call `CommitAsync` after completing writes. Use `EntityCodeGenerator` (Scoped) for entity code generation to prevent duplicate codes within a single command.

**AppService:** Input DTO có `Validate()` (return `(bool, string?)`). Not throw exceptions — return result object with `Success = false`.

**Controller:** chỉ inject `IMediator` + `IModelFactory`. Consice, not business logic.

**ModelFactory:** inject `IMediator` + `AppConfig`. Method `Prepare{Xxx}Model(...)`. Use MediatR queries.

---

## Need more context

- Working with Domain layer → read `docs/domain.md`
- Working with Application layer → read `docs/application.md`
- Working with Presentation layer → read `docs/presentation.md`
- New module / improvement feature plan → MUST HAVE USER COMMITED PLAN + IMPLEMENTATION (with TodoList)
- Create new module → read `.claude/checklist.md` + 3 layer docs

---

## Where read or save documentation?

- Implemented modules/features → `docs/{Module}/
- New modules/features plan: `plans/{Module}/plan_{short_desc}.md`
- New modules/features implementation: `plans/{Module}/implement_{short_desc}.md`

---

## Lệnh build/test

```bash
# Build solution
dotnet build NamEcommerce.sln

# Chạy tất cả tests
dotnet test

# Chạy test một project
dotnet test Tests/NamEcommerce.Domain.Services.Test

# Test theo filter
dotnet test --filter "FullyQualifiedName~CategoryManager"
```

---

## Conventions nhanh

- Namespace theo folder: `NamEcommerce.Domain.Entities.Catalog`
- `InternalsVisibleTo` khai báo trong `Domain/Accessibility/AssemblyAccessibility.cs`
- Extension `ToDto()` trong `{Layer}/Extensions/{Entity}Extensions.cs`
- Exception classes trong `Domain.Shared/Exceptions/{Module}/`