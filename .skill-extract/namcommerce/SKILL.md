---
name: namcommerce
description: >
  Skill bắt buộc dùng khi làm việc với hệ thống NamEcommerce / VLXD Tuấn Khôi — bao gồm viết code mới, mở rộng module, sửa bug, hoặc bất kỳ thay đổi nào trong codebase .NET. Kích hoạt khi người dùng đề cập đến: Domain Entity, Manager, AppService, Controller, ModelFactory, MediatR handler, unit test, repository, migration, hay bất kỳ module nào (Catalog, Orders, Inventory, Finance, Customers, Debts...). Đây là skill chứa toàn bộ quy tắc kiến trúc và naming conventions — hãy đọc kỹ trước khi viết bất kỳ dòng code nào.
---

# NamEcommerce — Skill Hướng Dẫn Làm Việc

## Nguyên tắc tối thượng

1. **KHÔNG chạy lệnh migration** (`Add-Migration`, `dotnet ef migrations add`) — người dùng tự làm.
2. **KHÔNG chạy lệnh update database** (`Update-Database`, `dotnet ef database update`) — người dùng tự làm.
3. **Mọi method mới trên Domain Manager** phải có unit test tương ứng viết theo TDD.

---

## Kiến trúc tổng quan

```
Clean Architecture + DDD
├── Domain Layer         → Business rules thuần túy
├── Application Layer    → Use cases, orchestration
├── Infrastructure       → EF Core, SQL Server, repositories
└── Presentation         → ASP.NET Core MVC + MediatR
```

**Luồng dữ liệu:** Request → Controller → MediatR → Handler → AppService → Manager → Repository

---

## Cấu trúc thư mục Projects

| Project | Mục đích |
|---|---|
| `NamEcommerce.Domain` | Entities theo module (Catalog, Orders, Inventory...) |
| `NamEcommerce.Domain.Shared` | Base classes, Enums, Constants, Domain DTOs, Interfaces |
| `NamEcommerce.Domain.Services` | Domain Managers — business logic phức tạp |
| `NamEcommerce.Application.Contracts` | Interfaces AppService + Application DTOs |
| `NamEcommerce.Application.Services` | Implementation AppService |
| `NamEcommerce.Data.SqlServer` | EF Core configs, migrations, repositories |
| `NamEcommerce.Web` | Controllers, Views, ModelFactories |
| `NamEcommerce.Web.Contracts` | Commands, Queries, Models, View Models |
| `NamEcommerce.Web.Framework` | MediatR Command/Query Handlers |

---

## ⚠️ Quy tắc Dependency Isolation

**`NamEcommerce.Web.Contracts.csproj` chỉ có một dependency duy nhất là `MediatR`.**

- Command/Query **KHÔNG ĐƯỢC** dùng `AppDto`, `DomainDto`, hay type từ project khác
- Command/Query **CHỈ ĐƯỢC** return `Model` type định nghĩa trong `Web.Contracts/Models/{Module}/`
- Ánh xạ `AppDto → Model` thực hiện trong Handler tại `Web.Framework`

```csharp
// ❌ SAI
public sealed class GetRecentPurchasePricesQuery : IRequest<IList<RecentPurchasePriceAppDto>>
// ✅ ĐÚNG
public sealed class GetRecentPurchasePricesQuery : IRequest<IList<RecentPurchasePriceModel>>
```

---

## ⚠️ Quy tắc DateTime giữa các Layer

| Layer | Convention | Lý do |
|---|---|---|
| Domain Entity, Domain DTO, AppDto | Có hậu tố `Utc` — `CreatedOnUtc` | Lưu DB dạng UTC |
| Web.Contracts Model, Web Model | Không có hậu tố — `CreatedOn` | Hiển thị LocalTime cho user |

**Presentation → Application** (user nhập vào):
```csharp
FromDateUtc = DateTimeHelper.ToUniversalTime(request.FromDate)
```
**Application → Presentation** (hiển thị ra):
```csharp
CreatedOn = appDto.CreatedOnUtc.ToLocalTime()
```

---

## ⚠️ Quy tắc Phân trang giữa các Layer

| Layer | Property | Bắt đầu từ | Lý do |
|---|---|---|---|
| Presentation (SearchModel, Command, Query) | `PageNumber` | **1** | Thân thiện với người dùng |
| Application trở đi (AppService, Manager) | `PageIndex` | **0** | Phù hợp skip/take |

**Công thức:** `PageIndex = PageNumber - 1` — chuyển đổi tại Handler hoặc ModelFactory.

```csharp
// ✅ SearchModel trong Presentation — chỉ dùng PageNumber
public sealed class XyzListSearchModel
{
    public int PageNumber { get; set; } = 1;   // KHÔNG có PageIndex
    public int PageSize { get; set; }
}

// ✅ ModelFactory chuyển đổi sang Query
var pageIndex = Math.Max(1, searchModel.PageNumber) - 1;
await _mediator.Send(new GetXyzListQuery { PageIndex = pageIndex, PageSize = pageSize });

// ✅ Handler chuyển đổi sang AppService
var result = await _xyzAppService.GetXyzsAsync(keywords, request.PageIndex, request.PageSize);
```

> Query trong `Web.Contracts` có thể dùng `PageIndex` vì nó là tầng giao tiếp nội bộ (Handler → AppService), không phải tầng user-facing.

---

## LAYER 1: Domain Layer

### Domain Entity — Quy tắc viết

- Kế thừa `AppAggregateEntity` (có soft delete) hoặc `AppEntity`
- Khai báo là `sealed record`
- **Constructor `internal`** — chỉ Manager mới được tạo entity
- **Properties: `public ... { get; private set; }`** (hoặc `internal set` nếu Manager cần)
- **Methods thay đổi state: `internal`**
- DateTime lưu DB luôn UTC, tên có hậu tố `Utc`

```csharp
[Serializable]
public sealed record Category : AppAggregateEntity
{
    internal Category(Guid id, string name) : base(id)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
        CreatedOnUtc = DateTime.UtcNow;
    }
    public string Name { get; private set; }
    public DateTime CreatedOnUtc { get; }
    internal async Task SetNameAsync(string name, INameExistCheckingService checker) { ... }
}
```

> Chi tiết và template đầy đủ: đọc `references/domain-layer.md`

### Domain Manager — Naming & Quy tắc

| Item | Convention | Ví dụ |
|---|---|---|
| Interface | `I{Entity}Manager` | `ICategoryManager` |
| Implementation | `{Entity}Manager` | `CategoryManager` |
| Input DTO (có `Verify()`) | `Create{Entity}Dto` | `CreateCategoryDto` |
| Output DTO | `{Entity}Dto` | `CategoryDto` |
| DTO location | `Domain.Shared/Dtos/{Module}/` | |
| Interface location | `Domain.Shared/Services/{Module}/` | |
| Implementation location | `Domain.Services/{Module}/` | |
| Extension ToDto() | `Domain.Services/Extensions/` | |

- `Verify()` throw exception nếu invalid
- Dùng `IRepository<T>` để Write, `IEntityDataReader<T>` để Read
- Publish events: `IEventPublisher.EntityCreated/Updated/Deleted`
- Mọi method public phải có unit test (TDD)

---

## LAYER 2: Application Layer

### AppService — Naming & Quy tắc

| Item | Convention | Ví dụ |
|---|---|---|
| Interface | `I{Entity}AppService` | `ICategoryAppService` |
| Implementation | `{Entity}AppService` | `CategoryAppService` |
| Input DTO (có `Validate()`) | `Create{Entity}AppDto` | `CreateCategoryAppDto` |
| Output DTO | `{Entity}AppDto` | `CategoryAppDto` |
| DTO location | `Application.Contracts/Dtos/{Module}/` | |
| Interface location | `Application.Contracts/{Module}/` | |
| Implementation location | `Application.Services/{Module}/` | |
| Extension ToDto() | `Application.Services/Extensions/` | |

- `Validate()` trả về `(bool valid, string? errorMessage)` — KHÔNG throw exception
- AppService không throw exception ra ngoài — return `{ Success = false }`
- DateTime trong AppDto có hậu tố `Utc`
- Pagination nhận `pageIndex` (0-based) từ Handler

```csharp
// AppDto — DateTime có hậu tố Utc
[Serializable]
public sealed record XyzAppDto(Guid Id)
{
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? FromDateUtc { get; init; }
}

// AppService — nhận pageIndex
public Task<IPagedDataAppDto<XyzAppDto>> GetXyzsAsync(
    string? keywords, int pageIndex, int pageSize) { ... }
```

> Chi tiết và template đầy đủ: đọc `references/application-layer.md`

---

## LAYER 3: Presentation Layer

### Controller — Quy tắc

- Kế thừa `BaseAuthorizedController` hoặc `BaseController`
- Chỉ inject `IMediator` và `I{Entity}ModelFactory`
- Code ngắn gọn — không chứa business logic, không gọi AppService trực tiếp
- Giao tiếp chỉ qua MediatR

### ModelFactory — Naming

| Item | Convention |
|---|---|
| Interface | `I{Entity}ModelFactory` |
| Implementation | `{Entity}ModelFactory` |
| Location | `NamEcommerce.Web/Services/{Module}/` |
| Method | `Prepare{Xxx}Model(searchModel)` |

- Inject `IMediator` + `AppConfig`
- Chuyển đổi `PageNumber → PageIndex` trước khi gửi Query

### Commands, Queries & Models

| Item | Convention | Location |
|---|---|---|
| Command | `Create{Entity}Command` | `Web.Contracts/Commands/Models/{Module}/` |
| Query | `Get{Entity}Query`, `Get{Entity}ListQuery` | `Web.Contracts/Queries/Models/{Module}/` |
| Model | `{Entity}Model`, `{Entity}ListModel` | `Web.Contracts/Models/{Module}/` |
| Handler | `Create{Entity}Handler` | `Web.Framework/{Commands\|Queries}/Handlers/{Module}/` |

- Command/Query return `Model` (trong `Web.Contracts`), KHÔNG return `AppDto`
- DateTime trong Model/Command KHÔNG có hậu tố `Utc`
- Pagination trong SearchModel/Command dùng `PageNumber` (1-based)
- Handler chuyển đổi: datetime dùng `DateTimeHelper.ToUniversalTime()` / `.ToLocalTime()`

### View Model & Validator

- Fluent Validation: `Create{Entity}Validator : AbstractValidator<Create{Entity}Model>`
- DateTime trong View Model KHÔNG có hậu tố `Utc` (LocalTime)
- Pagination dùng `PageNumber`, KHÔNG dùng `PageIndex`

> Chi tiết và template đầy đủ: đọc `references/presentation-layer.md`

---

## Unit Testing (TDD)

- Framework: **xUnit** + **Moq**
- Test location: `Tests/NamEcommerce.{Project}.Test/`
- Naming: `{MethodName}_{Scenario}_{ExpectedResult}`

---

## Checklist khi tạo module mới

**Domain:**
- [ ] `Domain/Entities/{Module}/Xyz.cs` — sealed record, internal constructor, DateTime có hậu tố Utc
- [ ] `Domain.Shared/Dtos/{Module}/XyzDtos.cs` — Verify() throw exception
- [ ] `Domain.Shared/Services/{Module}/IXyzManager.cs`
- [ ] `Domain.Services/{Module}/XyzManager.cs` + unit tests
- [ ] `Domain.Services/Extensions/XyzExtensions.cs` — entity.ToDto()
- [ ] `AssemblyAccessibility.cs` — InternalsVisibleTo

**Application:**
- [ ] `Application.Contracts/Dtos/{Module}/XyzAppDtos.cs` — Validate() return (bool, string?), DateTime Utc suffix
- [ ] `Application.Contracts/{Module}/IXyzAppService.cs`
- [ ] `Application.Services/{Module}/XyzAppService.cs` + unit tests
- [ ] `Application.Services/Extensions/XyzExtensions.cs`

**Presentation:**
- [ ] `Web.Contracts/Models/{Module}/` — Model (DateTime không Utc, PageNumber không PageIndex)
- [ ] `Web.Contracts/Commands/Models/{Module}/` — Commands
- [ ] `Web.Contracts/Queries/Models/{Module}/` — Queries (PageIndex ok vì nội bộ)
- [ ] `Web.Framework/Commands/Handlers/{Module}/` — convert datetime + PageNumber→PageIndex
- [ ] `Web.Framework/Queries/Handlers/{Module}/` — convert datetime + map AppDto→Model
- [ ] `Web/Services/{Module}/` — IXyzModelFactory + XyzModelFactory
- [ ] `Web/Models/{Module}/` — View Models + Validators (DateTime không Utc, PageNumber)
- [ ] `Web/Controllers/XyzController.cs`

**Infrastructure:**
- [ ] EF Entity Configuration + DbSet
- [ ] **KHÔNG tạo migration**

---

## Modules hiện có

| Module | Entities |
|---|---|
| Catalog | Category, Product, UnitMeasurement, Vendor |
| Orders | Order, OrderItem |
| Inventory | InventoryStock, Warehouse, StockAuditLog, StockMovementLog |
| PurchaseOrders | PurchaseOrder, PurchaseOrderItem |
| Customers | Customer |
| Debts | CustomerDebt, CustomerPayment |
| DeliveryNotes | DeliveryNote, DeliveryNoteItem |
| Finance | Expense |
| Media | Picture |
| Users | User, Role, UserRole |
| Security | Permission, RolePermission |
