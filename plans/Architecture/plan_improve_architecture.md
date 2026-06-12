Dưới đây là toàn bộ nội dung văn bản được chuyển từ hình ảnh của bạn:

---

### 💀 Lỗ hổng nghiêm trọng

**1. Customer.Api hoàn toàn bypass UoW/Outbox**

**File:** `Customer/NameEcommerce.Customer.Api/Infrastructure/CustomerApiServiceCollectionExtensions.cs` line 123

```csharp
services.AddScoped(typeof(IRepository<>), typeof(NameEcommerceEfRepository<>));

```

**Ba vấn đề cộng dồn:**

* `NameEcommerceEfRepository` vẫn autosave (gọi `IDbContext.UpdateAsync` $\rightarrow$ `SaveChangesAsync` ngay)
* Không có `UnitOfWorkBehavior` trong MediatR pipeline của `Customer.Api`
* `DomainEventDispatchInterceptor` không được đăng ký trong `AddDbContext` của `Customer.Api`

**Hệ quả:** mọi write của `Customer.Api` autosave, domain events bị drop hoàn toàn (không vào outbox). Plan đánh dấu "skip" nhưng không document rõ – đây là gap thực sự nếu `Customer.Api` có flow quan trọng.

**2. IDbContext autosave methods vẫn còn sống (P3.2 pending)**

**File:** `NameEcommerceEfDbContext.cs` lines 56, 71, 79

`IDbContext.AddAsync/UpdateAsync/RemoveAsync` vẫn gọi `SaveChangesAsync` inline. Bất kỳ code mới nào vô tình resolve `IDbContext` thay vì `IRepository<>` sẽ bypass UoW mà không có lỗi compile.

---

### ⚠️ Vấn đề quan trọng

**3. UnitOfWorkBehavior dùng string-suffix check – dễ gãy**

**File:** `NameEcommerce.Web.Framework/Behaviors/UnitOfWorkBehavior.cs`

```csharp
if (requestName.EndsWith("Command"))
    await unitOfWork.CommitAsync(cancellationToken);

```

Command handler nào không đặt tên đúng convention `*Command` $\rightarrow$ staged writes không bao giờ được flush xuống DB, không có lỗi. Cần document rõ trong CLAUDE.md.

**4. StagedRepository.GetAllAsync trả deferred query**

**File:** `StagedRepository.cs`

```csharp
public Task<IEnumerable<TEntity>> GetAllAsync()
    => Task.FromResult<IEnumerable<TEntity>>(_context.Set<TEntity>().AsNoTracking());

await unitOfWork.CommitAsync(cancellationToken);

```

*(Đoạn code bên dưới lặp lại cấu trúc trên)*

**File:** `StagedRepository.cs`

```csharp
public Task<IEnumerable<TEntity>> GetAllAsync()
    => Task.FromResult<IEnumerable<TEntity>>(_context.Set<TEntity>().AsNoTracking());

```

Trả `IQueryable` bọc trong `IEnumerable` – query chưa materialize. Caller enumerate sau khi scope dispose $\rightarrow$ throw. Cần `.ToList()`.

**5. CLAUDE.md update chưa đầy đủ (P6 partial)**

Có đề cập outbox/load-for-write nhưng thiếu:

* Warning rõ ràng: `IEntityDataReader.GetByIdAsync` không dùng khi cần mutate entity