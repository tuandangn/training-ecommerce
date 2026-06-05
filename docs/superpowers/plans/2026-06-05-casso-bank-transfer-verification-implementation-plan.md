# Casso Bank Transfer Verification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Casso webhook and Casso transaction reconciliation so Fast Sale VietQR payment intents can be confirmed automatically through real bank transaction data.

**Architecture:** Keep Phase 2 as the accounting boundary: Casso transactions are mapped into `ProcessBankTransferProviderTransactionAsync(...)`, and that existing boundary owns intent matching, duplicate detection, confirmation, and verification logs. Phase 3 adds Casso-specific settings, DTOs, mapper, API client, webhook endpoint, manual reconciliation endpoint, scheduled worker, and run metadata without changing Fast Sale order creation.

**Tech Stack:** ASP.NET Core MVC, MediatR, EF Core SQL Server mappings and migrations, hosted services, typed `HttpClient`, `System.Text.Json`.

---

## Assumptions

- Automated tests remain out of execution scope because the user explicitly asked to skip tests for now. This plan uses build checks and manual HTTP smoke checks as the verification gate.
- The app runs as a single web instance. The scheduled reconciliation worker uses an in-process `SemaphoreSlim`; a database lock is not part of this phase.
- Production secrets are provided by deployment configuration. Committed config files keep `ApiKey` and `WebhookSecurityKey` empty.
- Casso transaction API endpoint is `GET https://oauth.casso.vn/v2/transactions` with `Authorization` using the `Apikey` scheme and the configured `Payments:BankTransfer:Casso:ApiKey` value.
- Casso webhook payload uses a `data` array. Each transaction `id` is the provider idempotency key.

## File Map

**Settings and configuration**

- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Settings/BankTransferPaymentSettings.cs`
  - Add `CassoPaymentSettings` under the existing `BankTransferPaymentSettings`.
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/appsettings.json`
  - Add committed Casso defaults with empty secrets.
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/appsettings.Development.json`
  - Add matching local defaults with empty secrets.

**Run metadata**

- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/Debts/CassoReconciliationRunTrigger.cs`
  - Stores `Manual` and `Scheduled` trigger values.
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Debts/CassoReconciliationRunDtos.cs`
  - DTOs for creating and reading reconciliation run summaries.
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Debts/ICassoReconciliationRunManager.cs`
  - Domain manager contract for starting/completing/failing runs.
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/CassoReconciliationRun.cs`
  - Aggregate storing run-level operational metadata.
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Debts/CassoReconciliationRunManager.cs`
  - Manager for persistence and DTO mapping.
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CassoReconciliationRunMapping.cs`
  - EF table mapping for `CassoReconciliationRuns`.
- Create: `NamEcommerce/Migrations/NamEcommerce.Data.SqlServerMigrations/Migrations/20260605103000_AddCassoReconciliationRun.cs`
  - SQL Server migration for the run metadata table.
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`
  - Register `ICassoReconciliationRunManager`.

**Application Casso integration**

- Create: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Debts/ICassoBankTransferAppService.cs`
  - Public application service boundary for webhook and reconciliation.
- Create: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Debts/ICassoTransactionClient.cs`
  - Casso transaction API client boundary.
- Create: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Debts/CassoBankTransferAppDtos.cs`
  - Casso webhook, transaction, reconciliation request, and summary DTOs.
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/Debts/CassoTransactionMapper.cs`
  - Maps Casso transaction records to the normalized Phase 2 provider transaction DTO.
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/Debts/CassoTransactionClient.cs`
  - Typed `HttpClient` implementation for `GET /v2/transactions`.
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/Debts/CassoBankTransferAppService.cs`
  - Processes webhook records and reconciliation pages through the mapper and `IBankTransferPaymentIntentAppService`.
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`
  - Register Casso app service, mapper, and typed client.

**Presentation endpoints and worker**

- Create: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/Debts/CassoBankTransferCommands.cs`
  - MediatR commands for webhook processing and manual reconciliation.
- Create: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/Debts/CassoBankTransferResultModels.cs`
  - Response models for webhook/manual run summaries.
- Create: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/Debts/CassoBankTransferCommandHandlers.cs`
  - Maps web commands into app DTOs.
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/CassoBankTransferController.cs`
  - `POST /api/casso/webhook` and `POST /api/casso/reconciliation/run`.
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Services/Debts/CassoReconciliationHostedService.cs`
  - Scheduled reconciliation worker.
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`
  - Register hosted service.

---

### Task 1: Add Casso Settings

**Files:**
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Settings/BankTransferPaymentSettings.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/appsettings.json`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/appsettings.Development.json`

- [ ] **Step 1: Add Casso settings classes**

In `BankTransferPaymentSettings.cs`, add a property to `BankTransferPaymentSettings`:

```csharp
public CassoPaymentSettings Casso { get; init; } = new();
```

In the same file, add this class below `BankTransferVerificationSettings`:

```csharp
[Serializable]
public sealed class CassoPaymentSettings
{
    public bool Enabled { get; init; }
    public string ApiBaseUrl { get; init; } = "https://oauth.casso.vn";
    public string ApiKey { get; init; } = string.Empty;
    public bool WebhookEnabled { get; init; }
    public string WebhookSecurityHeaderName { get; init; } = "X-NamEcommerce-Casso-Token";
    public string WebhookSecurityKey { get; init; } = string.Empty;
    public bool ReconciliationEnabled { get; init; }
    public int ReconciliationIntervalMinutes { get; init; } = 15;
    public int ReconciliationLookbackMinutes { get; init; } = 180;
    public int ReconciliationPageSize { get; init; } = 50;
}
```

- [ ] **Step 2: Add JSON defaults**

In both `appsettings.json` and `appsettings.Development.json`, add this `Casso` block beside the existing `Webhook` and `Verification` blocks under `Payments:BankTransfer`:

```json
"Casso": {
  "Enabled": false,
  "ApiBaseUrl": "https://oauth.casso.vn",
  "ApiKey": "",
  "WebhookEnabled": false,
  "WebhookSecurityHeaderName": "X-NamEcommerce-Casso-Token",
  "WebhookSecurityKey": "",
  "ReconciliationEnabled": false,
  "ReconciliationIntervalMinutes": 15,
  "ReconciliationLookbackMinutes": 180,
  "ReconciliationPageSize": 50
}
```

- [ ] **Step 3: Verify settings compile**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build completes with `0 Error(s)`.

- [ ] **Step 4: Commit settings**

Run:

```powershell
rtk git add NamEcommerce\Domain\NamEcommerce.Domain.Shared\Settings\BankTransferPaymentSettings.cs NamEcommerce\Presentation\NamEcommerce.Web\appsettings.json NamEcommerce\Presentation\NamEcommerce.Web\appsettings.Development.json
rtk git commit -m "feat: add casso bank transfer settings"
```

---

### Task 2: Add Casso Reconciliation Run Metadata

**Files:**
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/Debts/CassoReconciliationRunTrigger.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Debts/CassoReconciliationRunDtos.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Debts/ICassoReconciliationRunManager.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/CassoReconciliationRun.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Debts/CassoReconciliationRunManager.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CassoReconciliationRunMapping.cs`
- Create: `NamEcommerce/Migrations/NamEcommerce.Data.SqlServerMigrations/Migrations/20260605103000_AddCassoReconciliationRun.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`

- [ ] **Step 1: Add trigger enum**

Create `CassoReconciliationRunTrigger.cs`:

```csharp
namespace NamEcommerce.Domain.Shared.Enums.Debts;

public enum CassoReconciliationRunTrigger
{
    Manual = 10,
    Scheduled = 20
}
```

- [ ] **Step 2: Add run DTOs**

Create `CassoReconciliationRunDtos.cs`:

```csharp
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.Debts;

[Serializable]
public sealed record StartCassoReconciliationRunDto
{
    public required DateTime FromDate { get; init; }
    public required DateTime ToDate { get; init; }
    public required CassoReconciliationRunTrigger Trigger { get; init; }

    public void Verify()
    {
        if (FromDate > ToDate)
            throw new NamEcommerceDomainException("Error.CassoReconciliationDateRangeInvalid");
        if (!Enum.IsDefined(Trigger))
            throw new NamEcommerceDomainException("Error.CassoReconciliationTriggerInvalid");
    }
}

[Serializable]
public sealed record CompleteCassoReconciliationRunDto
{
    public required Guid RunId { get; init; }
    public int TotalRecords { get; init; }
    public int Processed { get; init; }
    public int Matched { get; init; }
    public int Duplicate { get; init; }
    public int Rejected { get; init; }
    public int Ignored { get; init; }
    public int Failed { get; init; }
}

[Serializable]
public sealed record FailCassoReconciliationRunDto
{
    public required Guid RunId { get; init; }
    public required string ErrorMessage { get; init; }
}

[Serializable]
public sealed record CassoReconciliationRunDto(Guid Id)
{
    public required DateTime StartedAtUtc { get; init; }
    public DateTime? FinishedAtUtc { get; init; }
    public required DateTime FromDate { get; init; }
    public required DateTime ToDate { get; init; }
    public required CassoReconciliationRunTrigger Trigger { get; init; }
    public int TotalRecords { get; init; }
    public int Processed { get; init; }
    public int Matched { get; init; }
    public int Duplicate { get; init; }
    public int Rejected { get; init; }
    public int Ignored { get; init; }
    public int Failed { get; init; }
    public string? ErrorMessage { get; init; }
}
```

- [ ] **Step 3: Add manager contract**

Create `ICassoReconciliationRunManager.cs`:

```csharp
using NamEcommerce.Domain.Shared.Dtos.Debts;

namespace NamEcommerce.Domain.Shared.Services.Debts;

public interface ICassoReconciliationRunManager
{
    Task<CassoReconciliationRunDto> StartAsync(StartCassoReconciliationRunDto dto);
    Task<CassoReconciliationRunDto> CompleteAsync(CompleteCassoReconciliationRunDto dto);
    Task<CassoReconciliationRunDto> FailAsync(FailCassoReconciliationRunDto dto);
}
```

- [ ] **Step 4: Add run entity**

Create `CassoReconciliationRun.cs`:

```csharp
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record CassoReconciliationRun : AppAggregateEntity
{
    private CassoReconciliationRun() : base(Guid.NewGuid()) { }

    internal CassoReconciliationRun(DateTime fromDate, DateTime toDate, CassoReconciliationRunTrigger trigger)
        : base(Guid.NewGuid())
    {
        if (fromDate > toDate)
            throw new NamEcommerceDomainException("Error.CassoReconciliationDateRangeInvalid");
        if (!Enum.IsDefined(trigger))
            throw new NamEcommerceDomainException("Error.CassoReconciliationTriggerInvalid");

        StartedAtUtc = DateTime.UtcNow;
        FromDate = fromDate;
        ToDate = toDate;
        Trigger = trigger;
    }

    public DateTime StartedAtUtc { get; private set; }
    public DateTime? FinishedAtUtc { get; private set; }
    public DateTime FromDate { get; private set; }
    public DateTime ToDate { get; private set; }
    public CassoReconciliationRunTrigger Trigger { get; private set; }
    public int TotalRecords { get; private set; }
    public int Processed { get; private set; }
    public int Matched { get; private set; }
    public int Duplicate { get; private set; }
    public int Rejected { get; private set; }
    public int Ignored { get; private set; }
    public int Failed { get; private set; }
    public string? ErrorMessage { get; private set; }

    internal void Complete(
        int totalRecords,
        int processed,
        int matched,
        int duplicate,
        int rejected,
        int ignored,
        int failed,
        DateTime finishedAtUtc)
    {
        TotalRecords = totalRecords;
        Processed = processed;
        Matched = matched;
        Duplicate = duplicate;
        Rejected = rejected;
        Ignored = ignored;
        Failed = failed;
        FinishedAtUtc = finishedAtUtc;
    }

    internal void Fail(string errorMessage, DateTime finishedAtUtc)
    {
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Error.CassoReconciliationFailed" : errorMessage;
        Failed += 1;
        FinishedAtUtc = finishedAtUtc;
    }
}
```

- [ ] **Step 5: Add run manager**

Create `CassoReconciliationRunManager.cs`:

```csharp
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Domain.Services.Debts;

public sealed class CassoReconciliationRunManager(
    IRepository<CassoReconciliationRun> runRepository) : ICassoReconciliationRunManager
{
    public async Task<CassoReconciliationRunDto> StartAsync(StartCassoReconciliationRunDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var run = new CassoReconciliationRun(dto.FromDate, dto.ToDate, dto.Trigger);
        var inserted = await runRepository.InsertAsync(run).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<CassoReconciliationRunDto> CompleteAsync(CompleteCassoReconciliationRunDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var run = await GetRunAsync(dto.RunId).ConfigureAwait(false);
        run.Complete(dto.TotalRecords, dto.Processed, dto.Matched, dto.Duplicate, dto.Rejected, dto.Ignored, dto.Failed, DateTime.UtcNow);
        var updated = await runRepository.UpdateAsync(run).ConfigureAwait(false);
        return MapToDto(updated);
    }

    public async Task<CassoReconciliationRunDto> FailAsync(FailCassoReconciliationRunDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var run = await GetRunAsync(dto.RunId).ConfigureAwait(false);
        run.Fail(dto.ErrorMessage, DateTime.UtcNow);
        var updated = await runRepository.UpdateAsync(run).ConfigureAwait(false);
        return MapToDto(updated);
    }

    private async Task<CassoReconciliationRun> GetRunAsync(Guid id)
        => await runRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CassoReconciliationRunIsNotFound");

    private static CassoReconciliationRunDto MapToDto(CassoReconciliationRun run)
        => new(run.Id)
        {
            StartedAtUtc = run.StartedAtUtc,
            FinishedAtUtc = run.FinishedAtUtc,
            FromDate = run.FromDate,
            ToDate = run.ToDate,
            Trigger = run.Trigger,
            TotalRecords = run.TotalRecords,
            Processed = run.Processed,
            Matched = run.Matched,
            Duplicate = run.Duplicate,
            Rejected = run.Rejected,
            Ignored = run.Ignored,
            Failed = run.Failed,
            ErrorMessage = run.ErrorMessage
        };
}
```

- [ ] **Step 6: Add EF mapping**

Create `CassoReconciliationRunMapping.cs`:

```csharp
using NamEcommerce.Domain.Entities.Debts;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class CassoReconciliationRunMapping : IEntityTypeConfiguration<CassoReconciliationRun>
{
    public void Configure(EntityTypeBuilder<CassoReconciliationRun> builder)
    {
        builder.ToTable("CassoReconciliationRuns", DbScheme);
        builder.HasKey(run => run.Id);

        builder.Property(run => run.StartedAtUtc).IsRequired();
        builder.Property(run => run.FinishedAtUtc).IsRequired(false);
        builder.Property(run => run.FromDate).IsRequired();
        builder.Property(run => run.ToDate).IsRequired();
        builder.Property(run => run.Trigger).HasConversion<int>().IsRequired();
        builder.Property(run => run.TotalRecords).IsRequired();
        builder.Property(run => run.Processed).IsRequired();
        builder.Property(run => run.Matched).IsRequired();
        builder.Property(run => run.Duplicate).IsRequired();
        builder.Property(run => run.Rejected).IsRequired();
        builder.Property(run => run.Ignored).IsRequired();
        builder.Property(run => run.Failed).IsRequired();
        builder.Property(run => run.ErrorMessage).HasMaxLength(500).IsRequired(false);

        builder.HasIndex(run => run.StartedAtUtc);
        builder.HasIndex(run => new { run.FromDate, run.ToDate });
    }
}
```

- [ ] **Step 7: Add migration**

Create `20260605103000_AddCassoReconciliationRun.cs` with a `CreateTable` for `tbl.CassoReconciliationRuns` and these columns:

```csharp
Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
Trigger = table.Column<int>(type: "int", nullable: false),
TotalRecords = table.Column<int>(type: "int", nullable: false),
Processed = table.Column<int>(type: "int", nullable: false),
Matched = table.Column<int>(type: "int", nullable: false),
Duplicate = table.Column<int>(type: "int", nullable: false),
Rejected = table.Column<int>(type: "int", nullable: false),
Ignored = table.Column<int>(type: "int", nullable: false),
Failed = table.Column<int>(type: "int", nullable: false),
ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
```

Add indexes:

```csharp
migrationBuilder.CreateIndex(
    name: "IX_CassoReconciliationRuns_StartedAtUtc",
    schema: "tbl",
    table: "CassoReconciliationRuns",
    column: "StartedAtUtc");

migrationBuilder.CreateIndex(
    name: "IX_CassoReconciliationRuns_FromDate_ToDate",
    schema: "tbl",
    table: "CassoReconciliationRuns",
    columns: new[] { "FromDate", "ToDate" });
```

- [ ] **Step 8: Register manager**

In `Program.cs`, add:

```csharp
services.AddScoped<ICassoReconciliationRunManager, CassoReconciliationRunManager>();
```

Place it beside the existing debt manager registrations:

```csharp
services.AddScoped<IBankTransferPaymentIntentManager, BankTransferPaymentIntentManager>();
services.AddScoped<IBankTransferVerificationLogManager, BankTransferVerificationLogManager>();
services.AddScoped<ICassoReconciliationRunManager, CassoReconciliationRunManager>();
```

- [ ] **Step 9: Verify and commit run metadata**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build completes with `0 Error(s)`.

Commit:

```powershell
rtk git add NamEcommerce\Domain\NamEcommerce.Domain.Shared\Enums\Debts\CassoReconciliationRunTrigger.cs NamEcommerce\Domain\NamEcommerce.Domain.Shared\Dtos\Debts\CassoReconciliationRunDtos.cs NamEcommerce\Domain\NamEcommerce.Domain.Shared\Services\Debts\ICassoReconciliationRunManager.cs NamEcommerce\Domain\NamEcommerce.Domain\Entities\Debts\CassoReconciliationRun.cs NamEcommerce\Domain\NamEcommerce.Domain.Services\Debts\CassoReconciliationRunManager.cs NamEcommerce\Infrastructure\NamEcommerce.Data.SqlServer\Mappings\CassoReconciliationRunMapping.cs NamEcommerce\Migrations\NamEcommerce.Data.SqlServerMigrations\Migrations\20260605103000_AddCassoReconciliationRun.cs NamEcommerce\Presentation\NamEcommerce.Web\Program.cs
rtk git commit -m "feat: track casso reconciliation runs"
```

---

### Task 3: Add Casso Application Contracts

**Files:**
- Create: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Debts/ICassoBankTransferAppService.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Debts/ICassoTransactionClient.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Debts/CassoBankTransferAppDtos.cs`

- [ ] **Step 1: Add app service contract**

Create `ICassoBankTransferAppService.cs`:

```csharp
using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Contracts.Debts;

public interface ICassoBankTransferAppService
{
    Task<CassoBankTransferProcessingResultAppDto> ProcessWebhookAsync(ProcessCassoWebhookAppDto dto);
    Task<CassoBankTransferProcessingResultAppDto> RunReconciliationAsync(RunCassoReconciliationAppDto dto);
}
```

- [ ] **Step 2: Add Casso client contract**

Create `ICassoTransactionClient.cs`:

```csharp
using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Contracts.Debts;

public interface ICassoTransactionClient
{
    Task<CassoTransactionPageAppDto> GetTransactionsAsync(GetCassoTransactionsAppDto dto, CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Add app DTOs**

Create `CassoBankTransferAppDtos.cs`:

```csharp
using System.Text.Json.Serialization;
using NamEcommerce.Domain.Shared.Enums.Debts;

namespace NamEcommerce.Application.Contracts.Dtos.Debts;

[Serializable]
public sealed record ProcessCassoWebhookAppDto
{
    public int? Error { get; init; }
    public IList<CassoTransactionAppDto> Data { get; init; } = [];
    public string RawPayload { get; init; } = string.Empty;
}

[Serializable]
public sealed record RunCassoReconciliationAppDto
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public required CassoReconciliationRunTrigger Trigger { get; init; }
}

[Serializable]
public sealed record GetCassoTransactionsAppDto
{
    public required DateTime FromDate { get; init; }
    public required DateTime ToDate { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}

[Serializable]
public sealed record CassoTransactionPageAppDto
{
    public IList<CassoTransactionAppDto> Records { get; init; } = [];
    public bool HasMore { get; init; }
}

[Serializable]
public sealed record CassoTransactionAppDto
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [JsonPropertyName("tid")]
    public string? Tid { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("when")]
    public string? When { get; init; }

    [JsonPropertyName("bank_sub_acc_id")]
    public string? BankSubAccId { get; init; }

    [JsonPropertyName("subAccId")]
    public string? SubAccId { get; init; }

    [JsonPropertyName("bankSubAccId")]
    public string? BankSubAccIdCamel { get; init; }

    [JsonPropertyName("bankName")]
    public string? BankName { get; init; }

    [JsonPropertyName("bankAbbreviation")]
    public string? BankAbbreviation { get; init; }

    [JsonPropertyName("bankCodeName")]
    public string? BankCodeName { get; init; }
}

[Serializable]
public sealed record CassoMappedTransactionAppDto
{
    public bool CanProcess { get; init; }
    public string? IgnoreReason { get; init; }
    public ProcessBankTransferProviderTransactionAppDto? ProviderTransaction { get; init; }
}

[Serializable]
public sealed record CassoTransactionProcessingResultAppDto
{
    public required bool Success { get; init; }
    public required bool Ignored { get; init; }
    public string? Message { get; init; }
    public string? ProviderTransactionId { get; init; }
    public Guid? VerificationLogId { get; init; }
}

[Serializable]
public sealed record CassoBankTransferProcessingResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? RunId { get; init; }
    public int TotalRecords { get; init; }
    public int Processed { get; init; }
    public int Matched { get; init; }
    public int Duplicate { get; init; }
    public int Rejected { get; init; }
    public int Ignored { get; init; }
    public int Failed { get; init; }
    public IList<CassoTransactionProcessingResultAppDto> Results { get; init; } = [];
}
```

- [ ] **Step 4: Verify and commit contracts**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build completes with `0 Error(s)`.

Commit:

```powershell
rtk git add NamEcommerce\Application\NamEcommerce.Application.Contracts\Debts\ICassoBankTransferAppService.cs NamEcommerce\Application\NamEcommerce.Application.Contracts\Debts\ICassoTransactionClient.cs NamEcommerce\Application\NamEcommerce.Application.Contracts\Dtos\Debts\CassoBankTransferAppDtos.cs
rtk git commit -m "feat: add casso bank transfer contracts"
```

---

### Task 4: Add Casso Transaction Mapper

**Files:**
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/Debts/CassoTransactionMapper.cs`

- [ ] **Step 1: Create mapper**

Create `CassoTransactionMapper.cs`:

```csharp
using System.Globalization;
using System.Text;
using System.Text.Json;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Settings;

namespace NamEcommerce.Application.Services.Debts;

public sealed class CassoTransactionMapper(BankTransferPaymentSettings settings)
{
    public CassoMappedTransactionAppDto Map(
        CassoTransactionAppDto transaction,
        BankTransferVerificationSource source)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (transaction.Amount <= 0)
            return Ignore("Ignored.NonIncomingTransfer");

        var providerTransactionId = transaction.Id?.ToString(CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(providerTransactionId))
            return Ignore("Ignored.ProviderTransactionIdMissing");

        var accountNo = FirstNonEmpty(transaction.BankSubAccId, transaction.SubAccId, transaction.BankSubAccIdCamel);
        if (string.IsNullOrWhiteSpace(accountNo))
            return Ignore("Ignored.AccountNoMissing");

        var referenceCode = ExtractReferenceCode(transaction.Description);
        if (string.IsNullOrWhiteSpace(referenceCode))
            return Ignore("Ignored.ReferenceCodeMissing");

        var confirmedAtUtc = ParseConfirmedAtUtc(transaction.When);
        var bankId = FirstNonEmpty(transaction.BankAbbreviation, transaction.BankCodeName, settings.BankId);
        if (string.IsNullOrWhiteSpace(bankId))
            return Ignore("Ignored.BankIdMissing");

        return new CassoMappedTransactionAppDto
        {
            CanProcess = true,
            ProviderTransaction = new ProcessBankTransferProviderTransactionAppDto
            {
                ReferenceCode = referenceCode,
                Amount = transaction.Amount,
                BankId = bankId,
                AccountNo = accountNo,
                ProviderTransactionId = providerTransactionId,
                Source = (int)source,
                RawPayload = JsonSerializer.Serialize(transaction),
                ConfirmedAtUtc = confirmedAtUtc
            }
        };
    }

    private string? ExtractReferenceCode(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var prefix = string.IsNullOrWhiteSpace(settings.TransferContentPrefix)
            ? "QS"
            : settings.TransferContentPrefix.Trim().ToUpperInvariant();

        var tokens = NormalizeTokens(description);
        return tokens.FirstOrDefault(token => token.StartsWith(prefix, StringComparison.Ordinal) && token.Length <= 25);
    }

    private static IList<string> NormalizeTokens(string description)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();

        foreach (var ch in description.ToUpperInvariant())
        {
            if (ch is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                current.Append(ch);
                continue;
            }

            AddToken(tokens, current);
        }

        AddToken(tokens, current);
        return tokens;
    }

    private static void AddToken(ICollection<string> tokens, StringBuilder current)
    {
        if (current.Length == 0)
            return;

        tokens.Add(current.ToString());
        current.Clear();
    }

    private static DateTime ParseConfirmedAtUtc(string? value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            return parsed.ToUniversalTime();

        return DateTime.UtcNow;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static CassoMappedTransactionAppDto Ignore(string reason)
        => new()
        {
            CanProcess = false,
            IgnoreReason = reason
        };
}
```

- [ ] **Step 2: Verify mapper compiles**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build completes with `0 Error(s)`.

- [ ] **Step 3: Commit mapper**

Run:

```powershell
rtk git add NamEcommerce\Application\NamEcommerce.Application.Services\Debts\CassoTransactionMapper.cs
rtk git commit -m "feat: map casso transactions to bank transfer provider records"
```

---

### Task 5: Add Casso Transaction API Client

**Files:**
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/Debts/CassoTransactionClient.cs`

- [ ] **Step 1: Add typed client implementation**

Create `CassoTransactionClient.cs`:

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Settings;

namespace NamEcommerce.Application.Services.Debts;

public sealed class CassoTransactionClient(
    HttpClient httpClient,
    BankTransferPaymentSettings settings) : ICassoTransactionClient
{
    public async Task<CassoTransactionPageAppDto> GetTransactionsAsync(
        GetCassoTransactionsAppDto dto,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(settings.Casso.ApiKey))
            throw new InvalidOperationException("Error.CassoApiKeyRequired");

        httpClient.BaseAddress = new Uri(settings.Casso.ApiBaseUrl.TrimEnd('/') + "/");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Apikey", settings.Casso.ApiKey);

        var url = $"v2/transactions?fromDate={dto.FromDate:yyyy-MM-dd}&toDate={dto.ToDate:yyyy-MM-dd}&page={dto.Page}&pageSize={dto.PageSize}&sort=ASC";
        var response = await httpClient.GetFromJsonAsync<CassoTransactionsApiResponse>(url, cancellationToken).ConfigureAwait(false);
        var records = response?.Data?.Records ?? [];

        return new CassoTransactionPageAppDto
        {
            Records = records,
            HasMore = records.Count >= dto.PageSize
        };
    }

    private sealed record CassoTransactionsApiResponse
    {
        [JsonPropertyName("data")]
        public CassoTransactionsApiData? Data { get; init; }
    }

    private sealed record CassoTransactionsApiData
    {
        [JsonPropertyName("records")]
        public IList<CassoTransactionAppDto> Records { get; init; } = [];
    }
}
```

- [ ] **Step 2: Verify API client compiles**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build completes with `0 Error(s)`.

- [ ] **Step 3: Commit API client**

```powershell
rtk git add NamEcommerce\Application\NamEcommerce.Application.Services\Debts\CassoTransactionClient.cs
rtk git commit -m "feat: add casso transaction api client"
```

---

### Task 6: Add Casso Bank Transfer App Service

**Files:**
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/Debts/CassoBankTransferAppService.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`

- [ ] **Step 1: Add app service implementation**

Create `CassoBankTransferAppService.cs`:

```csharp
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Settings;

namespace NamEcommerce.Application.Services.Debts;

public sealed class CassoBankTransferAppService(
    CassoTransactionMapper mapper,
    ICassoTransactionClient transactionClient,
    IBankTransferPaymentIntentAppService paymentIntentAppService,
    ICassoReconciliationRunManager reconciliationRunManager,
    BankTransferPaymentSettings settings) : ICassoBankTransferAppService
{
    public async Task<CassoBankTransferProcessingResultAppDto> ProcessWebhookAsync(ProcessCassoWebhookAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!IsCassoEnabled())
            return Error("Error.CassoVerificationDisabled");
        if (!settings.Casso.WebhookEnabled)
            return Error("Error.CassoWebhookDisabled");
        if (dto.Data.Count == 0)
            return Success([]);

        return await ProcessTransactionsAsync(dto.Data, BankTransferVerificationSource.BankWebhook).ConfigureAwait(false);
    }

    public async Task<CassoBankTransferProcessingResultAppDto> RunReconciliationAsync(RunCassoReconciliationAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!IsCassoEnabled())
            return Error("Error.CassoVerificationDisabled");
        if (!settings.Casso.ReconciliationEnabled)
            return Error("Error.CassoReconciliationDisabled");

        var (fromDate, toDate) = ResolveDateRange(dto);
        var run = await reconciliationRunManager.StartAsync(new StartCassoReconciliationRunDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            Trigger = dto.Trigger
        }).ConfigureAwait(false);

        try
        {
            var allResults = new List<CassoTransactionProcessingResultAppDto>();
            var page = 1;
            var pageSize = Math.Max(1, settings.Casso.ReconciliationPageSize);
            var hasMore = true;

            while (hasMore)
            {
                var cassoPage = await transactionClient.GetTransactionsAsync(new GetCassoTransactionsAppDto
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Page = page,
                    PageSize = pageSize
                }, CancellationToken.None).ConfigureAwait(false);

                var pageResult = await ProcessTransactionsAsync(cassoPage.Records, BankTransferVerificationSource.BankStatement).ConfigureAwait(false);
                allResults.AddRange(pageResult.Results);

                hasMore = cassoPage.HasMore;
                page += 1;
            }

            var result = Success(allResults) with { RunId = run.Id };
            await CompleteRunAsync(run.Id, result).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            await reconciliationRunManager.FailAsync(new FailCassoReconciliationRunDto
            {
                RunId = run.Id,
                ErrorMessage = ex.Message
            }).ConfigureAwait(false);

            return Error(ex.Message) with { RunId = run.Id };
        }
    }

    private async Task<CassoBankTransferProcessingResultAppDto> ProcessTransactionsAsync(
        IEnumerable<CassoTransactionAppDto> transactions,
        BankTransferVerificationSource source)
    {
        var results = new List<CassoTransactionProcessingResultAppDto>();

        foreach (var transaction in transactions)
        {
            var mapped = mapper.Map(transaction, source);
            if (!mapped.CanProcess || mapped.ProviderTransaction is null)
            {
                results.Add(new CassoTransactionProcessingResultAppDto
                {
                    Success = true,
                    Ignored = true,
                    Message = mapped.IgnoreReason,
                    ProviderTransactionId = transaction.Id?.ToString()
                });
                continue;
            }

            var processResult = await paymentIntentAppService
                .ProcessProviderTransactionAsync(mapped.ProviderTransaction)
                .ConfigureAwait(false);

            results.Add(new CassoTransactionProcessingResultAppDto
            {
                Success = processResult.Success,
                Ignored = false,
                Message = processResult.ErrorMessage,
                ProviderTransactionId = mapped.ProviderTransaction.ProviderTransactionId,
                VerificationLogId = processResult.VerificationLogId
            });
        }

        return Success(results);
    }

    private async Task CompleteRunAsync(Guid runId, CassoBankTransferProcessingResultAppDto result)
    {
        await reconciliationRunManager.CompleteAsync(new CompleteCassoReconciliationRunDto
        {
            RunId = runId,
            TotalRecords = result.TotalRecords,
            Processed = result.Processed,
            Matched = result.Matched,
            Duplicate = result.Duplicate,
            Rejected = result.Rejected,
            Ignored = result.Ignored,
            Failed = result.Failed
        }).ConfigureAwait(false);
    }

    private bool IsCassoEnabled()
        => settings.Casso.Enabled
           && string.Equals(settings.Verification.Provider, "Casso", StringComparison.OrdinalIgnoreCase);

    private (DateTime fromDate, DateTime toDate) ResolveDateRange(RunCassoReconciliationAppDto dto)
    {
        var toDate = dto.ToDate?.Date ?? DateTime.Today;
        var fromDate = dto.FromDate?.Date ?? DateTime.UtcNow.AddMinutes(-settings.Casso.ReconciliationLookbackMinutes).Date;
        return (fromDate, toDate);
    }

    private static CassoBankTransferProcessingResultAppDto Success(IList<CassoTransactionProcessingResultAppDto> results)
        => new()
        {
            Success = true,
            TotalRecords = results.Count,
            Processed = results.Count(result => !result.Ignored),
            Matched = results.Count(result => result.Success && !result.Ignored),
            Duplicate = results.Count(result => result.Message == "Error.PaymentIntentProviderTransactionDuplicated"),
            Rejected = results.Count(result => !result.Success && !result.Ignored && result.Message != "Error.PaymentIntentProviderTransactionDuplicated"),
            Ignored = results.Count(result => result.Ignored),
            Failed = 0,
            Results = results
        };

    private static CassoBankTransferProcessingResultAppDto Error(string errorMessage)
        => new()
        {
            Success = false,
            ErrorMessage = errorMessage,
            Failed = 1
        };
}
```

- [ ] **Step 2: Ensure DI registration exists**

In `Program.cs`, add:

```csharp
services.AddScoped<CassoTransactionMapper>();
services.AddScoped<ICassoBankTransferAppService, CassoBankTransferAppService>();
services.AddHttpClient<ICassoTransactionClient, CassoTransactionClient>();
```

Place it beside:

```csharp
services.AddScoped<IBankTransferPaymentIntentAppService, BankTransferPaymentIntentAppService>();
services.AddScoped<IBankTransferVerificationProvider, NoneBankTransferVerificationProvider>();
```

- [ ] **Step 3: Verify and commit app service**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build completes with `0 Error(s)`.

Commit:

```powershell
rtk git add NamEcommerce\Application\NamEcommerce.Application.Services\Debts\CassoBankTransferAppService.cs NamEcommerce\Presentation\NamEcommerce.Web\Program.cs
rtk git commit -m "feat: process casso bank transfer transactions"
```

---

### Task 7: Add Casso Web Commands, Handlers, and Controller

**Files:**
- Create: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/Debts/CassoBankTransferCommands.cs`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/Debts/CassoBankTransferResultModels.cs`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/Debts/CassoBankTransferCommandHandlers.cs`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/CassoBankTransferController.cs`

- [ ] **Step 1: Add command models**

Create `CassoBankTransferCommands.cs`:

```csharp
using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Contracts.Commands.Models.Debts;

[Serializable]
public sealed record ProcessCassoWebhookCommand : IRequest<CassoBankTransferProcessingResultModel>
{
    public int? Error { get; init; }
    public IList<CassoTransactionAppDto> Data { get; init; } = [];
    public string RawPayload { get; init; } = string.Empty;
}

[Serializable]
public sealed record RunCassoReconciliationCommand : IRequest<CassoBankTransferProcessingResultModel>
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public CassoReconciliationRunTrigger Trigger { get; init; } = CassoReconciliationRunTrigger.Manual;
}
```

- [ ] **Step 2: Add result models**

Create `CassoBankTransferResultModels.cs`:

```csharp
namespace NamEcommerce.Web.Contracts.Models.Debts;

[Serializable]
public sealed record CassoTransactionProcessingResultModel
{
    public bool Success { get; init; }
    public bool Ignored { get; init; }
    public string? Message { get; init; }
    public string? ProviderTransactionId { get; init; }
    public Guid? VerificationLogId { get; init; }
}

[Serializable]
public sealed record CassoBankTransferProcessingResultModel
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? RunId { get; init; }
    public int TotalRecords { get; init; }
    public int Processed { get; init; }
    public int Matched { get; init; }
    public int Duplicate { get; init; }
    public int Rejected { get; init; }
    public int Ignored { get; init; }
    public int Failed { get; init; }
    public IList<CassoTransactionProcessingResultModel> Results { get; init; } = [];
}
```

- [ ] **Step 3: Add handlers**

Create `CassoBankTransferCommandHandlers.cs`:

```csharp
using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class ProcessCassoWebhookCommandHandler(ICassoBankTransferAppService cassoAppService)
    : IRequestHandler<ProcessCassoWebhookCommand, CassoBankTransferProcessingResultModel>
{
    public async Task<CassoBankTransferProcessingResultModel> Handle(ProcessCassoWebhookCommand request, CancellationToken cancellationToken)
    {
        var result = await cassoAppService.ProcessWebhookAsync(new ProcessCassoWebhookAppDto
        {
            Error = request.Error,
            Data = request.Data,
            RawPayload = request.RawPayload
        }).ConfigureAwait(false);

        return CassoBankTransferCommandHandlerMapper.Map(result);
    }
}

public sealed class RunCassoReconciliationCommandHandler(ICassoBankTransferAppService cassoAppService)
    : IRequestHandler<RunCassoReconciliationCommand, CassoBankTransferProcessingResultModel>
{
    public async Task<CassoBankTransferProcessingResultModel> Handle(RunCassoReconciliationCommand request, CancellationToken cancellationToken)
    {
        var result = await cassoAppService.RunReconciliationAsync(new RunCassoReconciliationAppDto
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Trigger = request.Trigger
        }).ConfigureAwait(false);

        return CassoBankTransferCommandHandlerMapper.Map(result);
    }
}

internal static class CassoBankTransferCommandHandlerMapper
{
    public static CassoBankTransferProcessingResultModel Map(CassoBankTransferProcessingResultAppDto result)
        => new()
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            RunId = result.RunId,
            TotalRecords = result.TotalRecords,
            Processed = result.Processed,
            Matched = result.Matched,
            Duplicate = result.Duplicate,
            Rejected = result.Rejected,
            Ignored = result.Ignored,
            Failed = result.Failed,
            Results = result.Results.Select(Map).ToList()
        };

    private static CassoTransactionProcessingResultModel Map(CassoTransactionProcessingResultAppDto result)
        => new()
        {
            Success = result.Success,
            Ignored = result.Ignored,
            Message = result.Message,
            ProviderTransactionId = result.ProviderTransactionId,
            VerificationLogId = result.VerificationLogId
        };
}
```

- [ ] **Step 4: Add controller**

Create `CassoBankTransferController.cs`:

```csharp
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Domain.Shared.Settings;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;

namespace NamEcommerce.Web.Controllers;

[Route("api/casso")]
public sealed class CassoBankTransferController(
    IMediator mediator,
    BankTransferPaymentSettings settings) : Controller
{
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveWebhook()
    {
        if (!settings.Casso.Enabled || !string.Equals(settings.Verification.Provider, "Casso", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        if (!settings.Casso.WebhookEnabled)
            return NotFound();

        if (string.IsNullOrWhiteSpace(settings.Casso.WebhookSecurityKey)
            || string.IsNullOrWhiteSpace(settings.Casso.WebhookSecurityHeaderName))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var token = Request.Headers[settings.Casso.WebhookSecurityHeaderName].ToString();
        if (!string.Equals(token, settings.Casso.WebhookSecurityKey, StringComparison.Ordinal))
            return Unauthorized();

        string rawPayload;
        using (var reader = new StreamReader(Request.Body))
            rawPayload = await reader.ReadToEndAsync().ConfigureAwait(false);

        ProcessCassoWebhookCommand? command;
        try
        {
            command = JsonSerializer.Deserialize<ProcessCassoWebhookCommand>(rawPayload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return BadRequest(new { success = false, message = "Error.CassoWebhookPayloadMalformed" });
        }

        if (command is null)
            return BadRequest(new { success = false, message = "Error.CassoWebhookPayloadMalformed" });

        command = command with { RawPayload = rawPayload };
        var result = await mediator.Send(command).ConfigureAwait(false);

        return Ok(new
        {
            success = true,
            result.TotalRecords,
            result.Processed,
            result.Matched,
            result.Duplicate,
            result.Rejected,
            result.Ignored,
            result.Failed,
            result.Results
        });
    }

    [Authorize]
    [HttpPost("reconciliation/run")]
    public async Task<IActionResult> RunReconciliation([FromBody] RunCassoReconciliationCommand command)
    {
        var result = await mediator.Send(command).ConfigureAwait(false);
        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }
}
```

- [ ] **Step 5: Verify controller route compiles**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build completes with `0 Error(s)`.

- [ ] **Step 6: Commit Casso endpoints**

Run:

```powershell
rtk git add NamEcommerce\Presentation\NamEcommerce.Web.Contracts\Commands\Models\Debts\CassoBankTransferCommands.cs NamEcommerce\Presentation\NamEcommerce.Web.Contracts\Models\Debts\CassoBankTransferResultModels.cs NamEcommerce\Presentation\NamEcommerce.Web.Framework\Commands\Handlers\Debts\CassoBankTransferCommandHandlers.cs NamEcommerce\Presentation\NamEcommerce.Web\Controllers\CassoBankTransferController.cs
rtk git commit -m "feat: add casso bank transfer endpoints"
```

---

### Task 8: Add Scheduled Casso Reconciliation Worker

**Files:**
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Services/Debts/CassoReconciliationHostedService.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`

- [ ] **Step 1: Add hosted service**

Create `CassoReconciliationHostedService.cs`:

```csharp
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Settings;

namespace NamEcommerce.Web.Services.Debts;

public sealed class CassoReconciliationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<CassoReconciliationHostedService> logger) : BackgroundService
{
    private static readonly SemaphoreSlim RunLock = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = GetInterval();

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Casso reconciliation worker failed.");
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (!await RunLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<BankTransferPaymentSettings>();

            if (!settings.Casso.Enabled
                || !settings.Casso.ReconciliationEnabled
                || !string.Equals(settings.Verification.Provider, "Casso", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var appService = scope.ServiceProvider.GetRequiredService<ICassoBankTransferAppService>();
            await appService.RunReconciliationAsync(new RunCassoReconciliationAppDto
            {
                Trigger = CassoReconciliationRunTrigger.Scheduled
            }).ConfigureAwait(false);
        }
        finally
        {
            RunLock.Release();
        }
    }

    private TimeSpan GetInterval()
    {
        using var scope = scopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<BankTransferPaymentSettings>();
        return TimeSpan.FromMinutes(Math.Max(1, settings.Casso.ReconciliationIntervalMinutes));
    }
}
```

- [ ] **Step 2: Register hosted service**

In `Program.cs`, add:

```csharp
services.AddHostedService<CassoReconciliationHostedService>();
```

Place it near the existing outbox hosted service:

```csharp
services.AddHostedService<OutboxProcessor>();
services.AddHostedService<CassoReconciliationHostedService>();
```

- [ ] **Step 3: Verify and commit worker**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build completes with `0 Error(s)`.

Commit:

```powershell
rtk git add NamEcommerce\Presentation\NamEcommerce.Web\Services\Debts\CassoReconciliationHostedService.cs NamEcommerce\Presentation\NamEcommerce.Web\Program.cs
rtk git commit -m "feat: schedule casso bank transfer reconciliation"
```

---

### Task 9: Manual Smoke Verification

**Files:**
- No code files.

- [ ] **Step 1: Run formatting and build checks**

Run:

```powershell
rtk git diff --check
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected:

```text
rtk git diff --check
```

produces no output.

```text
Build succeeded.
0 Error(s)
```

- [ ] **Step 2: Start the web app with local Casso config disabled**

Run:

```powershell
rtk dotnet run --project NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj --no-build --urls https://localhost:7132
```

Expected: app starts without Casso API calls because committed config keeps `Casso.Enabled = false`.

- [ ] **Step 3: Smoke the disabled webhook route**

Send:

```powershell
Invoke-WebRequest -Method Post -Uri https://localhost:7132/api/casso/webhook -SkipCertificateCheck -Body '{"error":0,"data":[]}' -ContentType 'application/json'
```

Expected: HTTP `404` because Casso is disabled.

- [ ] **Step 4: Smoke the enabled webhook route with a temporary local override**

Use a local override that is not committed:

```powershell
$env:Payments__BankTransfer__Verification__Provider='Casso'
$env:Payments__BankTransfer__Casso__Enabled='true'
$env:Payments__BankTransfer__Casso__WebhookEnabled='true'
$env:Payments__BankTransfer__Casso__WebhookSecurityKey='local-secret'
```

Start the app again and send:

```powershell
Invoke-WebRequest -Method Post -Uri https://localhost:7132/api/casso/webhook -SkipCertificateCheck -Headers @{'X-NamEcommerce-Casso-Token'='local-secret'} -Body '{"error":0,"data":[]}' -ContentType 'application/json'
```

Expected: HTTP `200` with JSON containing:

```json
{
  "success": true,
  "totalRecords": 0,
  "processed": 0,
  "matched": 0,
  "ignored": 0
}
```

- [ ] **Step 5: Confirm no smoke-only edits remain**

Run:

```powershell
rtk git status --short
```

Expected: no uncommitted code changes remain after the Task 8 commit. If this command prints changed files, inspect those exact files, decide whether they belong to one of the previous tasks, and commit them through that task's commit step.

---

## Completion Gate

Run these commands before claiming Phase 3 implementation is complete:

```powershell
rtk git diff --check
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected:

- `rtk git diff --check` has no output.
- Web project build completes with `0 Error(s)`.
- Automated `dotnet test` commands are not part of this execution because tests are intentionally skipped for now.
- Full solution build is not the gate for this branch because `NamEcommerce/Customer.Client` currently requires .NET Framework MSBuild support and fails independently of this feature.

## Self-Review Notes

- Spec coverage: Casso webhook, security header validation, transaction mapper, Casso API client, manual reconciliation endpoint, scheduled worker, run metadata, duplicate path through Phase 2, and no secret commits are all covered.
- Type consistency: web commands map to app DTOs, app service returns app result DTOs, web handlers map to web result models, and Casso transactions are normalized to `ProcessBankTransferProviderTransactionAppDto`.
- Scope control: Fast Sale UI, VietQR generation, order accounting, virtual accounts, payOS, OAuth, and dashboard screens remain unchanged.
