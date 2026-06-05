# Retail Walk-In Customer Plan

## Goal

Add one protected system customer named `Khách bán lẻ` for walk-in retail sales, so fast sale orders always keep the existing accounting requirement that every order, delivery note, debt, and payment belongs to a customer.

## Assumptions

- `Khách bán lẻ` is the default for fast sale and immediate payment flows.
- This phase does not allow unpaid retail debt under `Khách bán lẻ`.
- Operators can still override the default customer when the buyer is a known customer.

## Pattern

- Add `CustomerKind` with `Standard = 10` and `RetailWalkIn = 20`.
- Add `Customer.IsSystem` so protected records cannot be deleted by normal customer maintenance.
- Add a domain manager method `GetOrCreateRetailWalkInCustomerAsync()`.
- Fast Sale model preparation calls that method and exposes `DefaultCustomerId`.
- Fast Sale UI selects `DefaultCustomerId` by default while still allowing the operator to choose another customer.
- Customer deletion rejects system customers before checking order usage.

## Data

- New columns on `tbl.Customer`:
  - `CustomerKind int not null default 10`
  - `IsSystem bit not null default 0`
- Filtered unique index:
  - One active `RetailWalkIn` system customer at most.

## Verification

- Build web project:
  - `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj`
- Tests are skipped for now per current project direction.
