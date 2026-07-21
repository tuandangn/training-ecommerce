# Shipper Reconciliation And Delivery Debt Implementation

## Todo
- [x] Add `PendingConfirmation` status and display names.
- [x] Add domain method for shipper-reported completion without `DeliveryNoteDelivered`.
- [x] Route mobile completion to pending confirmation.
- [x] Allow admin completion to finalize pending confirmation.
- [x] Verify by code review/build only if needed.

## Verification Notes
- Per current request, do not write tests and do not create migrations.
- Ran `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj`; build succeeded with existing warnings only.
