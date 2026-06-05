# Implementation Spec: AccountingSetup (PRE-1)

---

## 1. Domain Layer

### 1.1 Entity

**File:** `NamEcommerce.Domain/Entities/Finance/AccountingSetup.cs`

```csharp
namespace NamEcommerce.Domain.Entities.Finance;

[Serializable]
public sealed record AccountingSetup : AppAggregateEntity
{
    private AccountingSetup() : base(Guid.Empty) { }

    internal AccountingSetup(
        int fiscalYearStartMonth,
        int fiscalYearStartDay,
        DateTime accountingStartDate,
        decimal openingCash,
        decimal openingEquity,
        decimal defaultTaxRate) : base(Guid.NewGuid())
    {
        FiscalYearStartMonth = fiscalYearStartMonth;
        FiscalYearStartDay = fiscalYearStartDay;
        AccountingStartDate = accountingStartDate;
        OpeningCash = openingCash;
        OpeningEquity = openingEquity;
        DefaultTaxRate = defaultTaxRate;
        IsFinalized = false;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public int FiscalYearStartMonth { get; private set; }       // 1–12
    public int FiscalYearStartDay { get; private set; }         // 1–28
    public DateTime AccountingStartDate { get; private set; }
    public decimal OpeningCash { get; private set; }            // TK 111
    public decimal OpeningEquity { get; private set; }          // TK 411+421
    public decimal DefaultTaxRate { get; private set; }         // 0 / 0.05 / 0.08 / 0.10
    public decimal? CorporateTaxProvision { get; private set; } // TK 821 — luôn editable
    public bool IsFinalized { get; private set; }
    public DateTime? FinalizedOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    // Cho phép sửa trước khi Finalize
    internal void Update(
        int fiscalYearStartMonth, int fiscalYearStartDay,
        DateTime accountingStartDate,
        decimal openingCash, decimal openingEquity,
        decimal defaultTaxRate)
    {
        if (IsFinalized)
            throw new AccountingSetupAlreadyFinalizedException();

        FiscalYearStartMonth = fiscalYearStartMonth;
        FiscalYearStartDay = fiscalYearStartDay;
        AccountingStartDate = accountingStartDate;
        OpeningCash = openingCash;
        OpeningEquity = openingEquity;
        DefaultTaxRate = defaultTaxRate;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    // Khóa — chỉ gọi 1 lần
    internal void Finalize()
    {
        if (IsFinalized)
            throw new AccountingSetupAlreadyFinalizedException();

        IsFinalized = true;
        FinalizedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    // Luôn cho phép sửa (kể cả sau Finalize)
    internal void UpdateCorporateTaxProvision(decimal? amount)
    {
        if (amount.HasValue && amount.Value < 0)
            throw new AccountingSetupDataInvalidException("Error.Accounting.CorporateTaxProvisionCannotBeNegative");
        CorporateTaxProvision = amount;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void MarkCreated() => RaiseDomainEvent(new AccountingSetupCreated(Id));
}
```

---

### 1.2 Domain Exceptions

**File:** `NamEcommerce.Domain.Shared/Exceptions/Finance/AccountingSetupExceptions.cs`

```csharp
namespace NamEcommerce.Domain.Shared.Exceptions.Finance;

public sealed class AccountingSetupAlreadyFinalizedException()
    : NamEcommerceDomainException("Error.Accounting.SetupAlreadyFinalized");

public sealed class AccountingSetupNotFoundException()
    : NamEcommerceDomainException("Error.Accounting.SetupNotFound");

public sealed class AccountingSetupAlreadyExistsException()
    : NamEcommerceDomainException("Error.Accounting.SetupAlreadyExists");

public sealed class AccountingSetupDataInvalidException(string message)
    : NamEcommerceDomainException(message);
```

---

### 1.3 Domain Event

**File:** `NamEcommerce.Domain.Shared/Events/Finance/AccountingSetupEvents.cs`

```csharp
namespace NamEcommerce.Domain.Shared.Events.Finance;

public sealed record AccountingSetupCreated(Guid SetupId) : IDomainEvent;
```

---

### 1.4 Domain DTOs

**File:** `NamEcommerce.Domain.Shared/Dtos/Finance/AccountingSetupDtos.cs`

```csharp
namespace NamEcommerce.Domain.Shared.Dtos.Finance;

[Serializable]
public sealed record AccountingSetupDto
{
    public Guid Id { get; init; }
    public int FiscalYearStartMonth { get; init; }
    public int FiscalYearStartDay { get; init; }
    public DateTime AccountingStartDate { get; init; }
    public decimal OpeningCash { get; init; }
    public decimal OpeningEquity { get; init; }
    public decimal DefaultTaxRate { get; init; }
    public decimal? CorporateTaxProvision { get; init; }
    public bool IsFinalized { get; init; }
    public DateTime? FinalizedOnUtc { get; init; }
}

[Serializable]
public sealed record SaveAccountingSetupDto
{
    public int FiscalYearStartMonth { get; init; }
    public int FiscalYearStartDay { get; init; }
    public DateTime AccountingStartDate { get; init; }
    public decimal OpeningCash { get; init; }
    public decimal OpeningEquity { get; init; }
    public decimal DefaultTaxRate { get; init; }

    public void Verify()
    {
        if (FiscalYearStartMonth is < 1 or > 12)
            throw new AccountingSetupDataInvalidException("Error.Accounting.InvalidFiscalYearMonth");
        if (FiscalYearStartDay is < 1 or > 28)
            throw new AccountingSetupDataInvalidException("Error.Accounting.InvalidFiscalYearDay");
        if (OpeningCash < 0)
            throw new AccountingSetupDataInvalidException("Error.Accounting.OpeningCashCannotBeNegative");
        if (OpeningEquity < 0)
            throw new AccountingSetupDataInvalidException("Error.Accounting.OpeningEquityCannotBeNegative");
        if (DefaultTaxRate is not (0 or 0.05m or 0.08m or 0.10m))
            throw new AccountingSetupDataInvalidException("Error.Accounting.InvalidTaxRate");
    }
}
```

---

### 1.5 Manager Interface

**File:** `NamEcommerce.Domain.Shared/Services/Finance/IAccountingSetupManager.cs`

```csharp
namespace NamEcommerce.Domain.Shared.Services.Finance;

public interface IAccountingSetupManager
{
    /// <summary>Trả về AccountingSetup nếu đã cấu hình, null nếu chưa.</summary>
    Task<AccountingSetupDto?> GetAsync();

    /// <summary>Tạo mới nếu chưa có, cập nhật nếu chưa Finalize.</summary>
    Task<AccountingSetupDto> SaveAsync(SaveAccountingSetupDto dto);

    /// <summary>Khóa — không thể sửa sau khi gọi.</summary>
    Task FinalizeAsync();

    /// <summary>Luôn cho phép, kể cả sau khi Finalize.</summary>
    Task UpdateCorporateTaxProvisionAsync(decimal? amount);
}
```

---

### 1.6 Manager Implementation

**File:** `NamEcommerce.Domain.Services/Finance/AccountingSetupManager.cs`

```csharp
namespace NamEcommerce.Domain.Services.Finance;

public sealed class AccountingSetupManager : IAccountingSetupManager
{
    private readonly IRepository<AccountingSetup> _repository;
    private readonly IEntityDataReader<AccountingSetup> _dataReader;
    private readonly IEventPublisher _eventPublisher;

    public AccountingSetupManager(
        IRepository<AccountingSetup> repository,
        IEntityDataReader<AccountingSetup> dataReader,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _dataReader = dataReader;
        _eventPublisher = eventPublisher;
    }

    public async Task<AccountingSetupDto?> GetAsync()
    {
        var setup = await _dataReader.DataSource.FirstOrDefaultAsync();
        return setup?.ToDto();
    }

    public async Task<AccountingSetupDto> SaveAsync(SaveAccountingSetupDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var existing = await _dataReader.DataSource.FirstOrDefaultAsync();
        if (existing is null)
        {
            // Tạo mới
            var setup = new AccountingSetup(
                dto.FiscalYearStartMonth, dto.FiscalYearStartDay,
                dto.AccountingStartDate, dto.OpeningCash,
                dto.OpeningEquity, dto.DefaultTaxRate);
            setup.MarkCreated();
            var inserted = await _repository.InsertAsync(setup);
            await _eventPublisher.PublishAsync(inserted);
            return inserted.ToDto();
        }

        // Cập nhật (throws nếu đã Finalize)
        existing.Update(dto.FiscalYearStartMonth, dto.FiscalYearStartDay,
            dto.AccountingStartDate, dto.OpeningCash,
            dto.OpeningEquity, dto.DefaultTaxRate);
        var updated = await _repository.UpdateAsync(existing);
        return updated.ToDto();
    }

    public async Task FinalizeAsync()
    {
        var setup = await _dataReader.DataSource.FirstOrDefaultAsync()
            ?? throw new AccountingSetupNotFoundException();
        setup.Finalize();
        await _repository.UpdateAsync(setup);
    }

    public async Task UpdateCorporateTaxProvisionAsync(decimal? amount)
    {
        var setup = await _dataReader.DataSource.FirstOrDefaultAsync()
            ?? throw new AccountingSetupNotFoundException();
        setup.UpdateCorporateTaxProvision(amount);
        await _repository.UpdateAsync(setup);
    }
}
```

---

### 1.7 Extension ToDto()

**File:** `NamEcommerce.Domain.Services/Extensions/AccountingSetupExtensions.cs`

```csharp
public static class AccountingSetupExtensions
{
    public static AccountingSetupDto ToDto(this AccountingSetup s) => new()
    {
        Id = s.Id,
        FiscalYearStartMonth = s.FiscalYearStartMonth,
        FiscalYearStartDay = s.FiscalYearStartDay,
        AccountingStartDate = s.AccountingStartDate,
        OpeningCash = s.OpeningCash,
        OpeningEquity = s.OpeningEquity,
        DefaultTaxRate = s.DefaultTaxRate,
        CorporateTaxProvision = s.CorporateTaxProvision,
        IsFinalized = s.IsFinalized,
        FinalizedOnUtc = s.FinalizedOnUtc
    };
}
```

---

## 2. Application Layer

### 2.1 App DTOs

**File:** `NamEcommerce.Application.Contracts/Dtos/Finance/AccountingSetupAppDtos.cs`

```csharp
namespace NamEcommerce.Application.Contracts.Dtos.Finance;

[Serializable]
public sealed record AccountingSetupAppDto
{
    public Guid? Id { get; init; }
    public int FiscalYearStartMonth { get; init; }
    public int FiscalYearStartDay { get; init; }
    public DateTime AccountingStartDate { get; init; }
    public decimal OpeningCash { get; init; }
    public decimal OpeningEquity { get; init; }
    public decimal DefaultTaxRate { get; init; }
    public decimal? CorporateTaxProvision { get; init; }
    public bool IsFinalized { get; init; }
    public DateTime? FinalizedOnUtc { get; init; }
    public bool IsConfigured { get; init; }   // false nếu chưa setup lần nào
}

[Serializable]
public sealed record SaveAccountingSetupAppDto
{
    public int FiscalYearStartMonth { get; init; }
    public int FiscalYearStartDay { get; init; }
    public DateTime AccountingStartDate { get; init; }
    public decimal OpeningCash { get; init; }
    public decimal OpeningEquity { get; init; }
    public decimal DefaultTaxRate { get; init; }

    public (bool valid, string? error) Validate()
    {
        if (FiscalYearStartMonth is < 1 or > 12)
            return (false, "Error.Accounting.InvalidFiscalYearMonth");
        if (FiscalYearStartDay is < 1 or > 28)
            return (false, "Error.Accounting.InvalidFiscalYearDay");
        if (OpeningCash < 0) return (false, "Error.Accounting.OpeningCashCannotBeNegative");
        if (OpeningEquity < 0) return (false, "Error.Accounting.OpeningEquityCannotBeNegative");
        if (DefaultTaxRate is not (0 or 0.05m or 0.08m or 0.10m))
            return (false, "Error.Accounting.InvalidTaxRate");
        return (true, null);
    }
}

[Serializable]
public sealed record SaveAccountingSetupResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record UpdateCorporateTaxProvisionAppDto
{
    public decimal? Amount { get; init; }

    public (bool valid, string? error) Validate()
    {
        if (Amount.HasValue && Amount.Value < 0)
            return (false, "Error.Accounting.CorporateTaxProvisionCannotBeNegative");
        return (true, null);
    }
}
```

---

### 2.2 AppService Interface

**File:** `NamEcommerce.Application.Contracts/Finance/IAccountingSetupAppService.cs`

```csharp
namespace NamEcommerce.Application.Contracts.Finance;

public interface IAccountingSetupAppService
{
    Task<AccountingSetupAppDto> GetSetupAsync();
    Task<SaveAccountingSetupResultAppDto> SaveSetupAsync(SaveAccountingSetupAppDto dto);
    Task<SaveAccountingSetupResultAppDto> FinalizeSetupAsync();
    Task<SaveAccountingSetupResultAppDto> UpdateCorporateTaxProvisionAsync(UpdateCorporateTaxProvisionAppDto dto);
}
```

---

### 2.3 AppService Implementation

**File:** `NamEcommerce.Application.Services/Finance/AccountingSetupAppService.cs`

```csharp
public sealed class AccountingSetupAppService : IAccountingSetupAppService
{
    private readonly IAccountingSetupManager _manager;

    public AccountingSetupAppService(IAccountingSetupManager manager)
        => _manager = manager;

    public async Task<AccountingSetupAppDto> GetSetupAsync()
    {
        var dto = await _manager.GetAsync();
        if (dto is null)
            return new AccountingSetupAppDto { IsConfigured = false };
        return dto.ToAppDto() with { IsConfigured = true };
    }

    public async Task<SaveAccountingSetupResultAppDto> SaveSetupAsync(SaveAccountingSetupAppDto dto)
    {
        var (valid, error) = dto.Validate();
        if (!valid) return new SaveAccountingSetupResultAppDto { Success = false, ErrorMessage = error };

        try
        {
            await _manager.SaveAsync(dto.ToDomainDto());
            return new SaveAccountingSetupResultAppDto { Success = true };
        }
        catch (AccountingSetupAlreadyFinalizedException ex)
        {
            return new SaveAccountingSetupResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<SaveAccountingSetupResultAppDto> FinalizeSetupAsync()
    {
        try
        {
            await _manager.FinalizeAsync();
            return new SaveAccountingSetupResultAppDto { Success = true };
        }
        catch (AccountingSetupNotFoundException ex)
        {
            return new SaveAccountingSetupResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (AccountingSetupAlreadyFinalizedException ex)
        {
            return new SaveAccountingSetupResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<SaveAccountingSetupResultAppDto> UpdateCorporateTaxProvisionAsync(
        UpdateCorporateTaxProvisionAppDto dto)
    {
        var (valid, error) = dto.Validate();
        if (!valid) return new SaveAccountingSetupResultAppDto { Success = false, ErrorMessage = error };

        try
        {
            await _manager.UpdateCorporateTaxProvisionAsync(dto.Amount);
            return new SaveAccountingSetupResultAppDto { Success = true };
        }
        catch (AccountingSetupNotFoundException ex)
        {
            return new SaveAccountingSetupResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }
}
```

---

## 3. Presentation Layer

### 3.1 Queries & Commands

**File:** `NamEcommerce.Web.Contracts/Queries/Models/Accounting/GetAccountingSetupQuery.cs`

```csharp
public sealed class GetAccountingSetupQuery : IRequest<AccountingSetupModel> { }
```

**File:** `NamEcommerce.Web.Contracts/Commands/Models/Accounting/SaveAccountingSetupCommand.cs`

```csharp
public sealed class SaveAccountingSetupCommand : IRequest<CommandResultModel>
{
    public int FiscalYearStartMonth { get; init; }
    public int FiscalYearStartDay { get; init; }
    public DateTime AccountingStartDate { get; init; }
    public decimal OpeningCash { get; init; }
    public decimal OpeningEquity { get; init; }
    public decimal DefaultTaxRate { get; init; }
}

public sealed class FinalizeAccountingSetupCommand : IRequest<CommandResultModel> { }

public sealed class UpdateCorporateTaxProvisionCommand : IRequest<CommandResultModel>
{
    public decimal? Amount { get; init; }
}
```

---

### 3.2 Result Model

**File:** `NamEcommerce.Web.Contracts/Models/Accounting/AccountingSetupModel.cs`

```csharp
public sealed class AccountingSetupModel
{
    public bool IsConfigured { get; set; }
    public bool IsFinalized { get; set; }
    public int FiscalYearStartMonth { get; set; }
    public int FiscalYearStartDay { get; set; }
    public DateTime AccountingStartDate { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal OpeningEquity { get; set; }
    public decimal DefaultTaxRate { get; set; }
    public decimal? CorporateTaxProvision { get; set; }
    public DateTime? FinalizedOnUtc { get; set; }
}
```

---

### 3.3 Handlers

**File:** `NamEcommerce.Web.Framework/Queries/Handlers/Accounting/GetAccountingSetupHandler.cs`

```csharp
public sealed class GetAccountingSetupHandler : IRequestHandler<GetAccountingSetupQuery, AccountingSetupModel>
{
    private readonly IAccountingSetupAppService _appService;

    public GetAccountingSetupHandler(IAccountingSetupAppService appService)
        => _appService = appService;

    public async Task<AccountingSetupModel> Handle(GetAccountingSetupQuery request, CancellationToken ct)
    {
        var dto = await _appService.GetSetupAsync();
        return new AccountingSetupModel
        {
            IsConfigured = dto.IsConfigured,
            IsFinalized = dto.IsFinalized,
            FiscalYearStartMonth = dto.FiscalYearStartMonth,
            FiscalYearStartDay = dto.FiscalYearStartDay,
            AccountingStartDate = dto.AccountingStartDate,
            OpeningCash = dto.OpeningCash,
            OpeningEquity = dto.OpeningEquity,
            DefaultTaxRate = dto.DefaultTaxRate,
            CorporateTaxProvision = dto.CorporateTaxProvision,
            FinalizedOnUtc = dto.FinalizedOnUtc
        };
    }
}
```

**File:** `NamEcommerce.Web.Framework/Commands/Handlers/Accounting/AccountingSetupHandlers.cs`

```csharp
public sealed class SaveAccountingSetupHandler : IRequestHandler<SaveAccountingSetupCommand, CommandResultModel>
{
    private readonly IAccountingSetupAppService _appService;
    public SaveAccountingSetupHandler(IAccountingSetupAppService appService) => _appService = appService;

    public async Task<CommandResultModel> Handle(SaveAccountingSetupCommand request, CancellationToken ct)
    {
        var result = await _appService.SaveSetupAsync(new SaveAccountingSetupAppDto
        {
            FiscalYearStartMonth = request.FiscalYearStartMonth,
            FiscalYearStartDay = request.FiscalYearStartDay,
            AccountingStartDate = request.AccountingStartDate,
            OpeningCash = request.OpeningCash,
            OpeningEquity = request.OpeningEquity,
            DefaultTaxRate = request.DefaultTaxRate
        });
        return new CommandResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class FinalizeAccountingSetupHandler : IRequestHandler<FinalizeAccountingSetupCommand, CommandResultModel>
{ /* tương tự */ }

public sealed class UpdateCorporateTaxProvisionHandler : IRequestHandler<UpdateCorporateTaxProvisionCommand, CommandResultModel>
{ /* tương tự */ }
```

---

### 3.4 Controller

**File:** `NamEcommerce.Web/Controllers/AccountingController.cs`

```csharp
[Route("Accounting")]
public sealed class AccountingController : Controller
{
    private readonly IMediator _mediator;
    public AccountingController(IMediator mediator) => _mediator = mediator;

    [HttpGet("Setup")]
    public async Task<IActionResult> Setup()
    {
        var model = await _mediator.Send(new GetAccountingSetupQuery());
        return View(model);
    }

    [HttpPost("Setup/Save")]
    public async Task<IActionResult> Save(SaveAccountingSetupCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            TempData["Error"] = result.ErrorMessage;
        else
            TempData["Success"] = "Đã lưu khai báo kế toán.";
        return RedirectToAction(nameof(Setup));
    }

    [HttpPost("Setup/Finalize")]
    public async Task<IActionResult> Finalize()
    {
        var result = await _mediator.Send(new FinalizeAccountingSetupCommand());
        TempData[result.Success ? "Success" : "Error"] =
            result.Success ? "Đã xác nhận và khóa khai báo." : result.ErrorMessage;
        return RedirectToAction(nameof(Setup));
    }

    [HttpPost("Setup/UpdateProvision")]
    public async Task<IActionResult> UpdateProvision(UpdateCorporateTaxProvisionCommand command)
    {
        var result = await _mediator.Send(command);
        return Json(new { success = result.Success, message = result.ErrorMessage });
    }
}
```

---

### 3.5 View

**File:** `NamEcommerce.Web/Views/Accounting/Setup.cshtml`

```
@model AccountingSetupModel

Khi IsConfigured = false: hiển thị form nhập lần đầu (tất cả fields enabled)
Khi IsConfigured = true && IsFinalized = false: form có thể edit + nút "Xác nhận & Khóa"
Khi IsFinalized = true: hiển thị read-only, chỉ có form nhỏ để sửa CorporateTaxProvision

Fields:
- Tháng bắt đầu năm tài chính: select 1–12 (mặc định 1 = Tháng 1)
- Ngày bắt đầu: select 1–28 (mặc định 1)
- Ngày bắt đầu sử dụng hệ thống: date picker
- Tiền mặt quỹ đầu kỳ (TK 111): decimal input
- Vốn chủ sở hữu đầu kỳ (TK 411+421): decimal input
- Thuế GTGT mặc định: select 0% / 5% / 8% / 10%
- [Khi IsFinalized] Ước tính thuế TNDN kỳ này: decimal input + nút Cập nhật (AJAX)

Alert info: "Nợ phải thu/trả đầu kỳ → nhập tại Công nợ KH/NCC.
             Hàng tồn kho đầu kỳ → nhập tại Nhập hàng.
             Tài khoản ngân hàng → nhập tại mục Tài khoản NH."
```

---

## 4. Data Layer

### 4.1 EF Configuration

**File:** `NamEcommerce.Data.SqlServer/Configurations/Finance/AccountingSetupConfiguration.cs`

```csharp
public sealed class AccountingSetupConfiguration : IEntityTypeConfiguration<AccountingSetup>
{
    public void Configure(EntityTypeBuilder<AccountingSetup> builder)
    {
        builder.ToTable("AccountingSetup");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OpeningCash).HasColumnType("decimal(18,0)");
        builder.Property(x => x.OpeningEquity).HasColumnType("decimal(18,0)");
        builder.Property(x => x.DefaultTaxRate).HasColumnType("decimal(5,4)");
        builder.Property(x => x.CorporateTaxProvision).HasColumnType("decimal(18,0)");
        // Không dùng DB unique constraint cho singleton — Manager enforce
    }
}
```

### 4.2 Migration

**Migration name:** `AddAccountingSetupTable`

```csharp
migrationBuilder.CreateTable(
    name: "AccountingSetup",
    columns: table => new
    {
        Id = table.Column<Guid>(nullable: false),
        FiscalYearStartMonth = table.Column<int>(nullable: false, defaultValue: 1),
        FiscalYearStartDay = table.Column<int>(nullable: false, defaultValue: 1),
        AccountingStartDate = table.Column<DateTime>(nullable: false),
        OpeningCash = table.Column<decimal>(type: "decimal(18,0)", nullable: false, defaultValue: 0m),
        OpeningEquity = table.Column<decimal>(type: "decimal(18,0)", nullable: false, defaultValue: 0m),
        DefaultTaxRate = table.Column<decimal>(type: "decimal(5,4)", nullable: false, defaultValue: 0.10m),
        CorporateTaxProvision = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
        IsFinalized = table.Column<bool>(nullable: false, defaultValue: false),
        FinalizedOnUtc = table.Column<DateTime>(nullable: true),
        CreatedOnUtc = table.Column<DateTime>(nullable: false),
        UpdatedOnUtc = table.Column<DateTime>(nullable: true),
        IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
        DeletedOnUtc = table.Column<DateTime>(nullable: true)
    },
    constraints: table => table.PrimaryKey("PK_AccountingSetup", x => x.Id));
```
