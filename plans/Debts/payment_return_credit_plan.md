# Payment And Return Credit Note Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Separate real cash payments from return-based AR/AP credit notes so customer/vendor returns never silently FIFO-clear unrelated debts, while keeping the design compatible with a future accounting ledger.

**Architecture:** Treat returns that affect debt as internal `CreditNote` documents in the AR/AP subledger, not as `Payment` records. A credit note is settled through explicit allocation rows that point to one debt document at a time; when the return has a source document, allocation is limited to that source debt, and any excess/free return stays unapplied. `CustomerDebt`/`VendorDebt` keep `PaidAmount` as real cash movement and `RemainingAmount` as the current settlement projection; future double-entry accounting can post from these source documents without changing payment semantics again.

**Tech Stack:** ASP.NET Core, MediatR, EF Core SQL Server migrations, existing Clean Architecture + DDD module boundaries.

---

## Accounting Direction

- `CustomerPayment` and `VendorPayment` represent real money movement only: cash, bank transfer, or equivalent payment.
- `CustomerCreditNote` represents a customer-side adjustment caused by customer return value. It reduces Accounts Receivable only when allocated to a customer debt. If it is free/unmatched/excess, it remains unapplied customer credit.
- `VendorCreditNote` represents a vendor-side adjustment caused by vendor return value. It reduces Accounts Payable only when allocated to a vendor debt. If it is free/unmatched/excess, it remains unapplied vendor credit.
- `CreditNoteAllocation` is the audit link between a credit note and a specific debt. This is the source of traceability; do not infer settlement later from FIFO ordering.
- `CustomerDebt.RemainingAmount` and `VendorDebt.RemainingAmount` are current projections for operational screens. Long term, accounting reports should read from source documents and allocation rows, not from mutated debt totals alone.
- This plan does not build a legal VAT invoice, debit note, credit note issuance workflow, or GL journal module. It names the internal document `CreditNote` because that is the accounting-compatible concept we need in the subledger.

## Assumptions

- A real payment means money received from a customer or money paid to a vendor. Return value is not a payment.
- Existing `DebtStatus.FullyPaid` will continue to mean "fully settled" for now, even when settlement comes from payment plus credit note. Renaming the enum is a later cleanup.
- Customer return with `DeliveryNoteId != null && DeliveryNoteId != Guid.Empty` has a source delivery note. The matching debt source is `CustomerDebt.DeliveryNoteId`.
- Customer return without a valid `DeliveryNoteId` is a free return and must create unapplied customer credit note balance.
- Vendor return with `GoodsReceiptId` should allocate only to `VendorDebt.GoodsReceiptId`.
- Vendor return with no `GoodsReceiptId` but with `PurchaseOrderId` should allocate only to `VendorDebt.PurchaseOrderId`.
- Vendor return with neither source should create unapplied vendor credit note balance.
- Existing confirmed customer returns cannot be cancelled through `CustomerReturn.Cancel()` because the entity blocks `Confirmed -> Cancelled`. Therefore this iteration does not add customer credit-note reversal unless a real confirmed-return reversal flow is added.
- Existing vendor returns can be reversed through `VendorReturnManager.ReverseConfirmedAsync`; that reversal must restore only allocations created by the reversed vendor return.
- Existing historical return reductions cannot be reconstructed safely because current data does not store allocation rows. This plan applies the new behavior to new return confirmations after deployment.
- Existing `CustomerRefund` remains the cash-refund workflow. It should later refund/consume an explicit customer credit note, not rely on negative customer debt.

## Success Criteria

- Customer return from a delivery note reduces only the customer debt for that delivery note.
- Vendor return from a goods receipt or purchase order reduces only the vendor debt for that source.
- Free customer/vendor returns do not modify any existing debt automatically.
- If return value exceeds the source debt remaining amount, only the matching debt remainder is reduced and the excess stays as unapplied credit note balance.
- Flexible customer/vendor payments continue FIFO over positive remaining debts, but they never represent return value and never consume credit note balance.
- `CustomerDebt.PaidAmount` and `VendorDebt.PaidAmount` increase only from payment methods, never from return credit notes.
- Applying a payment after a credit-note allocation must decrease the current `RemainingAmount`; it must not recalculate `RemainingAmount = TotalAmount - PaidAmount` and lose the credit-note effect.
- Admin can see credit notes, allocation rows, reversed allocation rows, and unapplied credit note balance on customer/vendor debt detail screens.
- Every allocation stores credit note id/code, source return id/code, debt id/code, amount, applied timestamp, and optional reversal timestamp.
- Vendor return reversal restores only active allocations created by that return and marks the credit note cancelled/reversed for traceability.

## Non-Goals

- Do not rewrite old historical debts that were already reduced by return FIFO.
- Do not build a full double-entry accounting ledger.
- Do not build legal invoice/credit-note issuance or tax reporting.
- Do not build manual allocation of unapplied credit notes to future debts in this iteration.
- Do not change inventory return behavior.
- Do not change deposit/advance-payment behavior except to keep it separate from return credit notes.
- Do not auto-apply unapplied credit note balance to new debts in this iteration.

## File Map

Planning:
- Create during execution: `plans/Debts/payment_credit_note_implement.md`

Domain entities:
- Create `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/CustomerCreditNote.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/CustomerCreditNoteAllocation.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/VendorCreditNote.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/VendorCreditNoteAllocation.cs`
- Modify `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/CustomerDebt.cs`
- Modify `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/VendorDebt.cs`

Domain shared:
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/Debts/DebtStatus.cs`
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Debts/CustomerDebtDtos.cs`
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Debts/VendorDebtDtos.cs`
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Debts/ICustomerDebtManager.cs`
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Debts/IVendorDebtManager.cs`

Domain services:
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/Debts/CustomerDebtManager.cs`
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/Debts/VendorDebtManager.cs`
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/Returns/CustomerReturnManager.cs`
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/Returns/VendorReturnManager.cs`
- Modify debt extension mappers in `NamEcommerce/Domain/NamEcommerce.Domain.Services/Extensions/`

Application:
- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Debts/CustomerDebtAppDtos.cs`
- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Debts/VendorDebtAppDtos.cs`
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Debts/CustomerDebtAppService.cs`
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Debts/VendorDebtAppService.cs`
- Modify debt extension mappers in `NamEcommerce/Application/NamEcommerce.Application.Services/Extensions/`

Infrastructure:
- Create `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerCreditNoteMapping.cs`
- Create `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerCreditNoteAllocationMapping.cs`
- Create `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/VendorCreditNoteMapping.cs`
- Create `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/VendorCreditNoteAllocationMapping.cs`
- Create migration in `NamEcommerce/Migrations/NamEcommerce.Data.SqlServerMigrations/Migrations/`

Presentation:
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/Debts/DebtModels.cs`
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/Debts/VendorDebtModels.cs`
- Modify debt query handlers in `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Queries/Handlers/Debts/`
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Views/CustomerDebt/Details.cshtml`
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Views/VendorDebt/Details.cshtml`

Verification harness:
- Create temporary `.codex-build/debt-credit-note-harness/` during implementation only.

## Implementation TodoList

- [ ] Create an isolated worktree for implementation.
- [ ] Create `plans/Debts/payment_credit_note_implement.md` and track task progress there.
- [ ] Add a red regression harness for customer credit-note allocation.
- [ ] Add a red regression harness for vendor credit-note allocation and reversal.
- [ ] Add credit-note entities, allocation entities, enums, DTOs, and EF mappings.
- [ ] Add SQL migration for credit-note and allocation tables.
- [ ] Replace customer return FIFO debt reduction with source-debt-only credit-note allocation.
- [ ] Replace vendor return debt reduction/reversal with source-debt-only credit-note allocation and reversal.
- [ ] Fix payment application so later payments preserve prior credit-note allocations.
- [ ] Keep flexible payment as real-payment-only and add guard checks proving return value is not represented as payment.
- [ ] Surface unapplied credit notes and allocations on customer/vendor debt detail screens.
- [ ] Run targeted builds and diff checks.
- [ ] Remove temporary harness artifacts before commit.

---

## Task 1: Worktree, Baseline, And Implementation Log

**Files:**
- Create: `plans/Debts/payment_credit_note_implement.md`

- [ ] **Step 1: Detect current git state**

Run:

```powershell
rtk git status --short
rtk git branch --show-current
```

Expected:
- Note existing dirty files.
- Do not revert unrelated changes.

- [ ] **Step 2: Create isolated worktree from `dev-assistant`**

Run:

```powershell
rtk git worktree add .worktrees/codex-debt-credit-note -b codex/debt-credit-note dev-assistant
```

Expected:
- New worktree at `D:\Learning\NamTraining\training-ecommerce\.worktrees\codex-debt-credit-note`
- Branch `codex/debt-credit-note`

- [ ] **Step 3: Create implementation log**

Create `plans/Debts/payment_credit_note_implement.md`:

```markdown
# Payment And Return Credit Note Implementation

## Goal

Implement the approved credit-note plan from `plans/Debts/payment_return_credit_plan.md`.

## Progress

- [ ] Baseline checked
- [ ] Red harness created
- [ ] Domain model implemented
- [ ] EF mappings and migration added
- [ ] Customer return flow updated
- [ ] Vendor return flow updated
- [ ] Payment guardrails updated
- [ ] Admin read models and screens updated
- [ ] Verification complete

## Verification Notes

- Baseline:
- Red checks:
- Green checks:
- Manual checks:
```

- [ ] **Step 4: Verify baseline build for touched dependency graph**

Run from the worktree:

```powershell
rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain.Shared\NamEcommerce.Domain.Shared.csproj
rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain\NamEcommerce.Domain.csproj
rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain.Services\NamEcommerce.Domain.Services.csproj
rtk dotnet build NamEcommerce\Application\NamEcommerce.Application.Contracts\NamEcommerce.Application.Contracts.csproj
rtk dotnet build NamEcommerce\Application\NamEcommerce.Application.Services\NamEcommerce.Application.Services.csproj
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected:
- Builds complete or only show pre-existing warnings.
- Do not use solution build as the primary baseline if `NamEcommerce\NamEcommerce.sln` still contains projects that require .NET Framework MSBuild.

---

## Task 2: Red Harness For Credit-Note Contracts

**Files:**
- Create: `.codex-build/debt-credit-note-harness/DebtCreditNoteHarness.csproj`
- Create: `.codex-build/debt-credit-note-harness/Program.cs`

- [ ] **Step 1: Write a failing customer credit-note contract check**

Create a console harness that references Domain and Domain.Shared. The harness should assert these contracts by reflection before implementation:

```csharp
RequireType("NamEcommerce.Domain.Entities.Debts.CustomerCreditNote");
RequireType("NamEcommerce.Domain.Entities.Debts.CustomerCreditNoteAllocation");
RequireMethod("NamEcommerce.Domain.Shared.Services.Debts.ICustomerDebtManager",
    "ApplyCreditNoteFromCustomerReturnAsync");
RequireProperty("NamEcommerce.Domain.Shared.Dtos.Debts.CustomerDebtDto",
    "CreditNoteAllocations");
RequireProperty("NamEcommerce.Domain.Shared.Dtos.Debts.CustomerDebtsByCustomerDto",
    "UnappliedCreditNotes");
```

- [ ] **Step 2: Write a failing vendor credit-note contract check**

Extend `Program.cs`:

```csharp
RequireType("NamEcommerce.Domain.Entities.Debts.VendorCreditNote");
RequireType("NamEcommerce.Domain.Entities.Debts.VendorCreditNoteAllocation");
RequireMethod("NamEcommerce.Domain.Shared.Services.Debts.IVendorDebtManager",
    "ApplyCreditNoteFromVendorReturnAsync");
RequireMethod("NamEcommerce.Domain.Shared.Services.Debts.IVendorDebtManager",
    "ReverseCreditNoteFromVendorReturnAsync");
RequireProperty("NamEcommerce.Domain.Shared.Dtos.Debts.VendorDebtDto",
    "CreditNoteAllocations");
RequireProperty("NamEcommerce.Domain.Shared.Dtos.Debts.VendorDebtsByVendorDto",
    "UnappliedCreditNotes");
```

- [ ] **Step 3: Verify RED**

Run:

```powershell
rtk dotnet build .codex-build\debt-credit-note-harness\DebtCreditNoteHarness.csproj
rtk dotnet run --project .codex-build\debt-credit-note-harness\DebtCreditNoteHarness.csproj
```

Expected:
- Build may pass.
- Run fails with missing `CustomerCreditNote` or equivalent missing contract.

---

## Task 3: Credit-Note Domain Model

**Files:**
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/CustomerCreditNote.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/CustomerCreditNoteAllocation.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/VendorCreditNote.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/VendorCreditNoteAllocation.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/Debts/DebtStatus.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/CustomerDebt.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/VendorDebt.cs`

- [ ] **Step 1: Add credit-note enums**

Add to `DebtStatus.cs`:

```csharp
public enum CreditNoteStatus
{
    Unapplied = 10,
    PartiallyApplied = 20,
    FullyApplied = 30,
    Cancelled = 40
}

public enum CreditNoteSourceType
{
    CustomerReturn = 10,
    VendorReturn = 20
}
```

- [ ] **Step 2: Add customer credit-note aggregate**

Create `CustomerCreditNote` with these fields:

```csharp
public string Code { get; private set; }
public Guid CustomerId { get; private set; }
public string CustomerName { get; private set; }
public CreditNoteSourceType SourceType { get; private set; }
public Guid SourceReturnId { get; private set; }
public string SourceReturnCode { get; private set; }
public Guid? SourceDeliveryNoteId { get; private set; }
public decimal Amount { get; private set; }
public decimal AppliedAmount { get; private set; }
public decimal RemainingAmount { get; private set; }
public CreditNoteStatus Status { get; private set; }
public DateTime CreatedOnUtc { get; private set; }
public DateTime? UpdatedOnUtc { get; private set; }
public DateTime? CancelledOnUtc { get; private set; }
public IReadOnlyCollection<CustomerCreditNoteAllocation> Allocations { get; }
```

Required methods:

```csharp
internal CustomerCreditNoteAllocation AllocateToDebt(CustomerDebt debt, decimal amount, Guid? appliedByUserId);
internal void Cancel();
```

Rules:
- Constructor requires `amount > 0`.
- `AllocateToDebt` requires `amount > 0`.
- `AllocateToDebt` requires `amount <= RemainingAmount`.
- `AllocateToDebt` calls `debt.ApplyCreditNote(amount)`.
- `RemainingAmount = Amount - AppliedAmount`.
- Status transitions to `FullyApplied` only when remaining amount is zero.
- `Cancel` is allowed only when there are no active allocations.

- [ ] **Step 3: Add customer allocation entity**

Create `CustomerCreditNoteAllocation` with:

```csharp
public Guid Id { get; private set; }
public Guid CustomerCreditNoteId { get; private set; }
public string CustomerCreditNoteCode { get; private set; }
public Guid SourceReturnId { get; private set; }
public string SourceReturnCode { get; private set; }
public Guid CustomerDebtId { get; private set; }
public string CustomerDebtCode { get; private set; }
public decimal Amount { get; private set; }
public DateTime AppliedOnUtc { get; private set; }
public Guid? AppliedByUserId { get; private set; }
public DateTime? ReversedOnUtc { get; private set; }
public Guid? ReversedByUserId { get; private set; }
public string? ReverseReason { get; private set; }
public bool IsReversed => ReversedOnUtc.HasValue;
```

- [ ] **Step 4: Add vendor credit-note aggregate and allocation**

Create `VendorCreditNote` with these fields:

```csharp
public string Code { get; private set; }
public Guid VendorId { get; private set; }
public string VendorName { get; private set; }
public CreditNoteSourceType SourceType { get; private set; }
public Guid SourceReturnId { get; private set; }
public string SourceReturnCode { get; private set; }
public Guid? SourceGoodsReceiptId { get; private set; }
public Guid? SourcePurchaseOrderId { get; private set; }
public decimal Amount { get; private set; }
public decimal AppliedAmount { get; private set; }
public decimal RemainingAmount { get; private set; }
public CreditNoteStatus Status { get; private set; }
public DateTime CreatedOnUtc { get; private set; }
public DateTime? UpdatedOnUtc { get; private set; }
public DateTime? CancelledOnUtc { get; private set; }
public IReadOnlyCollection<VendorCreditNoteAllocation> Allocations { get; }
```

`VendorCreditNote.AllocateToDebt(VendorDebt debt, decimal amount, Guid? appliedByUserId)` calls `debt.ApplyCreditNote(amount)`.

Create `VendorCreditNoteAllocation` with:

```csharp
public Guid Id { get; private set; }
public Guid VendorCreditNoteId { get; private set; }
public string VendorCreditNoteCode { get; private set; }
public Guid SourceReturnId { get; private set; }
public string SourceReturnCode { get; private set; }
public Guid VendorDebtId { get; private set; }
public string VendorDebtCode { get; private set; }
public decimal Amount { get; private set; }
public DateTime AppliedOnUtc { get; private set; }
public Guid? AppliedByUserId { get; private set; }
public DateTime? ReversedOnUtc { get; private set; }
public Guid? ReversedByUserId { get; private set; }
public string? ReverseReason { get; private set; }
public bool IsReversed => ReversedOnUtc.HasValue;
```

- [ ] **Step 5: Add explicit credit-note methods on debts**

In `CustomerDebt` and `VendorDebt`, add:

```csharp
internal void ApplyCreditNote(decimal amount)
{
    if (amount <= 0) return;

    RemainingAmount -= amount;
    if (RemainingAmount < 0)
        RemainingAmount = 0;

    Status = RemainingAmount <= 0
        ? DebtStatus.FullyPaid
        : PaidAmount > 0 ? DebtStatus.PartiallyPaid : DebtStatus.Outstanding;

    UpdatedOnUtc = DateTime.UtcNow;
}
```

In `VendorDebt`, add reversal support:

```csharp
internal void ReverseCreditNote(decimal amount)
{
    if (amount <= 0) return;

    var maxRemaining = TotalAmount - PaidAmount;
    RemainingAmount += amount;
    if (RemainingAmount > maxRemaining)
        RemainingAmount = maxRemaining;

    Status = RemainingAmount <= 0
        ? DebtStatus.FullyPaid
        : PaidAmount > 0 ? DebtStatus.PartiallyPaid : DebtStatus.Outstanding;

    UpdatedOnUtc = DateTime.UtcNow;
}
```

Do not increase `PaidAmount` from credit-note application.

- [ ] **Step 6: Fix payment projection logic**

Change `ApplyPayment(decimal amount)` in both debts so payment reduces the current projection:

```csharp
internal void ApplyPayment(decimal amount)
{
    if (amount <= 0) return;

    PaidAmount += amount;
    RemainingAmount -= amount;
    if (RemainingAmount < 0)
        RemainingAmount = 0;

    Status = RemainingAmount <= 0
        ? DebtStatus.FullyPaid
        : DebtStatus.PartiallyPaid;

    UpdatedOnUtc = DateTime.UtcNow;
}
```

Reason:
- Existing `RemainingAmount = TotalAmount - PaidAmount` would erase previous credit-note allocations when a later payment is recorded.

- [ ] **Step 7: Run GREEN compile**

Run:

```powershell
rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain.Shared\NamEcommerce.Domain.Shared.csproj
rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain\NamEcommerce.Domain.csproj
```

Expected:
- 0 errors.

---

## Task 4: DTOs, Manager Interfaces, And Query Shapes

**Files:**
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Debts/CustomerDebtDtos.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Debts/VendorDebtDtos.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Debts/ICustomerDebtManager.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Debts/IVendorDebtManager.cs`

- [ ] **Step 1: Add credit-note DTOs**

Add customer DTOs:

```csharp
[Serializable]
public sealed record CustomerCreditNoteDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required Guid SourceReturnId { get; init; }
    public required string SourceReturnCode { get; init; }
    public Guid? SourceDeliveryNoteId { get; init; }
    public decimal Amount { get; init; }
    public decimal AppliedAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public CreditNoteStatus Status { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public IList<CustomerCreditNoteAllocationDto> Allocations { get; init; } = [];
}

[Serializable]
public sealed record CustomerCreditNoteAllocationDto
{
    public required Guid Id { get; init; }
    public required Guid CustomerCreditNoteId { get; init; }
    public required string CustomerCreditNoteCode { get; init; }
    public required Guid SourceReturnId { get; init; }
    public required string SourceReturnCode { get; init; }
    public required Guid CustomerDebtId { get; init; }
    public required string CustomerDebtCode { get; init; }
    public decimal Amount { get; init; }
    public DateTime AppliedOnUtc { get; init; }
    public Guid? AppliedByUserId { get; init; }
    public DateTime? ReversedOnUtc { get; init; }
    public string? ReverseReason { get; init; }
}
```

Add vendor DTOs:

```csharp
[Serializable]
public sealed record VendorCreditNoteDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public required Guid SourceReturnId { get; init; }
    public required string SourceReturnCode { get; init; }
    public Guid? SourceGoodsReceiptId { get; init; }
    public Guid? SourcePurchaseOrderId { get; init; }
    public decimal Amount { get; init; }
    public decimal AppliedAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public CreditNoteStatus Status { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public IList<VendorCreditNoteAllocationDto> Allocations { get; init; } = [];
}

[Serializable]
public sealed record VendorCreditNoteAllocationDto
{
    public required Guid Id { get; init; }
    public required Guid VendorCreditNoteId { get; init; }
    public required string VendorCreditNoteCode { get; init; }
    public required Guid SourceReturnId { get; init; }
    public required string SourceReturnCode { get; init; }
    public required Guid VendorDebtId { get; init; }
    public required string VendorDebtCode { get; init; }
    public decimal Amount { get; init; }
    public DateTime AppliedOnUtc { get; init; }
    public Guid? AppliedByUserId { get; init; }
    public DateTime? ReversedOnUtc { get; init; }
    public string? ReverseReason { get; init; }
}
```

- [ ] **Step 2: Extend debt detail DTOs**

Add to `CustomerDebtDto`:

```csharp
public IList<CustomerCreditNoteAllocationDto> CreditNoteAllocations { get; init; } = [];
```

Add to `CustomerDebtsByCustomerDto`:

```csharp
public decimal UnappliedCreditNoteBalance { get; init; }
public IList<CustomerCreditNoteDto> UnappliedCreditNotes { get; init; } = [];
```

Add to `VendorDebtDto`:

```csharp
public IList<VendorCreditNoteAllocationDto> CreditNoteAllocations { get; init; } = [];
```

Add to `VendorDebtsByVendorDto`:

```csharp
public decimal UnappliedCreditNoteBalance { get; init; }
public IList<VendorCreditNoteDto> UnappliedCreditNotes { get; init; } = [];
```

- [ ] **Step 3: Replace return debt methods on manager interfaces**

In `ICustomerDebtManager`, replace old return FIFO method with:

```csharp
Task<CustomerCreditNoteDto> ApplyCreditNoteFromCustomerReturnAsync(
    Guid customerId,
    Guid returnId,
    string returnCode,
    Guid? sourceDeliveryNoteId,
    decimal amount);
```

In `IVendorDebtManager`, replace old return FIFO/reversal methods with:

```csharp
Task<VendorCreditNoteDto> ApplyCreditNoteFromVendorReturnAsync(
    Guid vendorId,
    Guid returnId,
    string returnCode,
    Guid? sourceGoodsReceiptId,
    Guid? sourcePurchaseOrderId,
    decimal amount);

Task ReverseCreditNoteFromVendorReturnAsync(Guid returnId, string reason);
```

- [ ] **Step 4: Run compile**

Run:

```powershell
rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain.Shared\NamEcommerce.Domain.Shared.csproj
```

Expected:
- Domain.Shared builds.
- Downstream projects may fail until manager implementations are updated.

---

## Task 5: EF Mapping And Migration

**Files:**
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerCreditNoteMapping.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerCreditNoteAllocationMapping.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/VendorCreditNoteMapping.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/VendorCreditNoteAllocationMapping.cs`
- Create migration via EF CLI.

- [ ] **Step 1: Map credit-note tables**

Mapping requirements:
- Tables: `CustomerCreditNotes`, `CustomerCreditNoteAllocations`, `VendorCreditNotes`, `VendorCreditNoteAllocations`.
- `Code` required, unique, max length 100.
- Counterparty id indexed.
- `SourceReturnId` indexed and unique for active credit notes.
- Source document id indexed.
- Amount columns use `decimal(18,2)`.
- Allocation tables index credit note id and debt id.
- Allocation reverse fields are nullable.

- [ ] **Step 2: Add migration**

Run:

```powershell
rtk dotnet ef migrations add DebtCreditNotes --project NamEcommerce\Migrations\NamEcommerce.Data.SqlServerMigrations\NamEcommerce.Data.SqlServerMigrations.csproj --startup-project NamEcommerce\Migrations\NamEcommerce.Data.SqlServerMigrations\NamEcommerce.Data.SqlServerMigrations.csproj --context NamEcommerce.Data.SqlServer.NamEcommerceEfDbContext --output-dir Migrations
```

Expected:
- Migration creates four new tables and indexes.
- Migration does not update old debt amounts.

- [ ] **Step 3: Build migration project**

Run:

```powershell
rtk dotnet build NamEcommerce\Migrations\NamEcommerce.Data.SqlServerMigrations\NamEcommerce.Data.SqlServerMigrations.csproj
```

Expected:
- 0 errors.

---

## Task 6: Customer Return Credit-Note Logic

**Files:**
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Debts/CustomerDebtManager.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Returns/CustomerReturnManager.cs`

- [ ] **Step 1: Add repositories/readers to `CustomerDebtManager`**

Inject:

```csharp
IRepository<CustomerCreditNote> creditNoteRepository,
IEntityDataReader<CustomerCreditNote> creditNoteReader
```

- [ ] **Step 2: Implement customer credit-note code generation**

Add:

```csharp
private Task<string> GenerateCustomerCreditNoteCodeAsync()
{
    var monthPrefix = $"CN-KH-{DateTime.UtcNow:yyMM}";
    var count = creditNoteReader.SecuredDataSource.Count(c => c.Code.StartsWith(monthPrefix));
    return Task.FromResult($"{monthPrefix}-{(count + 1):D3}");
}
```

- [ ] **Step 3: Implement source-debt-only application**

Implement `ApplyCreditNoteFromCustomerReturnAsync`:

```csharp
var existing = creditNoteReader.DataSource
    .FirstOrDefault(c => c.SourceReturnId == returnId && c.Status != CreditNoteStatus.Cancelled);
if (existing is not null)
    return existing.ToDto();

var creditNote = new CustomerCreditNote(
    await GenerateCustomerCreditNoteCodeAsync().ConfigureAwait(false),
    customerId,
    customerName,
    returnId,
    returnCode,
    sourceDeliveryNoteId,
    amount,
    currentUserAccessor.GetCurrentUser()?.Id);

if (sourceDeliveryNoteId.HasValue && sourceDeliveryNoteId.Value != Guid.Empty)
{
    var sourceDebt = debtReader.DataSource
        .FirstOrDefault(d => d.CustomerId == customerId
            && d.DeliveryNoteId == sourceDeliveryNoteId.Value
            && d.RemainingAmount > 0);

    if (sourceDebt is not null)
    {
        var applyAmount = Math.Min(creditNote.RemainingAmount, sourceDebt.RemainingAmount);
        creditNote.AllocateToDebt(sourceDebt, applyAmount, currentUserAccessor.GetCurrentUser()?.Id);
        sourceDebt.MarkUpdated();
        await debtRepository.UpdateAsync(sourceDebt).ConfigureAwait(false);
    }
}

var inserted = await creditNoteRepository.InsertAsync(creditNote).ConfigureAwait(false);
return inserted.ToDto();
```

Important:
- Do not loop over other customer debts.
- Do not create negative customer debt.
- Do not increase `CustomerDebt.PaidAmount`.
- Do not create `CustomerPayment`.
- Do not raise `CustomerReturnOverRefunded` from this path. Excess is `CustomerCreditNote.RemainingAmount`.

- [ ] **Step 4: Change customer return finalization**

In `CustomerReturnManager.FinalizeConfirmAsync`, replace:

```csharp
await customerDebtManager.ApplyReturnFromCustomerReturnAsync(
    customerReturn.CustomerId,
    customerReturn.Id,
    netRefundAmount).ConfigureAwait(false);
```

with:

```csharp
await customerDebtManager.ApplyCreditNoteFromCustomerReturnAsync(
    customerReturn.CustomerId,
    customerReturn.Id,
    customerReturn.Code,
    customerReturn.DeliveryNoteId == Guid.Empty ? null : customerReturn.DeliveryNoteId,
    netRefundAmount).ConfigureAwait(false);
```

Remove the `MarkOverRefunded` branch from this path because over-return value is now explicit unapplied credit-note balance.

- [ ] **Step 5: Build domain services**

Run:

```powershell
rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain.Services\NamEcommerce.Domain.Services.csproj
```

Expected:
- 0 errors.

---

## Task 7: Vendor Return Credit-Note Logic And Reversal

**Files:**
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Debts/VendorDebtManager.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Returns/VendorReturnManager.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Extensions/VendorDebtExtensions.cs`

- [ ] **Step 1: Add repositories/readers to `VendorDebtManager`**

Inject:

```csharp
IRepository<VendorCreditNote> creditNoteRepository,
IEntityDataReader<VendorCreditNote> creditNoteReader
```

- [ ] **Step 2: Implement vendor source-debt resolver**

Resolution order:

```csharp
VendorDebt? sourceDebt;
if (sourceGoodsReceiptId.HasValue)
{
    sourceDebt = debtReader.DataSource.FirstOrDefault(d => d.VendorId == vendorId
        && d.GoodsReceiptId == sourceGoodsReceiptId.Value
        && d.RemainingAmount > 0);
}
else if (sourcePurchaseOrderId.HasValue)
{
    sourceDebt = debtReader.DataSource.FirstOrDefault(d => d.VendorId == vendorId
        && d.PurchaseOrderId == sourcePurchaseOrderId.Value
        && d.RemainingAmount > 0);
}
else
{
    sourceDebt = null;
}
```

- [ ] **Step 3: Implement `ApplyCreditNoteFromVendorReturnAsync`**

Rules:
- Idempotent by `SourceReturnId`.
- Apply only to the resolved source debt.
- Excess remains in `VendorCreditNote.RemainingAmount`.
- No FIFO over other vendor debts.
- Do not increase `VendorDebt.PaidAmount`.
- Do not create `VendorPayment`.
- Do not raise `VendorDebtBecameNegative`.

- [ ] **Step 4: Implement `ReverseCreditNoteFromVendorReturnAsync`**

Find credit note by `SourceReturnId`.

Rules:
- If credit note does not exist, return without error so vendor return reversal remains idempotent.
- For each active allocation on that credit note, load its `VendorDebt`, call `VendorDebt.ReverseCreditNote(allocation.Amount)`, mark the allocation reversed with the same reason, and update the debt.
- After all active allocations are reversed, mark the credit note `Cancelled`.
- Do not delete allocation rows.
- Do not touch any debt that is not referenced by the credit note's active allocations.

Required call shape:

```csharp
await vendorDebtManager.ReverseCreditNoteFromVendorReturnAsync(vendorReturn.Id, reason)
    .ConfigureAwait(false);
```

- [ ] **Step 5: Change vendor return manager**

In confirm finalization, replace `ApplyReturnFromVendorReturnAsync` with `ApplyCreditNoteFromVendorReturnAsync`.

In `ReverseConfirmedAsync`, replace `ReverseReturnFromVendorReturnAsync(goodsReceiptId, purchaseOrderId, amount)` with `ReverseCreditNoteFromVendorReturnAsync(vendorReturn.Id, reason)`.

- [ ] **Step 6: Build domain services**

Run:

```powershell
rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain.Services\NamEcommerce.Domain.Services.csproj
```

Expected:
- 0 errors.

---

## Task 8: Payment Flow Guardrails

**Files:**
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Debts/CustomerDebtManager.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Debts/VendorDebtManager.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/CustomerDebt.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Debts/VendorDebt.cs`

- [ ] **Step 1: Keep flexible payment real-payment-only**

In both flexible payment methods, keep selecting only debts with:

```csharp
RemainingAmount > 0
```

Do not include credit-note balances in the pending-debt query.

- [ ] **Step 2: Add short comments where payment allocation happens**

Add before pending debt query:

```csharp
// Real payments reduce debt documents only. Return value is settled through CreditNote allocations.
```

- [ ] **Step 3: Add payment overpay guards**

For targeted customer payment and targeted vendor payment:

```csharp
if (payment.Amount > debt.RemainingAmount)
    throw new NamEcommerceDomainException("Error.PaymentAmountExceedsRemaining", payment.Amount, debt.RemainingAmount);
```

For flexible payment, each FIFO allocation must use:

```csharp
var appliedAmount = Math.Min(remainingPaymentAmount, debt.RemainingAmount);
```

- [ ] **Step 4: Verify payment after credit note**

Add a harness assertion or focused test setup:

```csharp
// Given total debt 1,000, credit note allocation 300, then payment 100
// Expected: PaidAmount = 100, RemainingAmount = 600
// Not expected: RemainingAmount = 900
```

- [ ] **Step 5: Build**

Run:

```powershell
rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain.Services\NamEcommerce.Domain.Services.csproj
```

Expected:
- 0 errors.

---

## Task 9: Application Mapping And Admin Read Models

**Files:**
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Debts/CustomerDebtAppDtos.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Debts/VendorDebtAppDtos.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/Extensions/DebtExtensions.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/Extensions/VendorDebtExtensions.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/Debts/CustomerDebtAppService.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/Debts/VendorDebtAppService.cs`

- [ ] **Step 1: Add app DTOs**

Add:

```csharp
public sealed record CustomerCreditNoteAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required Guid SourceReturnId { get; init; }
    public required string SourceReturnCode { get; init; }
    public Guid? SourceDeliveryNoteId { get; init; }
    public decimal Amount { get; init; }
    public decimal AppliedAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public CreditNoteStatus Status { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public IList<CustomerCreditNoteAllocationAppDto> Allocations { get; init; } = [];
}

public sealed record CustomerCreditNoteAllocationAppDto
{
    public required Guid Id { get; init; }
    public required Guid CustomerCreditNoteId { get; init; }
    public required string CustomerCreditNoteCode { get; init; }
    public required Guid SourceReturnId { get; init; }
    public required string SourceReturnCode { get; init; }
    public required Guid CustomerDebtId { get; init; }
    public required string CustomerDebtCode { get; init; }
    public decimal Amount { get; init; }
    public DateTime AppliedOnUtc { get; init; }
    public Guid? AppliedByUserId { get; init; }
    public DateTime? ReversedOnUtc { get; init; }
    public string? ReverseReason { get; init; }
}

public sealed record VendorCreditNoteAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public required Guid SourceReturnId { get; init; }
    public required string SourceReturnCode { get; init; }
    public Guid? SourceGoodsReceiptId { get; init; }
    public Guid? SourcePurchaseOrderId { get; init; }
    public decimal Amount { get; init; }
    public decimal AppliedAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public CreditNoteStatus Status { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public IList<VendorCreditNoteAllocationAppDto> Allocations { get; init; } = [];
}

public sealed record VendorCreditNoteAllocationAppDto
{
    public required Guid Id { get; init; }
    public required Guid VendorCreditNoteId { get; init; }
    public required string VendorCreditNoteCode { get; init; }
    public required Guid SourceReturnId { get; init; }
    public required string SourceReturnCode { get; init; }
    public required Guid VendorDebtId { get; init; }
    public required string VendorDebtCode { get; init; }
    public decimal Amount { get; init; }
    public DateTime AppliedOnUtc { get; init; }
    public Guid? AppliedByUserId { get; init; }
    public DateTime? ReversedOnUtc { get; init; }
    public string? ReverseReason { get; init; }
}
```

- [ ] **Step 2: Extend summary DTOs**

Expose on both customer and vendor detail/summary app DTOs:

```csharp
public decimal UnappliedCreditNoteBalance { get; init; }
```

Expose unapplied list:

```csharp
public IList<CustomerCreditNoteAppDto> UnappliedCreditNotes { get; init; } = [];
public IList<VendorCreditNoteAppDto> UnappliedCreditNotes { get; init; } = [];
```

- [ ] **Step 3: Map domain DTOs to app DTOs**

Map:
- Credit note id/code/source return/source debt fields.
- Amount, applied amount, remaining amount.
- Allocation rows including reversed metadata.

- [ ] **Step 4: Build application services**

Run:

```powershell
rtk dotnet build NamEcommerce\Application\NamEcommerce.Application.Contracts\NamEcommerce.Application.Contracts.csproj
rtk dotnet build NamEcommerce\Application\NamEcommerce.Application.Services\NamEcommerce.Application.Services.csproj
```

Expected:
- 0 errors.

---

## Task 10: Presentation Display

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/Debts/DebtModels.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/Debts/VendorDebtModels.cs`
- Modify: debt detail query handlers in `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Queries/Handlers/Debts/`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/CustomerDebt/Details.cshtml`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/VendorDebt/Details.cshtml`

- [ ] **Step 1: Add credit-note sections to detail models**

Customer detail model:

```csharp
public decimal UnappliedCreditNoteBalance { get; set; }
public IList<CustomerCreditNoteModel> UnappliedCreditNotes { get; set; } = [];
```

Vendor detail model:

```csharp
public decimal UnappliedCreditNoteBalance { get; set; }
public IList<VendorCreditNoteModel> UnappliedCreditNotes { get; set; } = [];
```

- [ ] **Step 2: Add allocation list per debt**

Each debt row/detail should expose:

```csharp
public IList<CreditNoteAllocationModel> CreditNoteAllocations { get; set; } = [];
```

- [ ] **Step 3: Render admin credit-note panel**

Add a compact panel:

- Title: `Chứng từ điều chỉnh từ trả hàng chưa cấn trừ`
- Columns: `Mã trả hàng`, `Mã chứng từ`, `Số tiền gốc`, `Đã cấn trừ`, `Còn lại`, `Ngày tạo`
- Empty text: `Không có chứng từ trả hàng nào đang chờ cấn trừ.`

- [ ] **Step 4: Render allocation under debt rows**

For debts with active allocations, show small rows:

```text
Cấn trừ từ trả hàng {ReturnCode}: {Amount}
```

For reversed allocations, show muted rows:

```text
Đã đảo cấn trừ từ trả hàng {ReturnCode}: {Amount}
```

- [ ] **Step 5: Build Web**

Run:

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected:
- 0 errors.

---

## Task 11: Verification And Cleanup

**Files:**
- Delete temporary `.codex-build/debt-credit-note-harness/` before commit.

- [ ] **Step 1: Run GREEN harness**

Run:

```powershell
rtk dotnet run --project .codex-build\debt-credit-note-harness\DebtCreditNoteHarness.csproj
```

Expected:
- Contract checks pass.

- [ ] **Step 2: Run targeted builds**

Run:

```powershell
rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain.Shared\NamEcommerce.Domain.Shared.csproj
rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain\NamEcommerce.Domain.csproj
rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain.Services\NamEcommerce.Domain.Services.csproj
rtk dotnet build NamEcommerce\Application\NamEcommerce.Application.Contracts\NamEcommerce.Application.Contracts.csproj
rtk dotnet build NamEcommerce\Application\NamEcommerce.Application.Services\NamEcommerce.Application.Services.csproj
rtk dotnet build NamEcommerce\Infrastructure\NamEcommerce.Data.SqlServer\NamEcommerce.Data.SqlServer.csproj
rtk dotnet build NamEcommerce\Migrations\NamEcommerce.Data.SqlServerMigrations\NamEcommerce.Data.SqlServerMigrations.csproj
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web.Contracts\NamEcommerce.Web.Contracts.csproj
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web.Framework\NamEcommerce.Web.Framework.csproj
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Expected:
- 0 errors.

- [ ] **Step 3: Run diff check**

Run:

```powershell
rtk git diff --check
```

Expected:
- No whitespace errors.

- [ ] **Step 4: Manual functional checks**

Use a local database and verify:

- Customer return tied to delivery note with debt `1,000,000`, return `300,000`:
  - Debt remaining decreases by `300,000`.
  - Debt paid amount stays unchanged.
  - Credit note exists with `Amount=300,000`, `AppliedAmount=300,000`, `RemainingAmount=0`.
  - Allocation points to that `CustomerDebtId`.
- Customer return tied to delivery note with debt remaining `100,000`, return `300,000`:
  - Debt remaining becomes `0`.
  - Credit note exists with `AppliedAmount=100,000`, `RemainingAmount=200,000`.
  - No other customer debt changes.
- Free customer return:
  - No debt changes.
  - Credit note exists with full amount remaining.
- Vendor return tied to goods receipt:
  - Only goods receipt debt changes.
  - Debt paid amount stays unchanged.
- Free vendor return:
  - No vendor debt changes.
  - Vendor credit note exists with full amount remaining.
- Vendor return reversal:
  - Active allocations from that vendor return are marked reversed.
  - The referenced vendor debt remaining amount is restored.
  - Other vendor debts do not change.
- Payment after credit-note allocation:
  - Debt total `1,000,000`, credit-note allocation `300,000`, payment `100,000`.
  - Expected `PaidAmount=100,000`, `RemainingAmount=600,000`.
- Flexible payment:
  - Creates payment records only.
  - Does not create or consume credit note records.

- [ ] **Step 5: Remove temporary harness**

Run with guarded path:

```powershell
$target = Resolve-Path -LiteralPath '.codex-build\debt-credit-note-harness' -ErrorAction SilentlyContinue
if ($target -and $target.Path.Contains('\.codex-build\debt-credit-note-harness')) {
    Remove-Item -LiteralPath $target.Path -Recurse -Force
}
```

- [ ] **Step 6: Commit**

Run:

```powershell
rtk git add NamEcommerce plans/Debts/payment_credit_note_implement.md
rtk git commit -m "Tach credit note tra hang khoi payment cong no"
```

Expected:
- Commit contains only debt/payment/return credit-note changes, migration, and implementation log if intentionally force-added.
- Do not stage unrelated dirty files from the parent worktree.

## Risks And Notes

- Historical records are not corrected. Old return reductions remain mixed into debt totals because there is no reliable allocation history to reverse safely.
- Existing `ApplyPayment` recalculates remaining from total and paid amount. This must change in the same implementation because credit-note allocation makes `RemainingAmount` a settlement projection.
- Existing `CustomerRefund` should be reviewed after this change. The accounting-compatible next step is an explicit refund action that consumes unapplied `CustomerCreditNote` balance.
- Vendor cash refund from supplier is not currently modeled as a payment direction. This plan stores vendor return value as vendor credit note balance first; later cash receipt from supplier should be a separate finance feature.
- `CreditNote` here is an internal AR/AP subledger document. If the business later needs tax-compliant credit/debit invoices, build that as a separate legal-document workflow and link it to these credit notes.
