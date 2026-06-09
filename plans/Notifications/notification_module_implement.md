# Notification Module Implementation

## Todo

- [x] Add `Notifications` domain entity, enums, DTOs, manager, and EF mappings.
- [x] Add application contracts/services for create, list, unread count, mark read, mark all read.
- [x] Add Web contracts, MediatR handlers, model factory, controller, and list view.
- [x] Add SignalR hub and realtime publisher grouped by permission.
- [x] Add header notification center UI and client JavaScript.
- [x] Add event producers for customer portal events.
- [x] Add event producers for delivery note and delivery run events.
- [x] Add event producers for goods receipt and purchase order events.
- [x] Add EF migration.
- [x] Add focused tests for manager/app service/event producers.
- [ ] Build solution.

## Implementation Steps

### Task 1: Domain model and persistence

Files:

- Create `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Notifications/SystemNotification.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Notifications/SystemNotificationRead.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/Notifications/SystemNotificationType.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/Notifications/SystemNotificationSeverity.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Notifications/SystemNotificationDtos.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Notifications/ISystemNotificationManager.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Services/Notifications/SystemNotificationManager.cs`
- Create `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/Notifications/SystemNotificationMap.cs`
- Create `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/Notifications/SystemNotificationReadMap.cs`

Steps:

- [x] Implement `SystemNotification` as `sealed record : AppAggregateEntity`.
- [x] Add fields from the plan: type, severity, title, message, required permission, related entity, action url, creator, created time.
- [x] Implement `SystemNotificationRead` with notification id, user id, read time.
- [x] Add `CreateSystemNotificationDto`, `SystemNotificationDto`, list filter DTO, and read result DTO.
- [x] Implement manager create/query/count/mark-read/mark-all-read methods.
- [x] Add EF mapping and indexes.
- [x] Verify with focused domain tests or, if tests are not available yet, `dotnet build NamEcommerce.sln`.

### Task 2: Application service

Files:

- Create `NamEcommerce/Application/NamEcommerce.Application.Contracts/Notifications/ISystemNotificationAppService.cs`
- Create `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Notifications/SystemNotificationAppDtos.cs`
- Create `NamEcommerce/Application/NamEcommerce.Application.Services/Notifications/SystemNotificationAppService.cs`
- Create `NamEcommerce/Application/NamEcommerce.Application.Services/Extensions/SystemNotificationExtensions.cs`
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs` for DI registration.

Steps:

- [x] Add app DTO `Validate()` methods returning `(bool, string?)`.
- [x] Map app DTOs to domain DTOs.
- [x] Return failure result for invalid create/read input.
- [x] Query notifications using the current user's permission list supplied by Web.
- [x] Count unread using user id plus permission list.
- [x] Keep service free of `HttpContext` and SignalR dependencies.
- [x] Verify application tests for create/count/read.

### Task 3: Permission snapshot service

Files:

- Create `NamEcommerce/Presentation/NamEcommerce.Web/Services/Notifications/IUserNotificationPermissionService.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Services/Notifications/UserNotificationPermissionService.cs`
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`.

Steps:

- [x] Resolve current authenticated user id from claims.
- [x] Resolve role claims.
- [x] Treat `Admin` as having all `SystemPermissions.GetAll()`.
- [x] For other roles, use existing `IPermissionCacheService.GetPermissionsForRoleAsync`.
- [x] Return normalized permission strings consistently.
- [x] Use this service in controller, model factory, and SignalR hub.

### Task 4: Web contracts and handlers

Files:

- Create `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/Notifications/SystemNotificationModels.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Queries/Models/Notifications/GetSystemNotificationListQuery.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Queries/Models/Notifications/GetSystemNotificationUnreadCountQuery.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/Notifications/MarkSystemNotificationReadCommand.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/Notifications/MarkAllSystemNotificationsReadCommand.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Queries/Handlers/Notifications/GetSystemNotificationListHandler.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Queries/Handlers/Notifications/GetSystemNotificationUnreadCountHandler.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/Notifications/MarkSystemNotificationReadHandler.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/Notifications/MarkAllSystemNotificationsReadHandler.cs`

Steps:

- [x] Keep controller pattern: controller uses `IMediator` and model factory only.
- [x] Keep query handlers thin: call app service and map to Web models.
- [x] Include action url and related entity metadata in list item model.
- [x] Add `Success/ErrorMessage` result models for mark-read commands.

### Task 5: MVC notification list

Files:

- Create `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/SystemNotificationController.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Services/Notifications/ISystemNotificationModelFactory.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Services/Notifications/SystemNotificationModelFactory.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Views/SystemNotification/List.cshtml`
- Modify shared navigation/menu only if the current layout has a central menu file.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`.

Steps:

- [x] Add authenticated list action.
- [x] Add filters: module/type, unread/read/all, severity.
- [x] Add POST action to mark one read.
- [x] Add POST action to mark all visible/readable read.
- [x] Add open action: mark read then redirect to `ActionUrl`.
- [x] Render newest first with compact operational rows.
- [x] Use restrained dashboard UI following `DESIGN.md`.

### Task 6: SignalR hub and publisher

Files:

- Create `NamEcommerce/Presentation/NamEcommerce.Web/Hubs/Notifications/SystemNotificationHub.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Services/Notifications/ISystemNotificationRealtimePublisher.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Services/Notifications/SignalRSystemNotificationRealtimePublisher.cs`
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`.

Steps:

- [x] Add `services.AddSignalR()`.
- [x] Map hub to `/hubs/system-notifications`.
- [x] Require authenticated user for hub.
- [x] On connect, join `permission:{permission}` groups based on current user permissions.
- [x] Publisher sends `systemNotificationCreated` to `permission:{RequiredPermission}`.
- [x] Log publish failures without failing notification persistence.

### Task 7: Header notification center client

Files:

- Create `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/js/system-notification-center.js`
- Modify authenticated shared layout file that renders the top bar.
- Add CSS to existing app stylesheet or create `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/system-notifications.css` if the project has no suitable stylesheet.

Steps:

- [x] Add bell icon button and unread badge.
- [x] Fetch latest notification list and unread count on page load.
- [x] Connect SignalR client.
- [x] On realtime event, prepend item and increment unread badge.
- [x] Mark as read when the user opens an item.
- [x] Add link to full list page.
- [x] Keep layout stable on desktop and mobile.

### Task 8: Customer portal event producers

Files:

- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/CustomerPortal/CustomerPortalAppService.cs`
- Optionally create `NamEcommerce/Application/NamEcommerce.Application.Services/Notifications/SystemNotificationComposer.cs` if message composition starts duplicating.

Steps:

- [x] After customer order request notification is created, create system notification with `Orders.View`.
- [x] After customer return request notification is created, create system notification with `CustomerReturns.Manage`.
- [x] After customer delivery confirmation notification is created, create system notification with `DeliveryNotes.View`.
- [x] Use action URLs that open the existing CustomerPortal admin detail pages.
- [x] Add tests for each producer.

### Task 9: Delivery note and delivery run event producers

Files:

- Create or modify handlers under `NamEcommerce/Application/NamEcommerce.Application.Services/Events/DeliveryNotes/`.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/DeliveryNotes/DeliveryRunAppService.cs` if delivery run domain events are not available.

Steps:

- [x] `DeliveryNoteCreated` creates `DeliveryNotes.Manage` notification.
- [x] `DeliveryNoteConfirmed` creates `DeliveryRuns.Manage` notification for non-direct customer delivery.
- [x] `DeliveryNoteDelivering` creates `DeliveryRuns.View` notification.
- [x] `DeliveryNoteDelivered` creates `DeliveryRuns.View` notification.
- [x] Delivery run created creates `DeliveryRuns.Manage` notification.
- [x] Delivery run handed over creates `DeliveryRuns.View` notification.
- [x] Delivery run cash handover pending creates `DeliveryRuns.ConfirmCashHandover` notification.
- [x] Add tests for audience and action URLs.

### Task 10: Goods receipt and purchase order event producers

Files:

- Modify or create handler under `NamEcommerce/Application/NamEcommerce.Application.Services/Events/GoodsReceipts/`.
- Modify or create handler under `NamEcommerce/Application/NamEcommerce.Application.Services/Events/PurchaseOrders/`.

Steps:

- [x] `GoodsReceiptCreated` creates `GoodsReceipts.Manage` notification.
- [x] `PurchaseOrderCreated` creates `PurchaseOrders.View` notification.
- [x] Purchase order status changed creates `PurchaseOrders.View` notification when the status requires action.
- [x] Avoid duplicate notifications if quick-create flow creates purchase order and goods receipt in one workflow; only create both if both are useful to supervisors.
- [x] Add tests for each event.

### Task 11: Migration and registration

Files:

- Create EF migration in `NamEcommerce/Migrations/NamEcommerce.Data.SqlServerMigrations`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Security/SystemPermissions.cs` only if a dedicated notification permission is needed.

Steps:

- [x] Register notification manager, app service, model factory, permission service, realtime publisher.
- [x] Add migration for `SystemNotification` and `SystemNotificationRead`.
- [x] Do not add a new permission unless the list page needs its own menu permission. Reuse existing module permissions for audience.
- [x] Verify migration compiles.

### Task 12: Verification

Commands:

```powershell
dotnet build NamEcommerce.sln
```

Manual checks:

- [ ] Login as warehouse manager and open admin layout.
- [ ] Trigger goods receipt creation and verify notification appears for user with `GoodsReceipts.Manage`.
- [ ] Login as user without `GoodsReceipts.Manage` and verify the notification is not visible.
- [ ] Trigger delivery run event and verify SignalR updates badge without refresh.
- [ ] Click notification and verify it marks read then opens the related entity.
- [ ] Mark all read and verify unread badge becomes zero.

## Guardrails

- Do not reuse `TempDataNotificationService` for operational feed.
- Do not rename or delete `CustomerPortalNotification` in phase 1.
- Do not inject SignalR into Domain or Application.Services business managers.
- Do not broadcast all operational notifications to every authenticated user.
- Do not create dynamic notification rule configuration in phase 1.
- Keep changes surgical and module-scoped.
