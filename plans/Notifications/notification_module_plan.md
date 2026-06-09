# Notification Module Plan

## Goal

Xây module thông báo nội bộ realtime để người giám sát theo dõi và hành động ngay khi có sự kiện vận hành quan trọng: khách thao tác trên customer portal, phiếu xuất/chuyến giao cần xử lý, shipper cập nhật giao hàng, hàng nhập kho, và đơn nhập cần theo dõi.

## Assumptions

- Notification này dành cho user nội bộ trong `NamEcommerce.Web`, không phải thông báo gửi SMS/email cho khách.
- Hệ thống cần cả realtime và lịch sử. User đang online nhận ngay, user offline vẫn xem lại được unread/history khi đăng nhập.
- Audience đi theo permission/module. Không broadcast toàn hệ thống.
- `TempDataNotificationService` hiện tại chỉ là toast feedback sau request và không nên bị dùng làm operational notification feed.
- `CustomerPortalNotification` hiện tại là notification riêng của portal. Phase 1 không migrate bảng đó; chỉ bridge các sự kiện portal quan trọng sang notification chung.
- SignalR chỉ nằm ở Web/presentation boundary. Domain và Application không phụ thuộc SignalR.
- Triển khai ưu tiên single-instance trước. Scale-out SignalR bằng Redis/Azure SignalR để sau.

## Success Criteria

- Notification được lưu DB với trạng thái read/unread theo từng user.
- User chỉ thấy notification nếu có permission phù hợp.
- User online nhận notification gần realtime qua SignalR.
- Header/admin layout có chuông thông báo và badge unread.
- Có trang danh sách notification để lọc theo module, trạng thái, severity.
- Notification có link hành động tới màn hình liên quan: customer portal request, phiếu xuất, chuyến giao, phiếu nhập, đơn nhập.
- Các nguồn sự kiện phase 1 tạo notification đúng audience.
- Build solution pass sau khi triển khai.

## Recommended Architecture

Tạo module `Notifications` độc lập trong các layer hiện có:

```text
Domain event / App workflow
    -> MediatR handler
    -> ISystemNotificationAppService
    -> ISystemNotificationManager
    -> SystemNotification + SystemNotificationRead
    -> Web SignalR publisher
    -> browser notification center
```

Lý do chọn hướng này:

- Giữ đúng Clean Architecture: Domain chỉ raise event, Application xử lý nghiệp vụ, Web lo realtime.
- Không trộn operational feed với toast feedback request hiện tại.
- Không kéo customer portal notification thành abstraction chung sai tên.
- Permission audience giúp feed ít nhiễu và sẵn sàng mở rộng.

## Out Of Scope

- Push notification native/mobile OS.
- Email/SMS/Zalo.
- Rule engine cấu hình động cho mọi loại notification.
- Escalation/SLA tự động.
- Multi-instance SignalR scale-out.
- Xóa/migrate `CustomerPortalNotification`.

## Data Model

### `SystemNotification`

Aggregate root mới trong module `Notifications`.

Fields:

- `Id`
- `Type`
- `Severity`
- `Title`
- `Message`
- `RequiredPermission`
- `RelatedEntityType`
- `RelatedEntityId`
- `ActionUrl`
- `CreatedByUserId`
- `CreatedOnUtc`

Rules:

- `Title`, `Type`, `Severity`, `RequiredPermission`, `CreatedOnUtc` required.
- `RequiredPermission` là permission string từ `SystemPermissions`.
- `ActionUrl` phải là URL nội bộ tương đối, ví dụ `/DeliveryRun/Details/{id}`.
- Không tạo notification nếu thiếu audience permission.

### `SystemNotificationRead`

Entity ghi nhận user đã đọc notification.

Fields:

- `Id`
- `NotificationId`
- `UserId`
- `ReadOnUtc`

Rules:

- Unique index: `(NotificationId, UserId)`.
- Mark read idempotent: nếu đã có row thì không tạo thêm.
- Unread count tính bằng `SystemNotification` được phép xem trừ các row read của user.

## Enums

`SystemNotificationType`:

- `CustomerPortalOrderRequestCreated`
- `CustomerPortalReturnRequestCreated`
- `CustomerPortalDeliveryConfirmed`
- `CustomerPortalPaymentPendingReconciliation`
- `DeliveryNoteCreated`
- `DeliveryNoteConfirmed`
- `DeliveryNoteDelivering`
- `DeliveryNoteDelivered`
- `DeliveryRunCreated`
- `DeliveryRunHandedOver`
- `DeliveryRunCashHandoverPending`
- `GoodsReceiptCreated`
- `PurchaseOrderCreated`
- `PurchaseOrderStatusChanged`

`SystemNotificationSeverity`:

- `Info`
- `Success`
- `Warning`
- `Danger`

## Audience Mapping

Initial mapping:

| Event | RequiredPermission |
|---|---|
| Khách tạo yêu cầu đặt hàng | `Orders.View` |
| Khách tạo yêu cầu trả hàng | `CustomerReturns.Manage` |
| Khách xác nhận đã nhận hàng | `DeliveryNotes.View` |
| Thanh toán portal cần đối soát | `Debts.CustomerDebtsView` |
| Phiếu xuất mới | `DeliveryNotes.Manage` |
| Phiếu xuất đã xác nhận cần giao | `DeliveryRuns.Manage` |
| Shipper bắt đầu giao | `DeliveryRuns.View` |
| Shipper giao xong | `DeliveryRuns.View` |
| Chuyến giao mới | `DeliveryRuns.Manage` |
| Chuyến giao bàn giao cho shipper | `DeliveryRuns.View` |
| Chuyến giao có tiền cần xác nhận | `DeliveryRuns.ConfirmCashHandover` |
| Hàng vừa nhập kho | `GoodsReceipts.Manage` |
| Đơn nhập mới/cần xử lý | `PurchaseOrders.View` |

Nếu một event cần nhiều nhóm nhận, phase 1 tạo nhiều notification cùng payload nhưng khác `RequiredPermission`. Không tạo bảng nhiều-nhiều audience ngay để tránh over-engineering.

## Application Contracts

Create in `NamEcommerce.Application.Contracts/Notifications/`:

- `ISystemNotificationAppService`

Create DTOs in `NamEcommerce.Application.Contracts/Dtos/Notifications/`:

- `CreateSystemNotificationAppDto`
- `SystemNotificationAppDto`
- `SystemNotificationListFilterAppDto`
- `SystemNotificationListResultAppDto`
- `SystemNotificationUnreadCountAppDto`
- `MarkSystemNotificationReadResultAppDto`

Core methods:

```csharp
Task<SystemNotificationAppDto> CreateAsync(CreateSystemNotificationAppDto dto);
Task<IPagedDataDto<SystemNotificationAppDto>> GetNotificationsAsync(SystemNotificationListFilterAppDto dto);
Task<int> CountUnreadAsync(Guid userId, IReadOnlyCollection<string> userPermissions);
Task MarkReadAsync(Guid notificationId, Guid userId);
Task MarkAllReadAsync(Guid userId, IReadOnlyCollection<string> userPermissions);
```

The service filters by permission list supplied by Web/current user context.

## Domain Services

Create in `NamEcommerce.Domain.Shared/Services/Notifications/`:

- `ISystemNotificationManager`

Create in `NamEcommerce.Domain.Services/Notifications/`:

- `SystemNotificationManager`

Responsibilities:

- Create notification.
- Query notifications by permission list, type, severity, read state.
- Count unread for a user.
- Mark one/all read idempotently.

Do not inject `IHttpContextAccessor`, SignalR, or Web services into the manager.

## Infrastructure

Create EF mappings:

- `NamEcommerce.Data.SqlServer/Mappings/Notifications/SystemNotificationMap.cs`
- `NamEcommerce.Data.SqlServer/Mappings/Notifications/SystemNotificationReadMap.cs`

Indexes:

- `SystemNotification`: `(RequiredPermission, CreatedOnUtc)`.
- `SystemNotification`: `(Type, CreatedOnUtc)`.
- `SystemNotification`: `RelatedEntityId`.
- `SystemNotificationRead`: unique `(NotificationId, UserId)`.
- `SystemNotificationRead`: `(UserId, ReadOnUtc)`.

Add EF migration in the migrations project during implementation.

## Realtime Web Boundary

Add SignalR to `NamEcommerce.Web`.

Create:

- `Hubs/Notifications/SystemNotificationHub.cs`
- `Services/Notifications/ISystemNotificationRealtimePublisher.cs`
- `Services/Notifications/SignalRSystemNotificationRealtimePublisher.cs`
- `Services/Notifications/IUserPermissionSnapshotService.cs`

Hub behavior:

- Requires authenticated internal user.
- On connected, resolve current user's permissions.
- Join groups named `permission:{permission}`.
- Client receives `systemNotificationCreated`.

Publisher behavior:

- Sends to group `permission:{RequiredPermission}`.
- Payload is a compact DTO for the header panel.
- Publishing failure should not rollback persisted notification. Log and continue.

`Program.cs` additions:

- `services.AddSignalR()`.
- Register notification services.
- `app.MapHub<SystemNotificationHub>("/hubs/system-notifications")`.

## Web UI

### Header notification center

Add to the main authenticated layout:

- Bell icon button.
- Unread badge.
- Dropdown/panel with latest 10 notifications.
- Mark one as read when opened.
- Link to `/SystemNotification/List`.

Use the project design system:

- Modern SaaS/dashboard style.
- White/surface panels, subtle borders, compact rows.
- Severity colors should be restrained: indigo/sky/emerald/red/slate.
- No marketing-style hero or decorative UI.

### Notification list page

Create MVC surface:

- `Controllers/SystemNotificationController.cs`
- `Views/SystemNotification/List.cshtml`
- `Services/Notifications/ISystemNotificationModelFactory.cs`
- `Services/Notifications/SystemNotificationModelFactory.cs`
- Web.Contracts models/queries/commands under `Notifications`.
- Web.Framework handlers under `Notifications`.

Features:

- Filter module/type.
- Filter unread/read/all.
- Filter severity.
- Page through newest first.
- Mark single read.
- Mark all visible/readable read.
- Open notification and redirect to `ActionUrl`.

## Event Producers Phase 1

Add MediatR handlers in Application.Services where domain events already exist.

Candidate handlers:

- Customer portal app workflow after it creates portal notification:
  - order request created
  - return request created
  - delivery received confirmed
  - payment pending reconciliation if available
- Delivery note events:
  - `DeliveryNoteCreated`
  - `DeliveryNoteConfirmed`
  - `DeliveryNoteDelivering`
  - `DeliveryNoteDelivered`
- Delivery run workflow:
  - create notification after run create/handover/cash pending.
  - If no domain events exist for delivery run yet, call app service from `DeliveryRunAppService` after successful manager call.
- Goods receipt:
  - `GoodsReceiptCreated`
- Purchase order:
  - `PurchaseOrderCreated`
  - status changed event if available.

Each producer should set:

- `Type`
- `Severity`
- `Title`
- `Message`
- `RequiredPermission`
- `RelatedEntityType`
- `RelatedEntityId`
- `ActionUrl`

## Error Handling

- DTO validation in AppService returns result where the existing pattern expects `Success = false`.
- Domain DTO `Verify()` throws for invalid notification data.
- SignalR send failures are logged, not thrown to business workflow.
- Missing related entity in handler should return early.
- Mark read on unknown notification returns false/result error at AppService layer.

## Testing Plan

Domain tests:

- Create notification validates required title/permission.
- Query by permission returns only allowed notifications.
- Mark read is idempotent.
- Unread count excludes read notifications.

Application tests:

- Invalid create dto returns failure.
- List filter applies permission/type/severity/read status.
- Mark all read only affects notifications user can see.

Event handler tests:

- Customer portal order request creates `Orders.View` notification.
- Delivery note confirmed creates `DeliveryRuns.Manage` notification.
- Delivery delivered with cash creates `DeliveryRuns.View` or cash handover notification as applicable.
- Goods receipt created creates `GoodsReceipts.Manage` notification.

Web/realtime verification:

- Hub requires authentication.
- Authenticated user joins only permission groups they have.
- Publisher sends compact payload to expected group.

Final verification:

```powershell
dotnet build NamEcommerce.sln
```

## Rollout Notes

- Ship DB-backed notification feed first.
- Then wire SignalR and header UI.
- Then add event producers one module at a time.
- Keep existing `CustomerPortal/Notifications` page until the new feed proves stable.
- If notification volume becomes high, add retention/archive policy later.
