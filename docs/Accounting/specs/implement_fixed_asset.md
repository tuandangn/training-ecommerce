# Implementation Spec: FixedAsset (PRE-7)

---

## Quyết định thiết kế

| Câu hỏi | Quyết định |
|---|---|
| Depreciation entry table | Không lưu — tính on-the-fly từ AcquisitionDate + UsefulLifeMonths (đường thẳng). Đủ cho TT200. |
| Qui ước tháng đầu KH | Theo TT200 điều 10: nếu mua **ngày 1** → tính từ tháng đó; mua từ **ngày 2 trở đi** → tính từ tháng kế tiếp. |
| Category → Cost center | User chọn khi tạo TSCĐ: **Selling** (641) hoặc **Admin** (642). Không tự suy ra từ Category. |
| Disposal | Khi thanh lý: set Status = Disposed, ghi nhận ngày. Book value còn lại ghi vào Expense loại mới `AssetDisposal`. Tiền thu thanh lý (nếu có) nhập thủ công qua CustomerPayment. |
| Revaluation | Ngoài phạm vi. |
| Multi-warehouse | Không track TSCĐ theo warehouse — chỉ theo entity. |

---

## 1. Domain Layer

### 1.1 Enums

**File:** `NamEcommerce.Domain.Shared/Enums/Finance/FixedAssetEnums.cs`

```csharp
namespace NamEcommerce.Domain.Shared.Enums.Finance;

public enum FixedAssetCategory
{
    Vehicle = 1,              // Xe tải, xe máy
    Equipment = 2,            // Máy móc thiết bị
    FurnitureAndFixtures = 3, // Kệ, bàn ghế, tủ
    Computer = 4,             // Máy tính, máy in
    Other = 5
}

public enum FixedAssetCostCenter
{
    Selling = 1,   // TK 641 — Chi phí bán hàng
    Admin = 2      // TK 642 — Chi phí quản lý doanh nghiệp
}

public enum FixedAssetStatus
{
    Active = 1,
    FullyDepreciated = 2,
    Disposed = 3
}
```

**Thêm vào `ExpenseType`:**

```csharp
// File: NamEcommerce.Domain.Shared/Enums/Finance/ExpenseType.cs
public enum ExpenseType
{
    Payroll = 1,
    Rent = 2,
    Marketing = 3,
    Utilities = 4,
    General = 5,
    ReturnCost = 6,
    AssetDisposal = 7   // ← THÊM MỚI: ghi nhận book value còn lại khi thanh lý TSCĐ
}
```

---

### 1.2 Entity

**File:** `NamEcommerce.Domain/Entities/Finance/FixedAsset.cs`

```csharp
namespace NamEcommerce.Domain.Entities.Finance;

[Serializable]
public sealed record FixedAsset : AppAggregateEntity
{
    private FixedAsset() : base(Guid.Empty) { }

    internal FixedAsset(
        string code,
        string name,
        string? description,
        FixedAssetCategory category,
        FixedAssetCostCenter costCenter,
        DateTime acquisitionDate,
        decimal acquisitionCost,
        decimal residualValue,
        int usefulLifeMonths,
        Guid? vendorId,
        string? vendorInvoiceNumber,
        string? note) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (acquisitionCost <= 0)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.AcquisitionCostMustBePositive");
        if (residualValue < 0 || residualValue >= acquisitionCost)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.ResidualValueInvalid");
        if (usefulLifeMonths <= 0)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.UsefulLifeMustBePositive");

        Code = code;
        Name = name;
        Description = description;
        Category = category;
        CostCenter = costCenter;
        AcquisitionDate = acquisitionDate;
        AcquisitionCost = acquisitionCost;
        ResidualValue = residualValue;
        UsefulLifeMonths = usefulLifeMonths;
        VendorId = vendorId;
        VendorInvoiceNumber = vendorInvoiceNumber;
        Note = note;
        Status = FixedAssetStatus.Active;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public FixedAssetCategory Category { get; private set; }
    public FixedAssetCostCenter CostCenter { get; private set; }   // Selling / Admin

    public DateTime AcquisitionDate { get; private set; }
    public decimal AcquisitionCost { get; private set; }           // Nguyên giá — TK 211
    public decimal ResidualValue { get; private set; }
    public int UsefulLifeMonths { get; private set; }

    public Guid? VendorId { get; private set; }
    public string? VendorInvoiceNumber { get; private set; }
    public string? Note { get; private set; }

    public FixedAssetStatus Status { get; private set; }
    public DateTime? DisposedOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    // ── Computed depreciation (không persist) ──────────────────────────────

    /// <summary>
    /// Ngày bắt đầu tính khấu hao.
    /// Nếu mua ngày 1 → từ tháng đó. Ngày 2+ → từ đầu tháng kế tiếp.
    /// </summary>
    public DateTime DepreciationStartDate =>
        AcquisitionDate.Day == 1
            ? new DateTime(AcquisitionDate.Year, AcquisitionDate.Month, 1)
            : new DateTime(AcquisitionDate.Year, AcquisitionDate.Month, 1).AddMonths(1);

    public decimal MonthlyDepreciation =>
        (AcquisitionCost - ResidualValue) / UsefulLifeMonths;

    /// <summary>Số tháng đã khấu hao tính đến thời điểm asOf (tối đa = UsefulLifeMonths).</summary>
    public int ElapsedDepreciationMonths(DateTime asOf)
    {
        var effectiveAsOf = Status == FixedAssetStatus.Disposed && DisposedOnUtc.HasValue
            ? DisposedOnUtc.Value
            : asOf;

        var start = DepreciationStartDate;
        if (effectiveAsOf < start) return 0;

        var months = (effectiveAsOf.Year - start.Year) * 12
                     + (effectiveAsOf.Month - start.Month) + 1;
        return Math.Min(months, UsefulLifeMonths);
    }

    /// <summary>Khấu hao lũy kế đến asOf — TK 214.</summary>
    public decimal GetAccumulatedDepreciation(DateTime asOf)
        => Math.Round(MonthlyDepreciation * ElapsedDepreciationMonths(asOf), 0);

    /// <summary>Giá trị còn lại đến asOf.</summary>
    public decimal GetBookValue(DateTime asOf)
        => AcquisitionCost - GetAccumulatedDepreciation(asOf);

    /// <summary>Khấu hao trong tháng cụ thể (dùng cho B02).</summary>
    public decimal GetDepreciationForMonth(int year, int month)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var start = DepreciationStartDate;

        if (monthEnd < start) return 0;
        if (Status == FixedAssetStatus.Disposed && DisposedOnUtc.HasValue && DisposedOnUtc.Value < monthStart) return 0;

        var elapsed = ElapsedDepreciationMonths(monthEnd);
        if (elapsed <= 0 || elapsed > UsefulLifeMonths) return 0;
        return Math.Round(MonthlyDepreciation, 0);
    }

    // ── State change methods ────────────────────────────────────────────────

    internal void UpdateInfo(string name, string? description, string? note,
        FixedAssetCostCenter costCenter)
    {
        if (Status == FixedAssetStatus.Disposed)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.CannotEditDisposed");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Description = description;
        Note = note;
        CostCenter = costCenter;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Dispose(DateTime disposedOnUtc)
    {
        if (Status == FixedAssetStatus.Disposed)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.AlreadyDisposed");
        Status = FixedAssetStatus.Disposed;
        DisposedOnUtc = disposedOnUtc;
        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new FixedAssetDisposed(Id, GetBookValue(disposedOnUtc)));
    }

    internal void CheckAndMarkFullyDepreciated(DateTime asOf)
    {
        if (Status == FixedAssetStatus.Active
            && ElapsedDepreciationMonths(asOf) >= UsefulLifeMonths)
        {
            Status = FixedAssetStatus.FullyDepreciated;
            UpdatedOnUtc = DateTime.UtcNow;
        }
    }

    internal void MarkCreated() => RaiseDomainEvent(new FixedAssetCreated(Id, Name));
}
```

---

### 1.3 Domain Exceptions

```csharp
// NamEcommerce.Domain.Shared/Exceptions/Finance/FixedAssetExceptions.cs

public sealed class FixedAssetNotFoundException(Guid id)
    : NamEcommerceDomainException($"Error.FixedAsset.NotFound:{id}");

public sealed class FixedAssetDataInvalidException(string message)
    : NamEcommerceDomainException(message);
```

---

### 1.4 Domain Events

```csharp
public sealed record FixedAssetCreated(Guid AssetId, string Name) : IDomainEvent;

// Raise khi thanh lý — Manager dùng để tự động tạo Expense(AssetDisposal)
public sealed record FixedAssetDisposed(Guid AssetId, decimal RemainingBookValue) : IDomainEvent;
```

---

### 1.5 Domain DTOs

**File:** `NamEcommerce.Domain.Shared/Dtos/Finance/FixedAssetDtos.cs`

```csharp
[Serializable]
public sealed record FixedAssetDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public FixedAssetCategory Category { get; init; }
    public FixedAssetCostCenter CostCenter { get; init; }
    public DateTime AcquisitionDate { get; init; }
    public decimal AcquisitionCost { get; init; }
    public decimal ResidualValue { get; init; }
    public int UsefulLifeMonths { get; init; }
    public decimal MonthlyDepreciation { get; init; }
    public string? VendorInvoiceNumber { get; init; }
    public string? Note { get; init; }
    public FixedAssetStatus Status { get; init; }
    public DateTime? DisposedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateFixedAssetDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public FixedAssetCategory Category { get; init; }
    public FixedAssetCostCenter CostCenter { get; init; }
    public DateTime AcquisitionDate { get; init; }
    public decimal AcquisitionCost { get; init; }
    public decimal ResidualValue { get; init; }
    public int UsefulLifeMonths { get; init; }
    public Guid? VendorId { get; init; }
    public string? VendorInvoiceNumber { get; init; }
    public string? Note { get; init; }

    public void Verify()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new FixedAssetDataInvalidException("Error.FixedAsset.NameRequired");
        if (AcquisitionCost <= 0)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.AcquisitionCostMustBePositive");
        if (ResidualValue < 0 || ResidualValue >= AcquisitionCost)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.ResidualValueInvalid");
        if (UsefulLifeMonths <= 0)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.UsefulLifeMustBePositive");
    }
}
```

---

### 1.6 Manager Interface

**File:** `NamEcommerce.Domain.Shared/Services/Finance/IFixedAssetManager.cs`

```csharp
public interface IFixedAssetManager
{
    Task<FixedAssetDto?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<FixedAssetDto>> GetAllAsync(FixedAssetStatus? status = null);
    Task<FixedAssetDto> CreateAsync(CreateFixedAssetDto dto);
    Task<FixedAssetDto> UpdateAsync(Guid id, string name, string? description, string? note, FixedAssetCostCenter costCenter);
    Task DisposeAsync(Guid id, DateTime disposedOnUtc);
    Task<string> GenerateCodeAsync();   // "TSCĐ-001", "TSCĐ-002"...
}
```

---

### 1.7 Manager Implementation (key methods)

**File:** `NamEcommerce.Domain.Services/Finance/FixedAssetManager.cs`

```csharp
public async Task<FixedAssetDto> CreateAsync(CreateFixedAssetDto dto)
{
    dto.Verify();
    var code = await GenerateCodeAsync();
    var asset = new FixedAsset(code, dto.Name, dto.Description, dto.Category,
        dto.CostCenter, dto.AcquisitionDate, dto.AcquisitionCost,
        dto.ResidualValue, dto.UsefulLifeMonths,
        dto.VendorId, dto.VendorInvoiceNumber, dto.Note);
    asset.MarkCreated();
    var inserted = await _repository.InsertAsync(asset);
    await _eventPublisher.PublishAsync(inserted);
    return inserted.ToDto();
}

public async Task DisposeAsync(Guid id, DateTime disposedOnUtc)
{
    var asset = await _dataReader.GetByIdAsync(id)
        ?? throw new FixedAssetNotFoundException(id);
    asset.Dispose(disposedOnUtc);   // raises FixedAssetDisposed event
    await _repository.UpdateAsync(asset);
    await _eventPublisher.PublishAsync(asset);
    // Event handler tự tạo Expense(AssetDisposal) với amount = asset.GetBookValue(disposedOnUtc)
}

public async Task<string> GenerateCodeAsync()
{
    var count = await _dataReader.DataSource.CountAsync();
    return $"TSCĐ-{(count + 1):D3}";
}
```

---

### 1.8 Domain Event Handler — Auto-create Expense on Disposal

**File:** `NamEcommerce.Domain.Services/Finance/FixedAssetDisposedHandler.cs`

```csharp
// Xử lý event FixedAssetDisposed → tạo Expense loại AssetDisposal
public sealed class FixedAssetDisposedHandler : IDomainEventHandler<FixedAssetDisposed>
{
    private readonly IExpenseManager _expenseManager;

    public async Task HandleAsync(FixedAssetDisposed @event)
    {
        if (@event.RemainingBookValue <= 0) return;

        await _expenseManager.CreateAsync(new CreateExpenseDto
        {
            Title = $"Thanh lý TSCĐ - giá trị còn lại",
            Amount = @event.RemainingBookValue,
            ExpenseType = ExpenseType.AssetDisposal,
            IncurredDate = DateTime.UtcNow
        });
    }
}
```

---

## 2. Application Layer

### 2.1 App DTOs

**File:** `NamEcommerce.Application.Contracts/Dtos/Finance/FixedAssetAppDtos.cs`

```csharp
[Serializable]
public sealed record FixedAssetAppDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public FixedAssetCategory Category { get; init; }
    public string CategoryDisplay { get; init; } = string.Empty;
    public FixedAssetCostCenter CostCenter { get; init; }
    public DateTime AcquisitionDate { get; init; }
    public decimal AcquisitionCost { get; init; }
    public decimal ResidualValue { get; init; }
    public int UsefulLifeMonths { get; init; }
    public decimal MonthlyDepreciation { get; init; }
    // Computed at query time:
    public decimal AccumulatedDepreciation { get; init; }
    public decimal BookValue { get; init; }
    public int RemainingMonths { get; init; }
    public FixedAssetStatus Status { get; init; }
    public DateTime? DisposedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateFixedAssetAppDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public FixedAssetCategory Category { get; init; }
    public FixedAssetCostCenter CostCenter { get; init; }
    public DateTime AcquisitionDate { get; init; }
    public decimal AcquisitionCost { get; init; }
    public decimal ResidualValue { get; init; }
    public int UsefulLifeMonths { get; init; }
    public string? VendorInvoiceNumber { get; init; }
    public string? Note { get; init; }

    public (bool valid, string? error) Validate()
    {
        if (string.IsNullOrWhiteSpace(Name)) return (false, "Error.FixedAsset.NameRequired");
        if (AcquisitionCost <= 0) return (false, "Error.FixedAsset.AcquisitionCostMustBePositive");
        if (ResidualValue < 0 || ResidualValue >= AcquisitionCost)
            return (false, "Error.FixedAsset.ResidualValueInvalid");
        if (UsefulLifeMonths <= 0) return (false, "Error.FixedAsset.UsefulLifeMustBePositive");
        return (true, null);
    }
}

[Serializable]
public sealed record FixedAssetOperationResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? AssetId { get; init; }
}

// Bảng khấu hao cho từng tháng — dùng trên trang chi tiết
[Serializable]
public sealed record DepreciationScheduleItemAppDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal DepreciationAmount { get; init; }
    public decimal CumulativeDepreciation { get; init; }
    public decimal BookValue { get; init; }
}
```

---

### 2.2 AppService Interface

**File:** `NamEcommerce.Application.Contracts/Finance/IFixedAssetAppService.cs`

```csharp
public interface IFixedAssetAppService
{
    Task<IReadOnlyList<FixedAssetAppDto>> GetFixedAssetsAsync(FixedAssetStatus? status = null);
    Task<FixedAssetAppDto?> GetFixedAssetByIdAsync(Guid id);
    Task<IReadOnlyList<DepreciationScheduleItemAppDto>> GetDepreciationScheduleAsync(Guid id);
    Task<FixedAssetOperationResultAppDto> CreateFixedAssetAsync(CreateFixedAssetAppDto dto);
    Task<FixedAssetOperationResultAppDto> UpdateFixedAssetAsync(Guid id, string name, string? description, string? note, FixedAssetCostCenter costCenter);
    Task<FixedAssetOperationResultAppDto> DisposeFixedAssetAsync(Guid id, DateTime disposedOnUtc);
}
```

---

### 2.3 AppService — GetDepreciationSchedule

```csharp
public async Task<IReadOnlyList<DepreciationScheduleItemAppDto>> GetDepreciationScheduleAsync(Guid id)
{
    var dto = await _manager.GetByIdAsync(id);
    if (dto is null) return [];

    // Tạo FixedAsset tạm để dùng computed methods — hoặc implement logic trực tiếp:
    var result = new List<DepreciationScheduleItemAppDto>();
    var start = dto.AcquisitionDate.Day == 1
        ? new DateTime(dto.AcquisitionDate.Year, dto.AcquisitionDate.Month, 1)
        : new DateTime(dto.AcquisitionDate.Year, dto.AcquisitionDate.Month, 1).AddMonths(1);
    var monthly = (dto.AcquisitionCost - dto.ResidualValue) / dto.UsefulLifeMonths;
    decimal cumulative = 0;

    for (int i = 0; i < dto.UsefulLifeMonths; i++)
    {
        var current = start.AddMonths(i);
        cumulative += Math.Round(monthly, 0);
        result.Add(new DepreciationScheduleItemAppDto
        {
            Year = current.Year,
            Month = current.Month,
            DepreciationAmount = Math.Round(monthly, 0),
            CumulativeDepreciation = cumulative,
            BookValue = dto.AcquisitionCost - cumulative
        });
    }
    return result;
}
```

---

## 3. Presentation Layer

### 3.1 Commands & Queries

```csharp
// Queries
public sealed class GetFixedAssetsQuery : IRequest<IReadOnlyList<FixedAssetModel>>
{
    public FixedAssetStatus? Status { get; init; }
}

// Commands
public sealed class CreateFixedAssetCommand : IRequest<CommandResultModel>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public FixedAssetCategory Category { get; init; }
    public FixedAssetCostCenter CostCenter { get; init; }
    public DateTime AcquisitionDate { get; init; }
    public decimal AcquisitionCost { get; init; }
    public decimal ResidualValue { get; init; }
    public int UsefulLifeMonths { get; init; }
    public string? VendorInvoiceNumber { get; init; }
    public string? Note { get; init; }
}

public sealed class DisposeFixedAssetCommand : IRequest<CommandResultModel>
{
    public required Guid Id { get; init; }
    public DateTime DisposedOnUtc { get; init; }
}
```

---

### 3.2 Controller (thêm vào AccountingController)

```csharp
[HttpGet("FixedAssets")]
public async Task<IActionResult> FixedAssets()
{
    var model = await _mediator.Send(new GetFixedAssetsQuery());
    return View(model);
}

[HttpPost("FixedAssets/Create")]
public async Task<IActionResult> CreateFixedAsset(CreateFixedAssetCommand command)
{
    var result = await _mediator.Send(command);
    return Json(new { success = result.Success, message = result.ErrorMessage });
}

[HttpPost("FixedAssets/{id:guid}/Dispose")]
public async Task<IActionResult> DisposeFixedAsset(Guid id, DateTime disposedOnUtc)
{
    var result = await _mediator.Send(new DisposeFixedAssetCommand { Id = id, DisposedOnUtc = disposedOnUtc });
    return Json(new { success = result.Success, message = result.ErrorMessage });
}
```

---

### 3.3 View Spec

**File:** `NamEcommerce.Web/Views/Accounting/FixedAssets.cshtml`

```
Hiển thị danh sách TSCĐ dạng table:

Cột: Mã | Tên | Loại | Ngày mua | Nguyên giá | KH/tháng | KH lũy kế | Còn lại | Trạng thái | Actions

Badge trạng thái:
  - Active → "Đang dùng" (success)
  - FullyDepreciated → "Hết KH" (secondary)
  - Disposed → "Đã thanh lý" (danger)

Filter: [Tất cả] [Đang dùng] [Hết KH] [Đã thanh lý]

Nút "Thêm TSCĐ" → modal form:
  - Tên TSCĐ *
  - Loại: Vehicle / Equipment / FurnitureAndFixtures / Computer / Other
  - Trung tâm chi phí: [Bán hàng (641)] [QLDN (642)]
  - Ngày mua *
  - Nguyên giá *
  - Giá trị thu hồi ước tính
  - Thời gian sử dụng (tháng) *
  - Số hóa đơn mua
  - Ghi chú
  → Preview tự động: "Khấu hao tháng: X.XXX đ, Bắt đầu từ: 01/MM/YYYY"

Chi tiết TSCĐ (click vào tên):
  → Hiển thị bảng lịch khấu hao đầy đủ (từng tháng)
  → Nút "Thanh lý" (chỉ hiện khi Active hoặc FullyDepreciated)

Modal Thanh lý:
  - Ngày thanh lý *
  - Thông báo: "Giá trị còn lại X đ sẽ được ghi nhận là chi phí"
  - Nút Xác nhận
```

---

## 4. Data Layer

### 4.1 EF Configuration

**File:** `NamEcommerce.Data.SqlServer/Configurations/Finance/FixedAssetConfiguration.cs`

```csharp
public sealed class FixedAssetConfiguration : IEntityTypeConfiguration<FixedAsset>
{
    public void Configure(EntityTypeBuilder<FixedAsset> builder)
    {
        builder.ToTable("FixedAssets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.AcquisitionCost).HasColumnType("decimal(18,0)");
        builder.Property(x => x.ResidualValue).HasColumnType("decimal(18,0)");
        builder.Property(x => x.Category).HasConversion<int>();
        builder.Property(x => x.CostCenter).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasIndex(x => x.Code).IsUnique();

        // MonthlyDepreciation, DepreciationStartDate, GetAccumulatedDepreciation, GetBookValue
        // là computed properties — KHÔNG map vào DB
        builder.Ignore(x => x.MonthlyDepreciation);
        builder.Ignore(x => x.DepreciationStartDate);
    }
}
```

### 4.2 Migration

```csharp
// AddFixedAssetTable
migrationBuilder.CreateTable(
    name: "FixedAssets",
    columns: table => new
    {
        Id = table.Column<Guid>(nullable: false),
        Code = table.Column<string>(maxLength: 20, nullable: false),
        Name = table.Column<string>(maxLength: 300, nullable: false),
        Description = table.Column<string>(maxLength: 1000, nullable: true),
        Category = table.Column<int>(nullable: false),
        CostCenter = table.Column<int>(nullable: false),
        AcquisitionDate = table.Column<DateTime>(nullable: false),
        AcquisitionCost = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
        ResidualValue = table.Column<decimal>(type: "decimal(18,0)", nullable: false, defaultValue: 0m),
        UsefulLifeMonths = table.Column<int>(nullable: false),
        VendorId = table.Column<Guid>(nullable: true),
        VendorInvoiceNumber = table.Column<string>(maxLength: 50, nullable: true),
        Note = table.Column<string>(maxLength: 500, nullable: true),
        Status = table.Column<int>(nullable: false, defaultValue: 1),   // Active
        DisposedOnUtc = table.Column<DateTime>(nullable: true),
        CreatedOnUtc = table.Column<DateTime>(nullable: false),
        UpdatedOnUtc = table.Column<DateTime>(nullable: true),
        IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
        DeletedOnUtc = table.Column<DateTime>(nullable: true)
    },
    constraints: table => table.PrimaryKey("PK_FixedAssets", x => x.Id));

migrationBuilder.CreateIndex("IX_FixedAssets_Code", "FixedAssets", "Code", unique: true);
```
