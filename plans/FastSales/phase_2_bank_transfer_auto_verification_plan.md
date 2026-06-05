# Fast Sale Phase 2 Bank Transfer Auto Verification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add automatic proof that VietQR money arrived before a bank-transfer fast sale can be completed.

**Architecture:** Keep `BankTransferPaymentIntent` as the source of truth. Provider/bank events enter through a secured webhook boundary, are written to an audit log, then matched by reference code + amount + receiving account + provider transaction id before the intent becomes confirmed. The cashier UI polls intent status and still creates the final order/delivery/debt/payment records only after confirmation.

**Tech Stack:** ASP.NET Core MVC, MediatR, EF Core SQL Server, Razor, vanilla JavaScript, existing NamEcommerce Clean Architecture + DDD patterns.

---

## Assumptions

- Phase 1 already exists on branch `codex/fast-sale-vietqr` with `BankTransferPaymentIntent`, fast sale UI, manual confirmation, and `ConfirmFromProviderAsync(...)`.
- No real bank/provider is selected yet. This phase implements a secured normalized webhook endpoint now; a Casso/bank-specific adapter can map its payload into the normalized command after the provider is chosen.
- A transfer is automatically accepted only when all of these match: `ReferenceCode`, `Amount`, `BankId`, `AccountNo`, and unique `ProviderTransactionId`.
- The system must log provider events even when they do not match a pending intent, so accounting/support can audit failed confirmations.
- QR expiration is enforced on status checks and sale completion. No background worker is required in this phase.
- The cashier still clicks the final complete-sale button. This avoids auto-creating order/accounting records while the customer or cashier may still change cart details.

## Out Of Scope

- Voice ordering.
- Partial fulfillment from an existing large order.
- Admin UI for bank account settings.
- Direct provider implementation for Casso or a specific bank until the provider and its payload/security docs are confirmed.
- Auto-finalizing the sale immediately after bank confirmation.

## File Structure

### Domain

- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/Debts/BankTransferVerificationLogStatus.cs`
  - Verification event processing result enum.
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/BankTransferVerificationLog.cs`
  - Immutable audit record for every provider/bank transaction event received.
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Debts/BankTransferVerificationLogDtos.cs`
  - Domain DTO for creating and returning log records.
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Debts/IBankTransferVerificationLogManager.cs`
  - Domain manager contract for writing matched/rejected/duplicate logs.
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Debts/BankTransferVerificationLogManager.cs`
  - Log manager implementation.
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/BankTransferPaymentIntent.cs`
  - Add `ExpiresAtUtc`, `ExpiredAtUtc`, and an internal `Expire(...)` method.
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Debts/BankTransferPaymentIntentDtos.cs`
  - Include expiry fields.
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Debts/IBankTransferPaymentIntentManager.cs`
  - Add `ExpireIfPendingAsync(...)`.
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Debts/BankTransferPaymentIntentManager.cs`
  - Store `ExpiresAtUtc`, expire pending intents, and keep provider matching rules.
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Settings/BankTransferPaymentSettings.cs`
  - Add expiry and webhook settings.

### Infrastructure

- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/BankTransferVerificationLogMapping.cs`
  - EF mapping for the audit log.
- Create: `NamEcommerce/Migrations/NamEcommerce.Data.SqlServerMigrations/Migrations/{timestamp}_AddBankTransferVerificationLogAndIntentExpiry.cs`
  - Add `BankTransferVerificationLogs`, `ExpiresAtUtc`, `ExpiredAtUtc`.
- Modify: `NamEcommerce/Migrations/NamEcommerce.Data.SqlServerMigrations/Migrations/NamEcommerceEfDbContextModelSnapshot.cs`
  - Snapshot update.

### Application

- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Debts/BankTransferPaymentIntentAppDtos.cs`
  - Add `ExpiresAtUtc`, `ExpiredAtUtc`, normalized provider transaction DTO, and processing result DTO.
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Debts/IBankTransferPaymentIntentAppService.cs`
  - Add `GetStatusAsync(Guid id)` and `ProcessProviderTransactionAsync(...)`.
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/Debts/BankTransferPaymentIntentAppService.cs`
  - Expire pending intents on status read; log every provider event; confirm matched intents.

### Presentation

- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/FastSales/FastSaleCommands.cs`
  - Add `GetBankTransferPaymentIntentStatusCommand` and `ProcessBankTransferProviderTransactionCommand`.
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/FastSales/FastSaleCommandHandlers.cs`
  - Add handlers and mapping for status/provider processing.
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/FastSales/FastSaleResultModels.cs`
  - Add expiry and verification fields returned to UI.
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/FastSaleController.cs`
  - Add authenticated status endpoint for UI polling.
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/BankTransferWebhookController.cs`
  - Add provider webhook endpoint guarded by configured shared token.
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/FastSale/Index.cshtml`
  - Add intent status URL, expiry display, and waiting status labels.
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/FastSale.js`
  - Poll intent status after QR creation and update UI automatically.
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/appsettings.json`
  - Add default expiry and disabled webhook config.
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/appsettings.Development.json`
  - Mirror development config.
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`
  - Register `BankTransferVerificationLogManager` and updated settings.

---

## Task 1: Add Intent Expiry Configuration

**Files:**
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Settings/BankTransferPaymentSettings.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/appsettings.json`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/appsettings.Development.json`

- [ ] **Step 1: Add settings properties**

Add these properties to `BankTransferPaymentSettings`:

```csharp
public int IntentExpiryMinutes { get; init; } = 15;
public int StatusPollingSeconds { get; init; } = 3;
public BankTransferWebhookSettings Webhook { get; init; } = new();
```

Add this class in the same file:

```csharp
[Serializable]
public sealed class BankTransferWebhookSettings
{
    public bool Enabled { get; init; }
    public string SecretToken { get; init; } = string.Empty;
}
```

- [ ] **Step 2: Update config**

Add these keys under `Payments:BankTransfer` in both appsettings files:

```json
"IntentExpiryMinutes": 15,
"StatusPollingSeconds": 3,
"Webhook": {
  "Enabled": false,
  "SecretToken": ""
}
```

- [ ] **Step 3: Verify**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build succeeds with `0 errors`.

---

## Task 2: Persist Intent Expiry

**Files:**
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/BankTransferPaymentIntent.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Debts/BankTransferPaymentIntentDtos.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Debts/IBankTransferPaymentIntentManager.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Debts/BankTransferPaymentIntentManager.cs`
- Modify: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/BankTransferPaymentIntentMapping.cs`
- Create: `NamEcommerce/Migrations/NamEcommerce.Data.SqlServerMigrations/Migrations/{timestamp}_AddBankTransferPaymentIntentExpiry.cs`
- Modify: `NamEcommerce/Migrations/NamEcommerce.Data.SqlServerMigrations/Migrations/NamEcommerceEfDbContextModelSnapshot.cs`

- [ ] **Step 1: Add expiry fields to entity**

Add properties:

```csharp
public DateTime ExpiresAtUtc { get; private set; }
public DateTime? ExpiredAtUtc { get; private set; }
```

Change the internal constructor signature to accept `DateTime expiresAtUtc` and set:

```csharp
ExpiresAtUtc = expiresAtUtc;
```

Add the state transition:

```csharp
internal void Expire(DateTime nowUtc)
{
    if (Status != BankTransferPaymentIntentStatus.Pending)
        return;

    Status = BankTransferPaymentIntentStatus.Expired;
    ExpiredAtUtc = nowUtc;
    UpdatedOnUtc = nowUtc;
}
```

- [ ] **Step 2: Add expiry to DTOs**

Add to `BankTransferPaymentIntentDto` and `BankTransferPaymentIntentAppDto`:

```csharp
public DateTime ExpiresAtUtc { get; init; }
public DateTime? ExpiredAtUtc { get; init; }
```

- [ ] **Step 3: Update manager contract**

Add to `IBankTransferPaymentIntentManager`:

```csharp
Task<BankTransferPaymentIntentDto> ExpireIfPendingAsync(Guid id, DateTime nowUtc);
```

- [ ] **Step 4: Update creation and expiry in manager**

In `CreateAsync`, compute:

```csharp
var expiresAtUtc = DateTime.UtcNow.AddMinutes(Math.Max(1, dto.IntentExpiryMinutes));
```

Pass `expiresAtUtc` into the entity constructor.

Implement:

```csharp
public async Task<BankTransferPaymentIntentDto> ExpireIfPendingAsync(Guid id, DateTime nowUtc)
{
    var intent = await intentRepository.GetByIdAsync(id).ConfigureAwait(false)
        ?? throw new NamEcommerceDomainException("Error.PaymentIntentIsNotFound");

    if (intent.Status == BankTransferPaymentIntentStatus.Pending && intent.ExpiresAtUtc <= nowUtc)
    {
        intent.Expire(nowUtc);
        var updated = await intentRepository.UpdateAsync(intent).ConfigureAwait(false);
        return MapToDto(updated);
    }

    return MapToDto(intent);
}
```

- [ ] **Step 5: Update mapping and migration**

Add columns:

```csharp
builder.Property(x => x.ExpiresAtUtc).IsRequired();
builder.Property(x => x.ExpiredAtUtc);
```

Migration should add:

```csharp
ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(minute, 15, SYSUTCDATETIME())")
ExpiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
```

- [ ] **Step 6: Verify**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build succeeds with `0 errors`.

---

## Task 3: Add Verification Audit Log

**Files:**
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/Debts/BankTransferVerificationLogStatus.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/BankTransferVerificationLog.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Debts/BankTransferVerificationLogDtos.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Debts/IBankTransferVerificationLogManager.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Debts/BankTransferVerificationLogManager.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/BankTransferVerificationLogMapping.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`

- [ ] **Step 1: Add log status enum**

```csharp
namespace NamEcommerce.Domain.Shared.Enums.Debts;

public enum BankTransferVerificationLogStatus
{
    Received = 10,
    Matched = 20,
    Rejected = 30,
    Duplicate = 40
}
```

- [ ] **Step 2: Add log entity**

Create a sealed record with these properties:

```csharp
public string ReferenceCode { get; private set; } = string.Empty;
public decimal Amount { get; private set; }
public string BankId { get; private set; } = string.Empty;
public string AccountNo { get; private set; } = string.Empty;
public string ProviderTransactionId { get; private set; } = string.Empty;
public BankTransferVerificationSource Source { get; private set; }
public BankTransferVerificationLogStatus Status { get; private set; }
public Guid? PaymentIntentId { get; private set; }
public string? ErrorMessage { get; private set; }
public string? RawPayload { get; private set; }
public DateTime ProviderConfirmedAtUtc { get; private set; }
public DateTime CreatedOnUtc { get; private set; }
public DateTime? UpdatedOnUtc { get; private set; }
```

Add internal methods:

```csharp
internal void MarkMatched(Guid paymentIntentId, DateTime nowUtc)
{
    PaymentIntentId = paymentIntentId;
    Status = BankTransferVerificationLogStatus.Matched;
    UpdatedOnUtc = nowUtc;
}

internal void MarkRejected(string errorMessage, DateTime nowUtc)
{
    ErrorMessage = errorMessage;
    Status = BankTransferVerificationLogStatus.Rejected;
    UpdatedOnUtc = nowUtc;
}

internal void MarkDuplicate(Guid? paymentIntentId, string errorMessage, DateTime nowUtc)
{
    PaymentIntentId = paymentIntentId;
    ErrorMessage = errorMessage;
    Status = BankTransferVerificationLogStatus.Duplicate;
    UpdatedOnUtc = nowUtc;
}
```

- [ ] **Step 3: Add manager**

`IBankTransferVerificationLogManager` should expose:

```csharp
Task<BankTransferVerificationLogDto> CreateReceivedAsync(CreateBankTransferVerificationLogDto dto);
Task<BankTransferVerificationLogDto> MarkMatchedAsync(Guid id, Guid paymentIntentId);
Task<BankTransferVerificationLogDto> MarkRejectedAsync(Guid id, string errorMessage);
Task<BankTransferVerificationLogDto> MarkDuplicateAsync(Guid id, Guid? paymentIntentId, string errorMessage);
```

- [ ] **Step 4: Add EF mapping**

Use table name `BankTransferVerificationLogs`. Add indexes:

```csharp
builder.HasIndex(x => x.ReferenceCode);
builder.HasIndex(x => x.ProviderTransactionId);
builder.HasIndex(x => x.PaymentIntentId);
```

Set string lengths:

```csharp
builder.Property(x => x.ReferenceCode).HasMaxLength(25).IsRequired();
builder.Property(x => x.BankId).HasMaxLength(50).IsRequired();
builder.Property(x => x.AccountNo).HasMaxLength(50).IsRequired();
builder.Property(x => x.ProviderTransactionId).HasMaxLength(100).IsRequired();
builder.Property(x => x.RawPayload).HasMaxLength(4000);
builder.Property(x => x.ErrorMessage).HasMaxLength(500);
```

- [ ] **Step 5: Register manager**

In `Program.cs`, register:

```csharp
builder.Services.AddScoped<IBankTransferVerificationLogManager, BankTransferVerificationLogManager>();
```

- [ ] **Step 6: Verify**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build succeeds with `0 errors`.

---

## Task 4: Process Provider Transactions In Application Layer

**Files:**
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Debts/BankTransferPaymentIntentAppDtos.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Debts/IBankTransferPaymentIntentAppService.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/Debts/BankTransferPaymentIntentAppService.cs`

- [ ] **Step 1: Add normalized provider DTO**

```csharp
[Serializable]
public sealed record ProcessBankTransferProviderTransactionAppDto
{
    public required string ReferenceCode { get; init; }
    public required decimal Amount { get; init; }
    public required string BankId { get; init; }
    public required string AccountNo { get; init; }
    public required string ProviderTransactionId { get; init; }
    public required int Source { get; init; }
    public string? RawPayload { get; init; }
    public DateTime ConfirmedAtUtc { get; init; } = DateTime.UtcNow;
}

[Serializable]
public sealed record BankTransferProviderProcessingResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public BankTransferPaymentIntentAppDto? Intent { get; init; }
    public Guid? VerificationLogId { get; init; }
}
```

- [ ] **Step 2: Add app service contract**

```csharp
Task<BankTransferPaymentIntentResultAppDto> GetStatusAsync(Guid id);
Task<BankTransferProviderProcessingResultAppDto> ProcessProviderTransactionAsync(ProcessBankTransferProviderTransactionAppDto dto);
```

- [ ] **Step 3: Implement status read with expiry**

`GetStatusAsync` should:

```csharp
var intent = await paymentIntentManager.ExpireIfPendingAsync(id, DateTime.UtcNow).ConfigureAwait(false);
return BankTransferPaymentIntentResultAppDto.CreateSuccess(MapToDto(intent));
```

If intent is missing, return `Success = false` with `Error.PaymentIntentIsNotFound`.

- [ ] **Step 4: Implement provider transaction processing**

Processing order:

1. Validate required reference, amount, bank id, account no, provider transaction id.
2. Create a `Received` verification log.
3. Call existing provider confirmation logic.
4. If confirmation succeeds, mark log `Matched`.
5. If provider transaction id is duplicated, mark log `Duplicate`.
6. For wrong amount/reference/account or missing intent, mark log `Rejected`.
7. Return the log id and matched intent when available.

Use existing error keys from Phase 1 where possible:

```csharp
"Error.PaymentIntentIsNotFound"
"Error.PaymentIntentVerificationMismatch"
"Error.PaymentIntentProviderTransactionDuplicated"
"Error.PaymentIntentCannotConfirm"
```

- [ ] **Step 5: Verify**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build succeeds with `0 errors`.

---

## Task 5: Add Secured Webhook Endpoint

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/FastSales/FastSaleCommands.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/FastSales/FastSaleCommandHandlers.cs`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/BankTransferWebhookController.cs`

- [ ] **Step 1: Add command**

```csharp
[Serializable]
public sealed class ProcessBankTransferProviderTransactionCommand : IRequest<BankTransferPaymentIntentResultModel>
{
    public required string ReferenceCode { get; init; }
    public required decimal Amount { get; init; }
    public required string BankId { get; init; }
    public required string AccountNo { get; init; }
    public required string ProviderTransactionId { get; init; }
    public required int Source { get; init; }
    public string? RawPayload { get; init; }
    public DateTime ConfirmedAtUtc { get; init; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Add handler**

Map the command into `ProcessBankTransferProviderTransactionAppDto` and return an existing `BankTransferPaymentIntentResultModel`.

- [ ] **Step 3: Create controller**

Create `BankTransferWebhookController` with:

```csharp
[AllowAnonymous]
[HttpPost]
public async Task<IActionResult> Receive([FromBody] ProcessBankTransferProviderTransactionCommand command)
```

Before sending MediatR command:

```csharp
if (!settings.Webhook.Enabled)
    return NotFound();

if (string.IsNullOrWhiteSpace(settings.Webhook.SecretToken))
    return StatusCode(StatusCodes.Status503ServiceUnavailable);

var token = Request.Headers["X-NamEcommerce-Webhook-Token"].ToString();
if (!string.Equals(token, settings.Webhook.SecretToken, StringComparison.Ordinal))
    return Unauthorized();
```

Return:

```csharp
return result.Success ? Ok(new { success = true, intent = result.Intent }) : BadRequest(new { success = false, message = result.ErrorMessage });
```

- [ ] **Step 4: Verify**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build succeeds with `0 errors`.

---

## Task 6: Add UI Status Polling

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/FastSales/FastSaleCommands.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/FastSales/FastSaleCommandHandlers.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/FastSaleController.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/FastSale/Index.cshtml`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/FastSale.js`

- [ ] **Step 1: Add status command**

```csharp
[Serializable]
public sealed class GetBankTransferPaymentIntentStatusCommand : IRequest<BankTransferPaymentIntentResultModel>
{
    public required Guid IntentId { get; init; }
}
```

- [ ] **Step 2: Add handler**

Handler calls:

```csharp
var result = await paymentIntentAppService.GetStatusAsync(request.IntentId).ConfigureAwait(false);
return FastSaleCommandHandlerMapper.MapIntentResult(result);
```

- [ ] **Step 3: Add controller action**

In `FastSaleController`:

```csharp
[HttpGet]
public async Task<IActionResult> GetPaymentIntentStatus(Guid intentId)
{
    var result = await mediator.Send(new GetBankTransferPaymentIntentStatusCommand { IntentId = intentId }).ConfigureAwait(false);
    if (!result.Success)
        return Json(new { success = false, message = LocalizeError(result.ErrorMessage ?? "Error.PaymentIntentIsNotFound") });

    return Json(new { success = true, intent = result.Intent });
}
```

- [ ] **Step 4: Add URL and status labels to view**

Add `data-status-url` to `#fastSaleApp`.

Add visible fields near the QR:

```html
<div id="fastSaleQrStatus" class="small text-muted"></div>
<div id="fastSaleQrExpires" class="small text-muted"></div>
```

- [ ] **Step 5: Update JavaScript polling**

Add URL:

```javascript
statusIntent: root.dataset.statusUrl
```

Add state:

```javascript
this.statusTimer = null;
```

After successful QR creation:

```javascript
this.startIntentPolling();
```

Implement:

```javascript
startIntentPolling() {
    this.stopIntentPolling();
    this.statusTimer = window.setInterval(() => this.refreshIntentStatus(), 3000);
    this.refreshIntentStatus();
}

stopIntentPolling() {
    if (this.statusTimer) window.clearInterval(this.statusTimer);
    this.statusTimer = null;
}

async refreshIntentStatus() {
    if (!this.paymentIntent) return;

    const params = new URLSearchParams({ intentId: this.paymentIntent.id });
    const response = await fetch(`${this.urls.statusIntent}?${params.toString()}`);
    const data = await response.json();

    if (!data.success) {
        this.showAlert('danger', data.message);
        this.stopIntentPolling();
        return;
    }

    this.paymentIntent = data.intent;
    this.paymentIntentConfirmed = this.paymentIntent.status === 20 || this.paymentIntent.status === 30;

    if (this.paymentIntentConfirmed) {
        this.showAlert('success', 'Đã xác nhận tiền vào tài khoản.');
        this.stopIntentPolling();
    }

    if (this.paymentIntent.status === 40 || this.paymentIntent.status === 50) {
        this.showAlert('warning', 'QR đã hết hạn hoặc đã bị hủy. Vui lòng tạo QR mới.');
        this.stopIntentPolling();
    }

    this.render();
}
```

Call `this.stopIntentPolling()` whenever cart, discount, warehouse, customer, or payment method changes.

- [ ] **Step 6: Verify**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build succeeds with `0 errors`.

---

## Task 7: Block Sale Completion For Expired Or Mismatched Intent

**Files:**
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/Orders/FastSaleAppService.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/Debts/BankTransferPaymentIntentAppService.cs`

- [ ] **Step 1: Expire before bank-transfer sale consumption**

In `CreateBankTransferQuickSaleAsync`, before consuming the intent, call the manager expiry method or app-service status method so an expired pending intent cannot be consumed.

Expected rejection:

```csharp
return QuickSaleResultAppDto.CreateError("Error.PaymentIntentCannotConsume");
```

- [ ] **Step 2: Re-check amount**

Keep the existing total comparison. The intent amount must equal `paidAmount` and the recalculated sale total.

Expected rejection:

```csharp
return QuickSaleResultAppDto.CreateError("Error.PaymentIntentAmountMismatch");
```

- [ ] **Step 3: Verify**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: build succeeds with `0 errors`.

---

## Task 8: Add Tests For Risky Money Flow

**Files:**
- Test: `NamEcommerce/Tests/NamEcommerce.Domain.Services.Test/Debts/BankTransferPaymentIntentManagerTests.cs`
- Test: `NamEcommerce/Tests/NamEcommerce.Application.Services.Test/Debts/BankTransferPaymentIntentAppServiceTests.cs`
- Test: `NamEcommerce/Tests/NamEcommerce.Application.Services.Test/Orders/FastSaleAppServiceTests.cs`

If the user still wants to skip tests for the next implementation pass, skip this task explicitly and run the manual verification commands in Task 9. For this phase, tests are recommended because this is money/accounting behavior.

- [ ] **Step 1: Domain tests**

Cover:

- Pending intent becomes `Expired` after `ExpiresAtUtc`.
- Confirmed or consumed intent does not expire.
- Duplicate `ProviderTransactionId` is rejected.
- Provider mismatch by amount/reference/account is rejected.

- [ ] **Step 2: Application tests**

Cover:

- Provider transaction creates a verification log even when no intent matches.
- Matching provider transaction marks log as `Matched` and intent as `Confirmed`.
- Duplicate provider transaction marks log as `Duplicate`.
- Expired intent cannot be used to complete bank-transfer fast sale.

- [ ] **Step 3: Run targeted tests**

```powershell
rtk dotnet test NamEcommerce\Tests\NamEcommerce.Domain.Services.Test
rtk dotnet test NamEcommerce\Tests\NamEcommerce.Application.Services.Test
```

Expected: all tests pass.

---

## Task 9: Manual Smoke Verification

**Files:**
- No code changes.

- [ ] **Step 1: Build**

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected: `0 errors`.

- [ ] **Step 2: Configure local bank transfer**

Use real or test values:

```json
"Payments": {
  "BankTransfer": {
    "Enabled": true,
    "BankId": "VCB",
    "AccountNo": "123456789",
    "AccountName": "CONG TY TNHH ABC",
    "Template": "compact2",
    "TransferContentPrefix": "QS",
    "IntentExpiryMinutes": 15,
    "StatusPollingSeconds": 3,
    "Verification": {
      "Provider": "None",
      "AllowManualConfirm": true
    },
    "Webhook": {
      "Enabled": true,
      "SecretToken": "dev-secret"
    }
  }
}
```

- [ ] **Step 3: Create QR from fast sale UI**

Expected:

- QR displays exact integer VND amount.
- Reference code is visible.
- Complete-sale button is disabled while intent is pending.
- Status label shows waiting state.

- [ ] **Step 4: Simulate provider confirmation**

POST to webhook endpoint with header:

```http
X-NamEcommerce-Webhook-Token: dev-secret
```

Body:

```json
{
  "referenceCode": "QS260605000001",
  "amount": 100000,
  "bankId": "VCB",
  "accountNo": "123456789",
  "providerTransactionId": "DEV-TXN-0001",
  "source": 20,
  "rawPayload": "{ \"dev\": true }",
  "confirmedAtUtc": "2026-06-05T10:00:00Z"
}
```

Expected:

- Webhook returns success for a matching pending intent.
- UI polling marks the QR as confirmed within one polling interval.
- Complete-sale button becomes enabled.
- Completing the sale creates order, delivery note, debt, payment, and consumes the intent.

- [ ] **Step 5: Simulate failure cases**

Send wrong amount, wrong reference code, missing token, and duplicate provider transaction id.

Expected:

- Missing token returns unauthorized.
- Wrong amount/reference does not confirm the intent.
- Duplicate provider transaction id does not confirm a second intent.
- Verification log records every event.

---

## Execution Order

1. Task 1: Settings.
2. Task 2: Intent expiry.
3. Task 3: Verification audit log.
4. Task 4: Application processing.
5. Task 5: Webhook endpoint.
6. Task 6: UI polling.
7. Task 7: Sale completion guards.
8. Task 8: Tests, unless explicitly skipped again.
9. Task 9: Manual smoke verification.

## Commit Plan

- Commit 1: `feat: add bank transfer intent expiry`
- Commit 2: `feat: log bank transfer verification events`
- Commit 3: `feat: process bank transfer webhook confirmations`
- Commit 4: `feat: poll fast sale bank transfer status`
- Commit 5: `test: cover bank transfer verification flow`

## Open Decision Before Provider-Specific Integration

To integrate a real automatic provider, choose one:

- Casso/webhook provider.
- Direct bank webhook/API.
- Statement polling provider.

After that decision, add a small adapter that maps provider payload fields into `ProcessBankTransferProviderTransactionCommand`. Do not change the fast sale flow again unless the provider cannot supply transaction id, amount, account, and content/reference reliably.
