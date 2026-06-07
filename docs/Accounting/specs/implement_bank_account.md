# Implementation Spec: BankAccount (PRE-6)

---

## Quyết định thiết kế

| Câu hỏi | Quyết định |
|---|---|
| COD + BankAccountId | COD nghĩa là khách trả tiền mặt cho shipper → tiền cuối cùng vào tài khoản NH. `BankAccountId` là nullable với COD; nếu set thì ghi nhận vào TK đó, nếu null thì dùng TK mặc định |
| SetDefault | Manager xử lý: bỏ IsDefault tất cả accounts trước, rồi set account mới. Atomic trong 1 DB transaction |
| Deactivate | Không cho deactivate TK mặc định (phải đổi default trước) |
| OpeningBalance | Nhập khi tạo TK, không sửa sau khi tạo (thay vào đó dùng manual adjustment) |
| Soft delete | Dùng `IsActive = false` thay cho soft delete (tránh mất dữ liệu lịch sử) |

---

## 1. Domain Layer

### 1.1 Entity

**File:** `NamEcommerce.Domain/Entities/Finance/BankAccount.cs`

```csharp
namespace NamEcommerce.Domain.Entities.Finance;

[Serializable]
public sealed record BankAccount : AppAggregateEntity
{
    private BankAccount() : base(Guid.Empty) { }

    internal BankAccount(
        string code,
        string displayName,
        string bankName,
        string accountNumber,
        string accountHolderName,
        decimal openingBalance) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(bankName);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountHolderName);
        if (openingBalance < 0)
            throw new BankAccountDataInvalidException("Error.BankAccount.OpeningBalanceCannotBeNegative");

        Code = code;
        DisplayName = displayName;
        BankName = bankName;
        AccountNumber = accountNumber;
        AccountHolderName = accountHolderName;
        OpeningBalance = openingBalance;
        IsDefault = false;
        IsActive = true;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;      // "Vietcombank - CN Quận 1"
    public string BankName { get; private set; } = string.Empty;         // "Vietcombank"
    public string AccountNumber { get; private set; } = string.Empty;    // "1234567890"
    public string AccountHolderName { get; private set; } = string.Empty;
    public decimal OpeningBalance { get; private set; }                  // không sửa sau khi tạo
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    internal void UpdateInfo(string displayName, string bankName,
        string accountNumber, string accountHolderName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName;
        BankName = bankName;
        AccountNumber = accountNumber;
        AccountHolderName = accountHolderName;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    // Chỉ Manager gọi — sau khi đã clear default khỏi các account khác
    internal void SetAsDefault()
    {
        if (!IsActive)
            throw new BankAccountDataInvalidException("Error.BankAccount.CannotSetInactiveAsDefault");
        IsDefault = true;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void ClearDefault()
    {
        IsDefault = false;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Deactivate()
    {
        if (IsDefault)
            throw new BankAccountIsDefaultException();
        IsActive = false;
        IsDefault = false;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Activate()
    {
        IsActive = true;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void MarkCreated() => RaiseDomainEvent(new BankAccountCreated(Id, DisplayName));
}
```

---

### 1.2 Enums

Không cần enum mới — dùng `PaymentMethod` có sẵn trong `NamEcommerce.Domain.Shared.Enums.Orders`.

---

### 1.3 Domain Exceptions

**File:** `NamEcommerce.Domain.Shared/Exceptions/Finance/BankAccountExceptions.cs`

```csharp
public sealed class BankAccountNotFoundException(Guid id)
    : NamEcommerceDomainException($"Error.BankAccount.NotFound:{id}");

public sealed class BankAccountIsDefaultException()
    : NamEcommerceDomainException("Error.BankAccount.CannotDeactivateDefault");

public sealed class BankAccountDataInvalidException(string message)
    : NamEcommerceDomainException(message);

public sealed class NoBankAccountAvailableException()
    : NamEcommerceDomainException("Error.BankAccount.NoneAvailable");
```

---

### 1.4 Domain Event

```csharp
public sealed record BankAccountCreated(Guid AccountId, string DisplayName) : IDomainEvent;
```

---

### 1.5 Domain DTOs

**File:** `NamEcommerce.Domain.Shared/Dtos/Finance/BankAccountDtos.cs`

```csharp
[Serializable]
public sealed record BankAccountDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string BankName { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string AccountHolderName { get; init; } = string.Empty;
    public decimal OpeningBalance { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
}

[Serializable]
public sealed record CreateBankAccountDto
{
    public required string DisplayName { get; init; }
    public required string BankName { get; init; }
    public required string AccountNumber { get; init; }
    public required string AccountHolderName { get; init; }
    public decimal OpeningBalance { get; init; }
    public bool SetAsDefault { get; init; }

    public void Verify()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
            throw new BankAccountDataInvalidException("Error.BankAccount.DisplayNameRequired");
        if (string.IsNullOrWhiteSpace(AccountNumber))
            throw new BankAccountDataInvalidException("Error.BankAccount.AccountNumberRequired");
        if (OpeningBalance < 0)
            throw new BankAccountDataInvalidException("Error.BankAccount.OpeningBalanceCannotBeNegative");
    }
}

[Serializable]
public sealed record UpdateBankAccountDto
{
    public required Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public required string BankName { get; init; }
    public required string AccountNumber { get; init; }
    public required string AccountHolderName { get; init; }
}
```

---

### 1.6 Manager Interface

**File:** `NamEcommerce.Domain.Shared/Services/Finance/IBankAccountManager.cs`

```csharp
namespace NamEcommerce.Domain.Shared.Services.Finance;

public interface IBankAccountManager
{
    Task<BankAccountDto?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<BankAccountDto>> GetAllAsync(bool includeInactive = false);
    Task<BankAccountDto?> GetDefaultAsync();
    Task<BankAccountDto> CreateAsync(CreateBankAccountDto dto);
    Task<BankAccountDto> UpdateAsync(UpdateBankAccountDto dto);
    Task SetDefaultAsync(Guid id);
    Task DeactivateAsync(Guid id);
    Task ActivateAsync(Guid id);
    Task<string> GenerateCodeAsync();   // Tạo mã tự động: "NH-001", "NH-002"...
}
```

---

### 1.7 Manager Implementation

**File:** `NamEcommerce.Domain.Services/Finance/BankAccountManager.cs`

```csharp
public sealed class BankAccountManager : IBankAccountManager
{
    private readonly IRepository<BankAccount> _repository;
    private readonly IEntityDataReader<BankAccount> _dataReader;
    private readonly IEventPublisher _eventPublisher;

    // ... constructor

    public async Task<BankAccountDto> CreateAsync(CreateBankAccountDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var code = await GenerateCodeAsync();
        var account = new BankAccount(code, dto.DisplayName, dto.BankName,
            dto.AccountNumber, dto.AccountHolderName, dto.OpeningBalance);

        if (dto.SetAsDefault)
        {
            // Clear default on all others first
            var allAccounts = await _dataReader.DataSource
                .Where(x => x.IsDefault).ToListAsync();
            foreach (var a in allAccounts)
            {
                a.ClearDefault();
                await _repository.UpdateAsync(a);
            }
            account.SetAsDefault();
        }
        else if (!await _dataReader.DataSource.AnyAsync())
        {
            // First account → auto set as default
            account.SetAsDefault();
        }

        account.MarkCreated();
        var inserted = await _repository.InsertAsync(account);
        await _eventPublisher.PublishAsync(inserted);
        return inserted.ToDto();
    }

    public async Task SetDefaultAsync(Guid id)
    {
        var account = await _dataReader.GetByIdAsync(id)
            ?? throw new BankAccountNotFoundException(id);

        var currentDefault = await _dataReader.DataSource
            .FirstOrDefaultAsync(x => x.IsDefault && x.Id != id);
        if (currentDefault is not null)
        {
            currentDefault.ClearDefault();
            await _repository.UpdateAsync(currentDefault);
        }

        account.SetAsDefault();
        await _repository.UpdateAsync(account);
    }

    public async Task DeactivateAsync(Guid id)
    {
        var account = await _dataReader.GetByIdAsync(id)
            ?? throw new BankAccountNotFoundException(id);
        account.Deactivate();   // throws BankAccountIsDefaultException if IsDefault
        await _repository.UpdateAsync(account);
    }

    public async Task<string> GenerateCodeAsync()
    {
        var count = await _dataReader.DataSource.CountAsync();
        return $"NH-{(count + 1):D3}";
    }
}
```

---

## 2. Application Layer

### 2.1 App DTOs

**File:** `NamEcommerce.Application.Contracts/Dtos/Finance/BankAccountAppDtos.cs`

```csharp
[Serializable]
public sealed record BankAccountAppDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string BankName { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string AccountHolderName { get; init; } = string.Empty;
    public decimal OpeningBalance { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    // Computed khi prepare model (không trong entity):
    public decimal CurrentBalance { get; init; }
}

[Serializable]
public sealed record CreateBankAccountAppDto
{
    public required string DisplayName { get; init; }
    public required string BankName { get; init; }
    public required string AccountNumber { get; init; }
    public required string AccountHolderName { get; init; }
    public decimal OpeningBalance { get; init; }
    public bool SetAsDefault { get; init; }

    public (bool valid, string? error) Validate()
    {
        if (string.IsNullOrWhiteSpace(DisplayName)) return (false, "Error.BankAccount.DisplayNameRequired");
        if (string.IsNullOrWhiteSpace(AccountNumber)) return (false, "Error.BankAccount.AccountNumberRequired");
        if (OpeningBalance < 0) return (false, "Error.BankAccount.OpeningBalanceCannotBeNegative");
        return (true, null);
    }
}

[Serializable]
public sealed record UpdateBankAccountAppDto
{
    public required Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public required string BankName { get; init; }
    public required string AccountNumber { get; init; }
    public required string AccountHolderName { get; init; }
}

[Serializable]
public sealed record BankAccountOperationResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? AccountId { get; init; }
}
```

---

### 2.2 AppService Interface

**File:** `NamEcommerce.Application.Contracts/Finance/IBankAccountAppService.cs`

```csharp
public interface IBankAccountAppService
{
    Task<IReadOnlyList<BankAccountAppDto>> GetBankAccountsAsync(bool includeInactive = false);
    Task<BankAccountAppDto?> GetBankAccountByIdAsync(Guid id);
    Task<BankAccountOperationResultAppDto> CreateBankAccountAsync(CreateBankAccountAppDto dto);
    Task<BankAccountOperationResultAppDto> UpdateBankAccountAsync(UpdateBankAccountAppDto dto);
    Task<BankAccountOperationResultAppDto> SetDefaultBankAccountAsync(Guid id);
    Task<BankAccountOperationResultAppDto> DeactivateBankAccountAsync(Guid id);
    Task<BankAccountOperationResultAppDto> ActivateBankAccountAsync(Guid id);
}
```

> `CurrentBalance` trong `BankAccountAppDto` được tính trong `CashBookAppService` hoặc một `ICashBalanceCalculator` service riêng (xem spec Report Service).

---

## 3. Presentation Layer

### 3.1 Commands & Queries

```csharp
// Queries
public sealed class GetBankAccountsQuery : IRequest<IReadOnlyList<BankAccountModel>>
{
    public bool IncludeInactive { get; init; }
}

// Commands
public sealed class CreateBankAccountCommand : IRequest<CommandResultModel>
{
    public required string DisplayName { get; init; }
    public required string BankName { get; init; }
    public required string AccountNumber { get; init; }
    public required string AccountHolderName { get; init; }
    public decimal OpeningBalance { get; init; }
    public bool SetAsDefault { get; init; }
}

public sealed class UpdateBankAccountCommand : IRequest<CommandResultModel>
{
    public required Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public required string BankName { get; init; }
    public required string AccountNumber { get; init; }
    public required string AccountHolderName { get; init; }
}

public sealed class SetDefaultBankAccountCommand : IRequest<CommandResultModel>
{
    public required Guid Id { get; init; }
}

public sealed class DeactivateBankAccountCommand : IRequest<CommandResultModel>
{
    public required Guid Id { get; init; }
}
```

---

### 3.2 Controller

**File:** `NamEcommerce.Web/Controllers/AccountingController.cs` (thêm vào file hiện có)

```csharp
// GET /Accounting/BankAccounts
[HttpGet("BankAccounts")]
public async Task<IActionResult> BankAccounts()
{
    var model = await _mediator.Send(new GetBankAccountsQuery { IncludeInactive = true });
    return View(model);
}

// POST /Accounting/BankAccounts/Create  (form post hoặc AJAX)
[HttpPost("BankAccounts/Create")]
public async Task<IActionResult> CreateBankAccount(CreateBankAccountCommand command)
{
    var result = await _mediator.Send(command);
    return Json(new { success = result.Success, message = result.ErrorMessage });
}

// POST /Accounting/BankAccounts/Update
[HttpPost("BankAccounts/Update")]
public async Task<IActionResult> UpdateBankAccount(UpdateBankAccountCommand command)
{
    var result = await _mediator.Send(command);
    return Json(new { success = result.Success, message = result.ErrorMessage });
}

// POST /Accounting/BankAccounts/{id}/SetDefault
[HttpPost("BankAccounts/{id:guid}/SetDefault")]
public async Task<IActionResult> SetDefaultBankAccount(Guid id)
{
    var result = await _mediator.Send(new SetDefaultBankAccountCommand { Id = id });
    return Json(new { success = result.Success, message = result.ErrorMessage });
}

// POST /Accounting/BankAccounts/{id}/Deactivate
[HttpPost("BankAccounts/{id:guid}/Deactivate")]
public async Task<IActionResult> DeactivateBankAccount(Guid id)
{
    var result = await _mediator.Send(new DeactivateBankAccountCommand { Id = id });
    return Json(new { success = result.Success, message = result.ErrorMessage });
}
```

---

### 3.3 View Spec

**File:** `NamEcommerce.Web/Views/Accounting/BankAccounts.cshtml`

```
Hiển thị danh sách tài khoản ngân hàng dạng card/table:

Mỗi TK hiển thị:
  - Tên hiển thị (DisplayName)                   [e.g. "Vietcombank - CN Q1"]
  - Số tài khoản + Chủ TK                        [masked: "****7890 - Cty TNHH..."]
  - Badge "Mặc định" nếu IsDefault
  - Badge "Ngừng" nếu !IsActive
  - Số dư đầu kỳ
  - Số dư hiện tại (computed, lấy từ CurrentBalance)
  - Buttons: [Sửa] [Đặt làm mặc định] [Ngừng sử dụng]

Nút "Thêm tài khoản" → mở modal:
  Form: Tên NH | Tên hiển thị | Số TK | Chủ TK | Số dư đầu kỳ | checkbox "Đặt làm mặc định"
```

---

## 4. Link BankAccountId vào Payment Entities

### Migration

```csharp
// AddBankAccountIdToPayments
migrationBuilder.AddColumn<Guid>("BankAccountId", "CustomerPayments", nullable: true);
migrationBuilder.AddColumn<Guid>("BankAccountId", "VendorPayments", nullable: true);
migrationBuilder.AddColumn<Guid>("BankAccountId", "CustomerRefunds", nullable: true);
// Expense đã có trong PRE-4c migration
```

### UI change — CustomerPayment

Khi PaymentMethod = BankTransfer hoặc COD, hiển thị dropdown chọn TK ngân hàng:
- Load từ `/Accounting/BankAccounts` (active only)
- Default = TK mặc định
- Nếu null → ghi nhận vào TK mặc định khi tính sổ quỹ

---

## 5. EF Configuration

**File:** `NamEcommerce.Data.SqlServer/Configurations/Finance/BankAccountConfiguration.cs`

```csharp
public sealed class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BankName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AccountNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.AccountHolderName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OpeningBalance).HasColumnType("decimal(18,0)");
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
```

### Migration Table Creation

```csharp
// AddBankAccountTable
migrationBuilder.CreateTable(
    name: "BankAccounts",
    columns: table => new
    {
        Id = table.Column<Guid>(nullable: false),
        Code = table.Column<string>(maxLength: 20, nullable: false),
        DisplayName = table.Column<string>(maxLength: 200, nullable: false),
        BankName = table.Column<string>(maxLength: 100, nullable: false),
        AccountNumber = table.Column<string>(maxLength: 30, nullable: false),
        AccountHolderName = table.Column<string>(maxLength: 200, nullable: false),
        OpeningBalance = table.Column<decimal>(type: "decimal(18,0)", nullable: false, defaultValue: 0m),
        IsDefault = table.Column<bool>(nullable: false, defaultValue: false),
        IsActive = table.Column<bool>(nullable: false, defaultValue: true),
        CreatedOnUtc = table.Column<DateTime>(nullable: false),
        UpdatedOnUtc = table.Column<DateTime>(nullable: true),
        IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
        DeletedOnUtc = table.Column<DateTime>(nullable: true)
    },
    constraints: table => table.PrimaryKey("PK_BankAccounts", x => x.Id));

migrationBuilder.CreateIndex("IX_BankAccounts_Code", "BankAccounts", "Code", unique: true);
```
