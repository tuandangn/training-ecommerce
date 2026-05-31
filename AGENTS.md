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

## Quick rules (ONLY for simple tash)

**Entity:** `sealed record`, constructor `internal`, properties `public ... { get; private set; }`, change state methods accessibility is `internal`

**Manager:** inject `IRepository<T>` (write) + `IEntityDataReader<T>` (read). Publish events qua `IEventPublisher`. Input DTO has `Verify()` (throw exception).

**AppService:** Input DTO có `Validate()` (return `(bool, string?)`). Not throw exceptions — return result object with `Success = false`.

**Controller:** chỉ inject `IMediator` + `IModelFactory`. Consice, not business logic.

**ModelFactory:** inject `IMediator` + `AppConfig`. Method `Prepare{Xxx}Model(...)`. Use MediatR queries.

---

## Need more context

- Working with Domain layer → read `docs/domain.md`
- Working with Application layer → read `docs/application.md`
- Working with Presentation layer → read `docs/presentation.md`
- New or improvement module/feature → MUST HAVE USER COMMITED PLAN + IMPLEMENTATION (with TodoList)
- Create new module → read `.claude/checklist.md` + 3 layer docs

---

## Where read or save documentation?

- Implemented modules/features → `docs/{Module}/
- New modules/features plan: `plans/{Module}/{short_desc}_plan.md`
- New modules/features implementation: `plans/{Module}/{short_desc}_implement.md`

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
