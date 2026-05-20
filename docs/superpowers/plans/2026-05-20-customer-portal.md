# Customer Portal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a separate customer portal with React + TypeScript frontend, ASP.NET Core REST API backend, QR delivery-note access, session-only customer authentication, OTP abuse protection, order/return request workflows, and mock payment intents pending admin reconciliation.

**Architecture:** Add customer-specific presentation projects instead of reusing admin `NamEcommerce.Web.Contracts` or `NamEcommerce.Web.Framework`. Reuse the existing Application, Domain, and Infrastructure layers, but expose only customer-safe contracts and handlers through `NamEcommerce.Customer.Api`.

**Tech Stack:** .NET 10, ASP.NET Core Web API, EF Core SQL Server, MediatR, BCrypt via existing `ISecurityService`, React latest stable + TypeScript + Vite, plain CSS/CSS modules, browser `fetch` with `credentials: "include"`.

---

## Scope Rules

- Do not add or edit any `*.Test` project.
- Do not run `Add-Migration`, `dotnet ef migrations add`, `Update-Database`, or `dotnet ef database update`.
- Do not reference or register `NamEcommerce.Web.Framework` in the customer API.
- Do not expose admin commands or query models through the customer API.
- All authenticated customer API operations derive `CustomerId` from the session, never from request body.
- Keep customer-created order, return, and payment flows as requests/intents until admin approves or reconciles them.

## Plan Split

The spec covers several subsystems. Implement in this order:

1. Customer portal domain foundation and EF mappings.
2. Customer application services, providers, session validation, and anti-abuse rules.
3. Customer REST API contracts, handlers, controllers, middleware, and DI.
4. React SPA shell, public QR page, OTP/password auth, and session handling.
5. Authenticated portal features: orders, delivery notes, feedback, return requests, debts, mock payments.
6. Internal admin screens for request review, account blocking, security events, and payment reconciliation.
7. Verification and migration handoff notes.

Each phase should build successfully before moving to the next.

## File Map

### New Customer Presentation Projects

- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Contracts/NamEcommerce.Customer.Contracts.csproj`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Framework/NamEcommerce.Customer.Framework.csproj`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/NamEcommerce.Customer.Api.csproj`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Program.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Controllers/*`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Infrastructure/*`
- Modify: `NamEcommerce/NamEcommerce.sln`

### New React Client

- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Client/package.json`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Client/vite.config.ts`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Client/tsconfig.json`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Client/index.html`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Client/src/*`

### Domain

- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/CustomerPortal/CustomerPortalAccount.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/CustomerPortal/CustomerOtpChallenge.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/CustomerPortal/CustomerPortalSession.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/CustomerPortal/CustomerSecurityEvent.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/CustomerPortal/DeliveryNoteAccessToken.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/CustomerPortal/CustomerDeliveryFeedback.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/CustomerPortal/CustomerOrderRequest.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/CustomerPortal/CustomerOrderRequestItem.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/CustomerPortal/CustomerReturnRequest.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/CustomerPortal/CustomerReturnRequestItem.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/CustomerPortal/CustomerPaymentIntent.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/CustomerPortal/CustomerPortalEnums.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/CustomerPortal/CustomerPortalDtos.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/CustomerPortal/ICustomerPortalManager.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/CustomerPortal/ICustomerPortalSecurityManager.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Services/CustomerPortal/CustomerPortalManager.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Services/CustomerPortal/CustomerPortalSecurityManager.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Extensions/CustomerPortalExtensions.cs`

### Infrastructure

- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerPortal/CustomerPortalAccountMapping.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerPortal/CustomerOtpChallengeMapping.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerPortal/CustomerPortalSessionMapping.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerPortal/CustomerSecurityEventMapping.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerPortal/DeliveryNoteAccessTokenMapping.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerPortal/CustomerDeliveryFeedbackMapping.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerPortal/CustomerOrderRequestMapping.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerPortal/CustomerOrderRequestItemMapping.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerPortal/CustomerReturnRequestMapping.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerPortal/CustomerReturnRequestItemMapping.cs`
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerPortal/CustomerPaymentIntentMapping.cs`

### Application

- Create: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/CustomerPortal/CustomerPortalAppDtos.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Contracts/CustomerPortal/ICustomerPortalAppService.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Contracts/CustomerPortal/ICustomerPortalAuthAppService.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Contracts/CustomerPortal/ICustomerPortalPaymentAppService.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Contracts/CustomerPortal/ICustomerOtpSender.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Contracts/CustomerPortal/ICustomerPaymentProvider.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/CustomerPortal/CustomerPortalAppService.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/CustomerPortal/CustomerPortalAuthAppService.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/CustomerPortal/CustomerPortalPaymentAppService.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/CustomerPortal/MockSmsOtpSender.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/CustomerPortal/MockEmailOtpSender.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/CustomerPortal/MockCustomerPaymentProvider.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/Extensions/CustomerPortalAppExtensions.cs`

### Customer Contracts And Framework

- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Contracts/Models/*`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Contracts/Commands/*`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Contracts/Queries/*`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Framework/Commands/Handlers/*`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Framework/Queries/Handlers/*`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Framework/Services/*`

### Admin Web Additions

- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/CustomerController.cs`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/CustomerPortalController.cs`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Models/CustomerPortal/*`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Services/CustomerPortal/*`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Views/CustomerPortal/*`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/CustomerPortal/*`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Queries/Models/CustomerPortal/*`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/CustomerPortal/*`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/CustomerPortal/*`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Queries/Handlers/CustomerPortal/*`

---

## Task 1: Scaffold Customer Projects

**Files:**
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Contracts/NamEcommerce.Customer.Contracts.csproj`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Framework/NamEcommerce.Customer.Framework.csproj`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/NamEcommerce.Customer.Api.csproj`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Program.cs`
- Modify: `NamEcommerce/NamEcommerce.sln`

- [ ] **Step 1: Create customer contracts project**

Create `NamEcommerce.Customer.Contracts.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="14.1.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create customer framework project**

Create `NamEcommerce.Customer.Framework.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\Application\NamEcommerce.Application.Contracts\NamEcommerce.Application.Contracts.csproj" />
    <ProjectReference Include="..\NamEcommerce.Customer.Contracts\NamEcommerce.Customer.Contracts.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="14.1.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create customer API project**

Create `NamEcommerce.Customer.Api.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.4" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.7" />
    <PackageReference Include="MediatR" Version="14.1.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\Application\NamEcommerce.Application.Contracts\NamEcommerce.Application.Contracts.csproj" />
    <ProjectReference Include="..\..\..\Application\NamEcommerce.Application.Services\NamEcommerce.Application.Services.csproj" />
    <ProjectReference Include="..\..\..\Domain\NamEcommerce.Domain.Services\NamEcommerce.Domain.Services.csproj" />
    <ProjectReference Include="..\..\..\Domain\NamEcommerce.Domain.Shared\NamEcommerce.Domain.Shared.csproj" />
    <ProjectReference Include="..\..\..\Infrastructure\NamEcommerce.Data.SqlServer\NamEcommerce.Data.SqlServer.csproj" />
    <ProjectReference Include="..\NamEcommerce.Customer.Contracts\NamEcommerce.Customer.Contracts.csproj" />
    <ProjectReference Include="..\NamEcommerce.Customer.Framework\NamEcommerce.Customer.Framework.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Add initial API host**

Create `Program.cs`:

```csharp
using NamEcommerce.Customer.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomerPortalApi(builder.Configuration);

var app = builder.Build();

app.UseCustomerPortalApi();

app.Run();
```

- [ ] **Step 5: Add projects to solution**

Run:

```powershell
rtk dotnet sln NamEcommerce\NamEcommerce.sln add `
  NamEcommerce\Presentation\Customer\NamEcommerce.Customer.Contracts\NamEcommerce.Customer.Contracts.csproj `
  NamEcommerce\Presentation\Customer\NamEcommerce.Customer.Framework\NamEcommerce.Customer.Framework.csproj `
  NamEcommerce\Presentation\Customer\NamEcommerce.Customer.Api\NamEcommerce.Customer.Api.csproj
```

Expected: projects are added to the solution.

- [ ] **Step 6: Build**

Run:

```powershell
rtk dotnet build NamEcommerce\NamEcommerce.sln
```

Expected: build succeeds after empty project scaffolding.

---

## Task 2: Add Customer Portal Domain Foundation

**Files:**
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/CustomerPortal/CustomerPortalEnums.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/CustomerPortal/*.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/CustomerPortal/CustomerPortalDtos.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/CustomerPortal/ICustomerPortalSecurityManager.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/CustomerPortal/ICustomerPortalManager.cs`

- [ ] **Step 1: Create enums**

Create `CustomerPortalEnums.cs`:

```csharp
namespace NamEcommerce.Domain.Shared.Enums.CustomerPortal;

public enum CustomerPortalAccountStatus
{
    Active = 0,
    Blocked = 1
}

public enum CustomerOtpChannel
{
    Sms = 0,
    Email = 1
}

public enum CustomerOtpChallengeStatus
{
    Pending = 0,
    Verified = 1,
    Expired = 2,
    Locked = 3,
    Cancelled = 4
}

public enum CustomerPortalSessionStatus
{
    Active = 0,
    Revoked = 1,
    Expired = 2
}

public enum CustomerPortalSecurityEventOutcome
{
    Succeeded = 0,
    Failed = 1,
    Blocked = 2
}

public enum CustomerOrderRequestStatus
{
    PendingApproval = 0,
    Approved = 1,
    Rejected = 2,
    ConvertedToOrder = 3,
    Cancelled = 4
}

public enum CustomerReturnRequestStatus
{
    PendingReview = 0,
    Accepted = 1,
    Rejected = 2,
    ConvertedToReturn = 3,
    Cancelled = 4
}

public enum CustomerPaymentIntentStatus
{
    Created = 0,
    Processing = 1,
    SucceededPendingReconciliation = 2,
    Failed = 3,
    Cancelled = 4,
    Reconciled = 5
}
```

- [ ] **Step 2: Create account entity**

Create `CustomerPortalAccount.cs`:

```csharp
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerPortalAccount : AppAggregateEntity
{
    private CustomerPortalAccount() : base(Guid.NewGuid()) { }

    internal CustomerPortalAccount(Guid customerId) : base(Guid.NewGuid())
    {
        CustomerId = customerId;
        Status = CustomerPortalAccountStatus.Active;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? PasswordSalt { get; private set; }
    public CustomerPortalAccountStatus Status { get; private set; }
    public DateTime? PasswordSetOnUtc { get; private set; }
    public DateTime? LastLoginOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    internal bool IsBlocked() => Status == CustomerPortalAccountStatus.Blocked;

    internal void SetPassword(string passwordHash, string passwordSalt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordSalt);

        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        PasswordSetOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void MarkLoginSucceeded()
    {
        LastLoginOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Block()
    {
        Status = CustomerPortalAccountStatus.Blocked;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Unblock()
    {
        Status = CustomerPortalAccountStatus.Active;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
```

- [ ] **Step 3: Create OTP challenge entity**

Create `CustomerOtpChallenge.cs` with:

```csharp
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerOtpChallenge : AppAggregateEntity
{
    private const int MaxAttempts = 5;

    private CustomerOtpChallenge() : base(Guid.NewGuid()) { }

    internal CustomerOtpChallenge(
        Guid customerId,
        Guid deliveryNoteId,
        CustomerOtpChannel channel,
        string otpHash,
        DateTime expiresOnUtc,
        string? requestedIp,
        string? requestedUserAgent,
        string? sentToMasked) : base(Guid.NewGuid())
    {
        CustomerId = customerId;
        DeliveryNoteId = deliveryNoteId;
        Channel = channel;
        OtpHash = otpHash;
        ExpiresOnUtc = expiresOnUtc;
        RequestedIp = requestedIp;
        RequestedUserAgent = requestedUserAgent;
        SentToMasked = sentToMasked;
        Status = CustomerOtpChallengeStatus.Pending;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }
    public Guid DeliveryNoteId { get; private set; }
    public CustomerOtpChannel Channel { get; private set; }
    public string OtpHash { get; private set; } = string.Empty;
    public DateTime ExpiresOnUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public CustomerOtpChallengeStatus Status { get; private set; }
    public string? RequestedIp { get; private set; }
    public string? RequestedUserAgent { get; private set; }
    public string? SentToMasked { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? VerifiedOnUtc { get; private set; }

    internal bool CanVerify(DateTime nowUtc)
        => Status == CustomerOtpChallengeStatus.Pending
            && nowUtc <= ExpiresOnUtc
            && AttemptCount < MaxAttempts;

    internal void MarkVerifyFailed(DateTime nowUtc)
    {
        AttemptCount++;
        if (nowUtc > ExpiresOnUtc)
        {
            Status = CustomerOtpChallengeStatus.Expired;
            return;
        }

        if (AttemptCount >= MaxAttempts)
            Status = CustomerOtpChallengeStatus.Locked;
    }

    internal void MarkVerified()
    {
        Status = CustomerOtpChallengeStatus.Verified;
        VerifiedOnUtc = DateTime.UtcNow;
    }
}
```

- [ ] **Step 4: Create remaining entities**

Create the remaining entities with these fields and internal mutation methods:

- `CustomerPortalSession`
  - Fields: `CustomerId`, `SessionTokenHash`, `CreatedOnUtc`, `LastSeenOnUtc`, `ExpiresOnUtc`, `RevokedOnUtc`, `CreatedIp`, `UserAgent`.
  - Methods: `Touch(DateTime nowUtc)`, `Revoke(DateTime nowUtc)`, `IsActive(DateTime nowUtc)`.
- `CustomerSecurityEvent`
  - Fields: `CustomerId`, `DeliveryNoteId`, `EventType`, `Outcome`, `IpAddress`, `UserAgent`, `MetadataJson`, `CreatedOnUtc`.
  - Methods: no state-changing methods after creation.
- `DeliveryNoteAccessToken`
  - Fields: `DeliveryNoteId`, `TokenHash`, `ExpiresOnUtc`, `RevokedOnUtc`, `CreatedOnUtc`, `LastViewedOnUtc`.
  - Methods: `MarkViewed(DateTime nowUtc)`, `Revoke(DateTime nowUtc)`, `CanUse(DateTime nowUtc)`.
- `CustomerDeliveryFeedback`
  - Fields: `CustomerId`, `DeliveryNoteId`, `Rating`, `Message`, `Status`, `CreatedOnUtc`, `ReviewedOnUtc`.
  - Methods: `MarkReviewed(DateTime nowUtc)`.
- `CustomerOrderRequest`
  - Fields: `CustomerId`, `Code`, `Status`, `ExpectedShippingDateUtc`, `ShippingAddress`, `Note`, `AdminNote`, `CreatedOnUtc`, `ReviewedOnUtc`, `ReviewedByUserId`, `ConvertedOrderId`.
  - Private field collection: `_items`.
  - Methods: `AddItem(...)`, `Approve(...)`, `Reject(...)`, `Cancel(...)`, `MarkConverted(Guid orderId, DateTime nowUtc)`.
- `CustomerOrderRequestItem`
  - Fields: `CustomerOrderRequestId`, `ProductId`, `ProductName`, `Quantity`, `UnitPriceSnapshot`.
  - Methods: no state-changing methods after creation.
- `CustomerReturnRequest`
  - Fields: `CustomerId`, `DeliveryNoteId`, `Status`, `Reason`, `AdminNote`, `CreatedOnUtc`, `ReviewedOnUtc`, `ReviewedByUserId`, `ConvertedCustomerReturnId`.
  - Private field collection: `_items`.
  - Methods: `AddItem(...)`, `Accept(...)`, `Reject(...)`, `Cancel(...)`, `MarkConverted(Guid customerReturnId, DateTime nowUtc)`.
- `CustomerReturnRequestItem`
  - Fields: `CustomerReturnRequestId`, `DeliveryNoteItemId`, `ProductId`, `ProductName`, `RequestedQuantity`, `Reason`.
  - Methods: no state-changing methods after creation.
- `CustomerPaymentIntent`
  - Fields: `CustomerId`, `CustomerDebtId`, `Amount`, `Provider`, `ProviderIntentId`, `Status`, `FailureReason`, `CreatedOnUtc`, `CompletedOnUtc`, `ReconciledOnUtc`, `ReconciledByUserId`, `CustomerPaymentId`.
  - Methods: `MarkProcessing(...)`, `MarkSucceededPendingReconciliation(...)`, `MarkFailed(...)`, `Cancel(...)`, `MarkReconciled(...)`.

Every entity in this step must be a `sealed record`, inherit `AppAggregateEntity`, use `internal` constructors, keep DateTime database properties with `Utc` suffixes, and keep mutation methods `internal`.

- [ ] **Step 5: Create domain DTOs and manager interfaces**

Create DTOs in `CustomerPortalDtos.cs` for:

- `CustomerPortalAccountDto`
- `RequestCustomerOtpDto`
- `VerifyCustomerOtpDto`
- `CreateCustomerPortalSessionDto`
- `CreateCustomerOrderRequestDto`
- `CreateCustomerReturnRequestDto`
- `CreateCustomerPaymentIntentDto`
- `RecordCustomerSecurityEventDto`

Each input DTO must include a `Verify()` method that throws when data is invalid.

- [ ] **Step 6: Build**

Run:

```powershell
rtk dotnet build NamEcommerce\NamEcommerce.sln
```

Expected: build succeeds.

---

## Task 3: Add EF Mappings

**Files:**
- Create: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/CustomerPortal/*.cs`

- [ ] **Step 1: Map account**

Create `CustomerPortalAccountMapping.cs`:

```csharp
using NamEcommerce.Domain.Entities.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class CustomerPortalAccountMapping : IEntityTypeConfiguration<CustomerPortalAccount>
{
    public void Configure(EntityTypeBuilder<CustomerPortalAccount> builder)
    {
        builder.ToTable(nameof(CustomerPortalAccount), DbScheme);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerId).IsRequired();
        builder.HasIndex(x => x.CustomerId).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired(false);
        builder.Property(x => x.PasswordSalt).HasMaxLength(200).IsRequired(false);
        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(CustomerPortalAccountStatus.Active)
            .IsRequired();
        builder.Property(x => x.PasswordSetOnUtc).IsRequired(false);
        builder.Property(x => x.LastLoginOnUtc).IsRequired(false);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired(false);
    }
}
```

- [ ] **Step 2: Map token and session security tables**

Create mappings for:

- `DeliveryNoteAccessToken`
  - Unique index on `TokenHash`.
  - Index on `DeliveryNoteId`.
- `CustomerPortalSession`
  - Unique index on `SessionTokenHash`.
  - Index on `CustomerId`.
- `CustomerOtpChallenge`
  - Index on `CustomerId`, `DeliveryNoteId`, `CreatedOnUtc`.
  - `OtpHash` max length 200.
- `CustomerSecurityEvent`
  - Index on `CustomerId`, `CreatedOnUtc`.
  - `MetadataJson` max length 4000.

- [ ] **Step 3: Map request and payment tables**

Create mappings for:

- `CustomerOrderRequest` with one-to-many items.
- `CustomerReturnRequest` with one-to-many items.
- `CustomerDeliveryFeedback`.
- `CustomerPaymentIntent`.

Use `PropertyAccessMode.Field` for item collections, following existing delivery note mapping style.

- [ ] **Step 4: Build**

Run:

```powershell
rtk dotnet build NamEcommerce\NamEcommerce.sln
```

Expected: build succeeds. Do not create migrations.

---

## Task 4: Implement Domain Managers

**Files:**
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Services/CustomerPortal/CustomerPortalSecurityManager.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Services/CustomerPortal/CustomerPortalManager.cs`
- Create: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Extensions/CustomerPortalExtensions.cs`

- [ ] **Step 1: Implement security manager responsibilities**

`CustomerPortalSecurityManager` owns:

- Get or create portal account by `CustomerId`.
- Block and unblock account.
- Store password hash/salt.
- Create OTP challenge after rate-limit checks.
- Mark OTP verification success/failure.
- Create and revoke portal sessions.
- Record security events.

Do not inject HTTP abstractions here.

- [ ] **Step 2: Implement portal manager responsibilities**

`CustomerPortalManager` owns:

- Create order request.
- Approve/reject/cancel order request.
- Mark order request converted to internal `Order`.
- Create return request.
- Accept/reject/cancel return request.
- Mark return request converted to internal `CustomerReturn`.
- Create delivery feedback.
- Create payment intent.
- Mark payment intent mock result.
- Reconcile payment intent after admin approval.

- [ ] **Step 3: Keep domain methods narrow**

Example order request creation method shape:

```csharp
public async Task<CustomerOrderRequestDto> CreateOrderRequestAsync(CreateCustomerOrderRequestDto dto)
{
    ArgumentNullException.ThrowIfNull(dto);
    dto.Verify();

    var account = await GetRequiredActiveAccountAsync(dto.CustomerId).ConfigureAwait(false);
    if (account.IsBlocked())
        throw new InvalidOperationException("Customer portal account is blocked.");

    var request = new CustomerOrderRequest(dto.CustomerId, GenerateOrderRequestCode())
    {
        ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc,
        ShippingAddress = dto.ShippingAddress,
        Note = dto.Note
    };

    foreach (var item in dto.Items)
    {
        request.AddItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPriceSnapshot);
    }

    var inserted = await _orderRequestRepository.InsertAsync(request).ConfigureAwait(false);
    return inserted.ToDto();
}
```

Use the constructor and DTO names declared in this plan.

- [ ] **Step 4: Build**

Run:

```powershell
rtk dotnet build NamEcommerce\NamEcommerce.sln
```

Expected: build succeeds.

---

## Task 5: Implement Application Services And Mock Providers

**Files:**
- Create: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/CustomerPortal/CustomerPortalAppDtos.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Contracts/CustomerPortal/*.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/CustomerPortal/*.cs`
- Create: `NamEcommerce/Application/NamEcommerce.Application.Services/Extensions/CustomerPortalAppExtensions.cs`

- [ ] **Step 1: Add app DTOs**

Create DTOs for:

- Public delivery detail.
- OTP request and verify results.
- Customer session.
- Customer portal dashboard.
- Customer order summary/detail.
- Customer delivery note summary/detail.
- Customer debt summary.
- Customer order request create/result.
- Customer return request create/result.
- Customer payment intent create/result.

App DTO date properties must use `Utc` suffix.

- [ ] **Step 2: Add provider interfaces**

Create:

```csharp
namespace NamEcommerce.Application.Contracts.CustomerPortal;

public interface ICustomerOtpSender
{
    Task<CustomerOtpSendResultAppDto> SendAsync(CustomerOtpSendAppDto dto);
}

public interface ICustomerPaymentProvider
{
    Task<CreateCustomerPaymentProviderIntentResultAppDto> CreateIntentAsync(CreateCustomerPaymentProviderIntentAppDto dto);
    Task<CustomerPaymentProviderResultAppDto> CompleteMockAsync(string providerIntentId, bool success);
}
```

- [ ] **Step 3: Add mock providers**

`MockSmsOtpSender`:

- Logs masked destination and OTP in development-safe logs.
- Returns success unless explicitly disabled through options.

`MockEmailOtpSender`:

- Logs masked email and OTP.
- Used when SMS fails or is disabled.

`MockCustomerPaymentProvider`:

- Creates provider ids like `mock_{Guid:N}`.
- Returns success/failure only through explicit mock completion.

- [ ] **Step 4: Implement auth app service**

`CustomerPortalAuthAppService`:

- Resolve delivery token.
- Check portal account status.
- Apply cooldown and rate limits through manager queries.
- Generate numeric OTP.
- Hash OTP before storage.
- Try SMS first, email fallback.
- Verify OTP and create session.
- Set password using existing `ISecurityService`.
- Password login by phone or email.
- Record all security events.

Return generic failure messages for sensitive auth flows.

- [ ] **Step 5: Implement portal app service**

`CustomerPortalAppService`:

- Get public delivery detail by token.
- Get current customer dashboard.
- Get orders by customer.
- Get delivery notes by customer.
- Confirm delivery when business rules allow.
- Create feedback.
- Create order request.
- Create return request.
- Get debt summary.

All methods requiring auth accept `customerId` from caller context and validate ownership.

- [ ] **Step 6: Implement payment app service**

`CustomerPortalPaymentAppService`:

- Create payment intent for current customer and optional debt id.
- Ensure amount is positive and debt belongs to customer when debt id is provided.
- Call mock provider.
- Mark mock success as `SucceededPendingReconciliation`.
- Do not create or apply `CustomerPayment` until admin reconciliation.

- [ ] **Step 7: Build**

Run:

```powershell
rtk dotnet build NamEcommerce\NamEcommerce.sln
```

Expected: build succeeds.

---

## Task 6: Add Customer API Contracts And Handlers

**Files:**
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Contracts/Models/*.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Contracts/Commands/*.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Contracts/Queries/*.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Framework/Commands/Handlers/*.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Framework/Queries/Handlers/*.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Framework/Services/ICustomerSessionAccessor.cs`

- [ ] **Step 1: Add customer-safe models**

Use models under `Customer.Contracts/Models` only. Do not use AppDto or Domain DTO types.

Create:

- `PublicDeliveryNoteModel`
- `CustomerSessionModel`
- `CustomerOrderListModel`
- `CustomerOrderDetailsModel`
- `CustomerDeliveryNoteListModel`
- `CustomerDeliveryNoteDetailsModel`
- `CustomerDebtSummaryModel`
- `CustomerPaymentIntentModel`
- `CustomerActionResultModel`

Presentation model DateTime properties do not use `Utc` suffix.

- [ ] **Step 2: Add queries**

Create queries:

```csharp
public sealed record GetPublicDeliveryNoteQuery(string Token) : IRequest<PublicDeliveryNoteModel?>;
public sealed record GetCurrentCustomerSessionQuery : IRequest<CustomerSessionModel?>;
public sealed record GetCustomerOrdersQuery : IRequest<CustomerOrderListModel>;
public sealed record GetCustomerOrderDetailsQuery(Guid OrderId) : IRequest<CustomerOrderDetailsModel?>;
public sealed record GetCustomerDeliveryNotesQuery : IRequest<CustomerDeliveryNoteListModel>;
public sealed record GetCustomerDeliveryNoteDetailsQuery(Guid DeliveryNoteId) : IRequest<CustomerDeliveryNoteDetailsModel?>;
public sealed record GetCustomerDebtsQuery : IRequest<CustomerDebtSummaryModel>;
```

- [ ] **Step 3: Add commands**

Create commands:

```csharp
public sealed record RequestCustomerOtpCommand(string DeliveryToken) : IRequest<CustomerActionResultModel>;
public sealed record VerifyCustomerOtpCommand(Guid ChallengeId, string Otp) : IRequest<CustomerSessionModel?>;
public sealed record CustomerPasswordLoginCommand(string Login, string Password) : IRequest<CustomerSessionModel?>;
public sealed record SetCustomerPasswordCommand(string Password) : IRequest<CustomerActionResultModel>;
public sealed record LogoutCustomerCommand : IRequest<CustomerActionResultModel>;
public sealed record CreateCustomerOrderRequestCommand : IRequest<CustomerActionResultModel>;
public sealed record ConfirmCustomerDeliveryNoteCommand(Guid DeliveryNoteId, string? ReceiverName, string? Note) : IRequest<CustomerActionResultModel>;
public sealed record CreateCustomerDeliveryFeedbackCommand(Guid DeliveryNoteId, int? Rating, string? Message) : IRequest<CustomerActionResultModel>;
public sealed record CreateCustomerReturnRequestCommand : IRequest<CustomerActionResultModel>;
public sealed record CreateCustomerPaymentIntentCommand(Guid? CustomerDebtId, decimal Amount) : IRequest<CustomerPaymentIntentModel?>;
public sealed record CompleteMockCustomerPaymentCommand(Guid PaymentIntentId, bool Success) : IRequest<CustomerPaymentIntentModel?>;
```

Add nested item payloads for order and return request commands.

- [ ] **Step 4: Add session accessor**

`ICustomerSessionAccessor` exposes:

- `Guid? CustomerId`
- `Guid? SessionId`
- `bool IsAuthenticated`

Implementation lives in `Customer.Api` because it reads cookies and HTTP context.

- [ ] **Step 5: Add handlers**

Handlers must:

- Call customer application services.
- Map AppDto to customer-safe models.
- Reject unauthenticated requests when required.
- Never accept customer id from request payload.

- [ ] **Step 6: Build**

Run:

```powershell
rtk dotnet build NamEcommerce\NamEcommerce.sln
```

Expected: build succeeds.

---

## Task 7: Implement Customer API Host, DI, Controllers, And Middleware

**Files:**
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Infrastructure/CustomerApiServiceCollectionExtensions.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Infrastructure/CustomerApiApplicationBuilderExtensions.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Infrastructure/CustomerSessionAccessor.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Controllers/PublicDeliveryNotesController.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Controllers/AuthController.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Controllers/MeController.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Controllers/OrdersController.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Controllers/DeliveryNotesController.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Controllers/DebtsController.cs`
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Controllers/PaymentIntentsController.cs`

- [ ] **Step 1: Register services**

`AddCustomerPortalApi` registers:

- Controllers.
- OpenAPI in development.
- ProblemDetails.
- CORS for the React dev origin.
- Cookie policy/session auth.
- Rate limiter policies for OTP and public endpoints.
- EF Core `NamEcommerceEfDbContext`.
- Repository/data reader services.
- Required domain managers.
- Required app services.
- Customer portal app services and mock providers.
- MediatR handlers from `NamEcommerce.Customer.Framework`.

Do not register handlers from `NamEcommerce.Web.Framework`.

- [ ] **Step 2: Configure middleware**

Use order:

```csharp
app.UseHttpsRedirection();
app.UseCors("CustomerClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

Add `UseAuthentication()` before `UseAuthorization()`.

- [ ] **Step 3: Implement controllers**

Controllers use `IMediator` only. Keep them thin.

Example:

```csharp
[ApiController]
[Route("api/public/delivery-notes")]
public sealed class PublicDeliveryNotesController(IMediator mediator) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<IActionResult> Get(string token)
    {
        var model = await mediator.Send(new GetPublicDeliveryNoteQuery(token)).ConfigureAwait(false);
        return model is null ? NotFound() : Ok(model);
    }
}
```

- [ ] **Step 4: Add auth cookie handling**

`AuthController` writes and clears the session-only cookie after successful OTP/password login or logout.

Cookie rules:

- `HttpOnly = true`
- `Secure = true`
- `SameSite = SameSiteMode.Lax` initially
- No persistent expiration

- [ ] **Step 5: Add CSRF decision**

Because this is cookie-authenticated API, add one of:

- Custom header requirement for all state-changing customer endpoints, or
- ASP.NET Core antiforgery configured for SPA.

Recommended first phase: require header `X-Customer-Portal-Request: 1` for all non-GET authenticated endpoints and reject requests without it.

- [ ] **Step 6: Build and verify no admin framework reference**

Run:

```powershell
rtk powershell -Command "rg -n 'NamEcommerce.Web.Framework' NamEcommerce/Presentation/Customer -g '*.cs' -g '*.csproj'"
```

Expected: no results.

Run:

```powershell
rtk dotnet build NamEcommerce\NamEcommerce.sln
```

Expected: build succeeds.

---

## Task 8: Scaffold React Client

**Files:**
- Create: `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Client/*`

- [ ] **Step 1: Create Vite React TypeScript app**

Run from `NamEcommerce/Presentation/Customer`:

```powershell
rtk npm create vite@latest NamEcommerce.Customer.Client -- --template react-ts
```

If network access is blocked, request escalation for dependency download.

- [ ] **Step 2: Install dependencies**

Run:

```powershell
rtk npm install
```

Working directory:

```text
NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Client
```

Use React latest stable available from npm at implementation time. Keep the app client-only and do not add Next.js or React Server Components.

- [ ] **Step 3: Add app structure**

Create:

```text
src/api/client.ts
src/api/types.ts
src/app/App.tsx
src/app/routes.tsx
src/auth/AuthContext.tsx
src/auth/useAuth.ts
src/pages/PublicDeliveryPage.tsx
src/pages/OtpVerifyPage.tsx
src/pages/LoginPage.tsx
src/pages/SetPasswordPage.tsx
src/pages/DashboardPage.tsx
src/pages/OrdersPage.tsx
src/pages/OrderDetailsPage.tsx
src/pages/NewOrderRequestPage.tsx
src/pages/DeliveryNotesPage.tsx
src/pages/DeliveryNoteDetailsPage.tsx
src/pages/DebtsPage.tsx
src/pages/MockPaymentPage.tsx
src/styles/tokens.css
src/styles/app.css
```

- [ ] **Step 4: Add API client**

`src/api/client.ts`:

```ts
const API_BASE_URL = import.meta.env.VITE_CUSTOMER_API_URL ?? "https://localhost:7001";

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Accept", "application/json");

  if (init.method && init.method.toUpperCase() !== "GET") {
    headers.set("Content-Type", "application/json");
    headers.set("X-Customer-Portal-Request", "1");
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers,
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<T>;
}
```

- [ ] **Step 5: Add design tokens**

`src/styles/tokens.css`:

```css
:root {
  --color-primary: #5346e0;
  --color-success: #00a389;
  --color-danger: #ef4444;
  --color-alert: #d97706;
  --color-bg: #f8fafc;
  --color-card: #ffffff;
  --color-border: #e2e8f0;
  --color-text: #1e293b;
  --color-muted: #94a3b8;
  --radius-card: 8px;
  --font-base: Inter, Segoe UI, Roboto, sans-serif;
}
```

- [ ] **Step 6: Verify frontend build**

Run:

```powershell
rtk npm run build
```

Expected: Vite production build succeeds.

---

## Task 9: Implement Public QR And Auth UI

**Files:**
- Modify: React files under `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Client/src`

- [ ] **Step 1: Public delivery page**

`PublicDeliveryPage`:

- Reads `token` from route.
- Calls `GET /api/public/delivery-notes/{token}`.
- Shows delivery note code, order code, status, items, quantities.
- Shows "Verify to manage" action.
- Does not show debts, other orders, or sensitive customer data.

- [ ] **Step 2: OTP request and verify**

Flow:

1. Public page calls `POST /api/auth/otp/request` with delivery token.
2. API returns challenge id and masked destination when safe.
3. React navigates to `/verify`.
4. User enters OTP.
5. React calls `POST /api/auth/otp/verify`.
6. On success, React calls `/api/me` and navigates to `/app`.

- [ ] **Step 3: Password login**

`LoginPage`:

- Phone/email field.
- Password field.
- Calls `POST /api/auth/password/login`.
- Uses session cookie only.
- Does not store tokens in localStorage or sessionStorage.

- [ ] **Step 4: Set password**

`SetPasswordPage`:

- Requires authenticated session.
- Calls `POST /api/auth/password/set`.
- Shows simple success state.

- [ ] **Step 5: Auth context**

`AuthContext`:

- Calls `/api/me` on app load.
- Holds customer session in React memory.
- Clears memory on logout.
- Calls `POST /api/auth/logout`.

- [ ] **Step 6: Browser check**

Start API and React dev servers when implementation reaches this point.

Verify:

- Public QR page renders on mobile width.
- OTP mock flow works.
- Session reload works while browser session remains active.
- Closing browser session requires authentication again.

---

## Task 10: Implement Authenticated Portal Features

**Files:**
- Modify: Customer API controllers, contracts, handlers, app services.
- Modify: React pages under customer client.

- [ ] **Step 1: Dashboard**

Dashboard shows:

- Recent orders.
- Delivery notes waiting for confirmation.
- Debt summary.
- Quick links to new order request and payment.

- [ ] **Step 2: Orders**

Implement:

- `GET /api/orders`
- `GET /api/orders/{id}`
- `POST /api/order-requests`

React:

- Order list.
- Order detail.
- New order request form.

Order request form creates `CustomerOrderRequest`; it does not create internal `Order`.

- [ ] **Step 3: Delivery notes**

Implement:

- `GET /api/delivery-notes`
- `GET /api/delivery-notes/{id}`
- `POST /api/delivery-notes/{id}/confirm`
- `POST /api/delivery-notes/{id}/feedback`

Confirm action must validate:

- Delivery note belongs to session customer.
- Customer is not blocked.
- Delivery note status allows customer confirmation.

- [ ] **Step 4: Return requests**

Implement:

- `POST /api/return-requests`

React:

- Entry point from delivery note detail.
- Item-level requested quantities.
- Reason field.

Return request does not create internal `CustomerReturn`.

- [ ] **Step 5: Debts**

Implement:

- `GET /api/debts`

React:

- Summary cards.
- Debt list by order/delivery note.
- Payment history.
- Pay action opens payment intent flow.

- [ ] **Step 6: Mock payment**

Implement:

- `POST /api/payment-intents`
- `POST /api/payment-intents/{id}/mock-complete`

React:

- Create payment intent from debt page.
- Mock payment screen.
- Success moves intent to pending reconciliation.
- UI explains that payment is waiting for admin reconciliation.

- [ ] **Step 7: Build**

Run:

```powershell
rtk dotnet build NamEcommerce\NamEcommerce.sln
```

Run:

```powershell
rtk npm run build
```

Working directory:

```text
NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Client
```

Expected: backend and frontend builds succeed.

---

## Task 11: Add Internal Admin Review Screens

**Files:**
- Create/modify internal admin MVC files listed in the File Map under "Admin Web Additions".

- [ ] **Step 1: Portal account status on customer detail**

Add:

- Current portal account status.
- Block button.
- Unblock button.
- Last login time.

- [ ] **Step 2: Security events page**

Create customer portal security event list with:

- Customer filter.
- Event type.
- Outcome.
- Created date.
- IP address.
- Short metadata summary.

- [ ] **Step 3: Order request review**

Create:

- List page for pending order requests.
- Detail page.
- Approve/reject commands.
- Convert approved request into real internal `Order`.

Conversion must use existing `IOrderAppService.CreateOrderAsync`.

- [ ] **Step 4: Return request review**

Create:

- List page for pending return requests.
- Detail page.
- Accept/reject commands.
- Convert accepted request into internal `CustomerReturn`.

Conversion must use existing return app service.

- [ ] **Step 5: Payment reconciliation**

Create:

- List page for `SucceededPendingReconciliation`.
- Detail page.
- Reconcile command.

Reconcile command creates or applies `CustomerPayment` through existing `ICustomerDebtAppService` only after admin approval.

- [ ] **Step 6: Build**

Run:

```powershell
rtk dotnet build NamEcommerce\NamEcommerce.sln
```

Expected: build succeeds.

---

## Task 12: Final Verification And Handoff

**Files:**
- Update this plan only if implementation discoveries require it.
- Do not create migration files.
- Do not edit test projects.

- [ ] **Step 1: Full build**

Run:

```powershell
rtk dotnet build NamEcommerce\NamEcommerce.sln
```

Expected: build succeeds.

- [ ] **Step 2: Frontend build**

Run:

```powershell
rtk npm run build
```

Working directory:

```text
NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Client
```

Expected: build succeeds.

- [ ] **Step 3: Check forbidden references**

Run:

```powershell
rtk powershell -Command "rg -n 'NamEcommerce.Web.Framework|NamEcommerce.Web.Contracts' NamEcommerce/Presentation/Customer -g '*.cs' -g '*.csproj'"
```

Expected: no results, except documentation comments if intentionally present outside compiled files.

- [ ] **Step 4: Check no test project edits**

Run:

```powershell
rtk git status --short
```

Expected: no files under `NamEcommerce/Tests` changed.

- [ ] **Step 5: Manual API checks**

Verify:

- Invalid QR token returns 404.
- Valid QR token returns only public delivery fields.
- OTP request respects cooldown.
- Wrong OTP locks after configured attempts.
- Successful OTP creates a session-only cookie.
- Authenticated endpoints reject cross-customer ids.
- Blocked customer cannot request OTP or perform portal actions.
- Mock payment success creates pending reconciliation and does not reduce debt.

- [ ] **Step 6: Manual React checks**

Verify desktop and mobile widths:

- Public delivery page.
- OTP verify page.
- Login page.
- Dashboard.
- Orders list/detail.
- Delivery note detail and confirm action.
- Return request form.
- Debts page.
- Mock payment page.

- [ ] **Step 7: Migration handoff note**

Final implementation response must tell Tuan to create and run EF migration for the new customer portal tables:

- `CustomerPortalAccount`
- `CustomerOtpChallenge`
- `CustomerPortalSession`
- `CustomerSecurityEvent`
- `DeliveryNoteAccessToken`
- `CustomerDeliveryFeedback`
- `CustomerOrderRequest`
- `CustomerOrderRequestItem`
- `CustomerReturnRequest`
- `CustomerReturnRequestItem`
- `CustomerPaymentIntent`

Also mention no migration command was run by Codex.

## Execution Choice

Plan complete. Recommended execution is phase-by-phase with subagent-driven development:

1. Domain and mappings.
2. Application services and mock providers.
3. Customer API contracts/controllers.
4. React public/auth UI.
5. React portal features.
6. Admin review/reconciliation screens.
7. Verification and migration handoff.
