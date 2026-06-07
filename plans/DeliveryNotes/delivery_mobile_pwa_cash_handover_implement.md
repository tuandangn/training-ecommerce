# Delivery Mobile PWA And Cash Handover Implementation

## Current slice status

- [x] Create isolated worktree from `dev-assistant`.
- [x] Add minimal staff roles: `Admin`, `WarehouseManager`, `DeliveryStaff`, `Cashier`.
- [x] Add admin user role assignment UI.
- [x] Add delivery user assignment to delivery notes.
- [x] Require assigned delivery user before normal customer goods leave warehouse.
- [x] Preserve fast sale counter pickup flow (`ShippingAddress = "Tai quay"`).
- [x] Move normal customer stock dispatch from `Confirmed` to `Delivering`.
- [x] Convert missing-driver domain errors to action results for admin and customer confirmation flows.
- [x] Disable delivery action buttons in admin UI when a normal customer note has no assigned delivery user.

## Verification

- [x] `git diff --check`
- [x] `dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj -v:minimal`

## Remaining implementation

- [x] Add `DeliveryRun` domain/app/infrastructure skeleton and EF migration.
- [x] Add delivery run admin UI and paper manifest.
- [x] Add PWA cache acknowledgement and handover gating.
- [x] Add mobile delivery completion metadata and idempotent sync.
- [x] Add cash handover domain/app/UI and cashier confirmation.
- [x] Add PWA offline shell, IndexedDB store, and manual sync.

## Remaining hardening

- [x] Add role/policy enforcement for warehouse manager, delivery staff, and cashier pages/actions.
- [x] Record COD as customer cash payments when cashier confirms delivery run cash handover.
- [x] Improve delivery mobile offline UX with per-note cash collection and clearer pending sync states.
- [ ] Add automated tests once a test project is available in this worktree.
