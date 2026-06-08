# Kế hoạch: Dynamic RBAC — Phân quyền theo Permission

## Mục tiêu

Thay thế cơ chế "đăng nhập là được tất cả" bằng hệ thống phân quyền động:
- Permission được định nghĩa dưới dạng constants, lưu vào DB
- Mỗi Role có danh sách Permission được gán sẵn (có thể chỉnh qua UI)
- Controller/action dùng `[Authorize(Policy = "Permission.X")]`
- Admin có UI để xem và thay đổi quyền của từng role
- Cache permission để không query DB mỗi request

---

## Roles hiện tại + Role mới

| Role | Vai trò | Ghi chú |
|---|---|---|
| `Admin` | Quản lý | Có mọi quyền |
| `SalesStaff` | Nhân viên kinh doanh | **MỚI** — tạo đơn bán, quản lý KH |
| `WarehouseManager` | Quản kho | Nhập/xuất/chuyển kho |
| `DeliveryStaff` | Giao hàng | App mobile, xem chuyến giao |
| `Cashier` | Thủ quỹ | Công nợ, thu tiền, kế toán |

---

## Permission Constants (SystemPermissions)

```
Catalog.Categories.View | Catalog.Categories.Manage
Catalog.Products.View | Catalog.Products.Manage
Catalog.Vendors.View | Catalog.Vendors.Manage
Catalog.UnitMeasurements.Manage

Orders.View | Orders.Create | Orders.Edit | Orders.Cancel
Orders.FastSale

DeliveryNotes.View | DeliveryNotes.Manage
DeliveryRuns.View | DeliveryRuns.Manage | DeliveryRuns.ConfirmCashHandover | DeliveryRuns.MobileAccess
DirectShip.View | DirectShip.Manage

PurchaseOrders.View | PurchaseOrders.Create | PurchaseOrders.Cancel
GoodsReceipts.Manage
VendorReturns.Manage

Inventory.View | Inventory.Adjust | Inventory.Transfer | Inventory.ViewCostDetails | Inventory.Preparation
Warehouses.Manage

Customers.View | Customers.Manage
CustomerReturns.Manage

Debts.CustomerDebts.View | Debts.CustomerDebts.RecordPayment
Debts.CustomerRefunds.Manage
Debts.VendorDebts.View | Debts.VendorDebts.RecordPayment

Finance.Expenses.View | Finance.Expenses.Manage
Finance.Accounting
Finance.Reports.Financial | Finance.Reports.DirectShip

Users.ManageRoles
```

---

## Ma trận Role → Permission mặc định

| Permission | Admin | SalesStaff | WarehouseManager | DeliveryStaff | Cashier |
|---|:---:|:---:|:---:|:---:|:---:|
| **Catalog** |||||
| Categories.View | ✅ | ✅ | ✅ | | |
| Categories.Manage | ✅ | | | | |
| Products.View | ✅ | ✅ | ✅ | | |
| Products.Manage | ✅ | | | | |
| Vendors.View | ✅ | | ✅ | | |
| Vendors.Manage | ✅ | | | | |
| UnitMeasurements.Manage | ✅ | | | | |
| **Orders (Bán hàng)** |||||
| Orders.View | ✅ | ✅ | ✅ | | ✅ |
| Orders.Create | ✅ | ✅ | | | |
| Orders.Edit | ✅ | ✅ | | | |
| Orders.Cancel | ✅ | | | | |
| Orders.FastSale | ✅ | ✅ | | | |
| **Giao hàng** |||||
| DeliveryNotes.View | ✅ | ✅ | ✅ | ✅ | ✅ |
| DeliveryNotes.Manage | ✅ | | ✅ | | |
| DeliveryRuns.View | ✅ | | ✅ | ✅ | ✅ |
| DeliveryRuns.Manage | ✅ | | ✅ | | |
| DeliveryRuns.ConfirmCashHandover | ✅ | | | | ✅ |
| DeliveryRuns.MobileAccess | | | | ✅ | |
| DirectShip.View | ✅ | ✅ | ✅ | | |
| DirectShip.Manage | ✅ | | ✅ | | |
| **Nhập hàng** |||||
| PurchaseOrders.View | ✅ | | ✅ | | ✅ |
| PurchaseOrders.Create | ✅ | | ✅ | | |
| PurchaseOrders.Cancel | ✅ | | | | |
| GoodsReceipts.Manage | ✅ | | ✅ | | |
| VendorReturns.Manage | ✅ | | ✅ | | |
| **Kho / Tồn kho** |||||
| Inventory.View | ✅ | ✅ | ✅ | | |
| Inventory.Adjust | ✅ | | ✅ | | |
| Inventory.Transfer | ✅ | | ✅ | | |
| Inventory.ViewCostDetails | ✅ | | ✅ | | |
| Inventory.Preparation | ✅ | | ✅ | | |
| Warehouses.Manage | ✅ | | | | |
| **Khách hàng / Trả hàng** |||||
| Customers.View | ✅ | ✅ | | | ✅ |
| Customers.Manage | ✅ | ✅ | | | |
| CustomerReturns.Manage | ✅ | ✅ | ✅ | | |
| **Công nợ** |||||
| Debts.CustomerDebts.View | ✅ | ✅ | | | ✅ |
| Debts.CustomerDebts.RecordPayment | ✅ | | | | ✅ |
| Debts.CustomerRefunds.Manage | ✅ | | | | ✅ |
| Debts.VendorDebts.View | ✅ | | ✅ | | ✅ |
| Debts.VendorDebts.RecordPayment | ✅ | | | | ✅ |
| **Tài chính** |||||
| Finance.Expenses.View | ✅ | | | | ✅ |
| Finance.Expenses.Manage | ✅ | | | | ✅ |
| Finance.Accounting | ✅ | | | | ✅ |
| Finance.Reports.Financial | ✅ | | | | ✅ |
| Finance.Reports.DirectShip | ✅ | | ✅ | | |
| **Hệ thống** |||||
| Users.ManageRoles | ✅ | | | | |

---

## Kiến trúc kỹ thuật

### Data Flow

```
Request → [Authorize(Policy="Orders.Create")]
        → PermissionAuthorizationHandler
        → IPermissionCacheService.GetPermissionsForRolesAsync(userRoles)
        → IMemoryCache (hit) → return
                      (miss) → query DB (Permission + RolePermission JOIN)
                             → cache 10 phút
        → check if "Orders.Create" ∈ permissions → Succeed / Fail
```

### Caching Strategy

- Key: `permissions:role:{roleName}` → `HashSet<string>` (các permission name)
- TTL: 10 phút
- Invalidate: khi Admin thay đổi RolePermission → xóa cache của role đó
- Admin = mọi permission nên không cần query, luôn Succeed

---

## Các file sẽ tạo/sửa

### Phase 1 — Foundation (Constants + Role)

| File | Thay đổi |
|---|---|
| `Domain.Shared/Dtos/Users/UserDtos.cs` | Thêm `SalesStaff` vào `SystemUserRoleNames` |
| NEW: `Web.Contracts/Security/SystemPermissions.cs` | Static class với toàn bộ permission constants + `GetAll()` |

### Phase 2 — Authorization Handler

| File | Thay đổi |
|---|---|
| NEW: `Web/Authorization/PermissionRequirement.cs` | `IAuthorizationRequirement` chứa permission name |
| NEW: `Web/Authorization/PermissionAuthorizationHandler.cs` | Handler đọc DB/cache, check permission |
| NEW: `Web/Services/Permissions/IPermissionCacheService.cs` | Interface cache |
| NEW: `Web/Services/Permissions/PermissionCacheService.cs` | Implementation với IMemoryCache |
| `Web/Program.cs` | Register handler, cache service, auto-register tất cả permission policies |

### Phase 3 — Seeding

| File | Thay đổi |
|---|---|
| NEW: `Web/Services/Seeding/SystemPermissionsSeeder.cs` | Seed Permission records + RolePermission mặc định theo ma trận |
| `Web/Program.cs` | Register seeder |

### Phase 4 — Apply to Controllers

| Controller | Action cần phân quyền |
|---|---|
| `CategoryController` | View → `Catalog.Categories.View`, Manage → `Catalog.Categories.Manage` |
| `ProductController` | View → `Catalog.Products.View`, Manage → `Catalog.Products.Manage` |
| `VendorController` | View → `Catalog.Vendors.View`, Manage → `Catalog.Vendors.Manage` |
| `UnitMeasurementController` | `Catalog.UnitMeasurements.Manage` |
| `OrderController` | View, Create, Edit → các permission riêng; Cancel → `Orders.Cancel` |
| `FastSaleOrderController` | `Orders.FastSale` |
| `DeliveryNoteController` | View, Manage → tương ứng |
| `DeliveryRunController` | Giữ nguyên style hiện tại, map sang permission mới |
| `DeliveryMobileController` | `DeliveryRuns.MobileAccess` |
| `DirectShipDeliveryController` | View/Manage |
| `PurchaseOrderController` | View, Create, Cancel |
| `GoodsReceiptController` | `GoodsReceipts.Manage` |
| `VendorReturnController` | `VendorReturns.Manage` |
| `InventoryController` | View, ViewCostDetails |
| `StockAdjustmentController` | `Inventory.Adjust` |
| `StockTransferController` | `Inventory.Transfer` |
| `PreparationController` | `Inventory.Preparation` |
| `WarehouseController` | `Warehouses.Manage` |
| `CustomerController` | View, Manage |
| `CustomerReturnController` | `CustomerReturns.Manage` |
| `CustomerDebtController` | View, RecordPayment |
| `CustomerRefundController` | `Debts.CustomerRefunds.Manage` |
| `VendorDebtController` | View, RecordPayment |
| `ExpenseController` | View, Manage |
| `AccountingController` | `Finance.Accounting` |
| `ReportController` | Financial, DirectShip |
| `UserManagementController` | `Users.ManageRoles` (giữ nguyên policy hiện tại) |

### Phase 5 — Menu Integration

| File | Thay đổi |
|---|---|
| `Web/Models/Common/MenuNavigationModel.cs` | Thêm các bool flags cho từng section menu |
| `Web/Components/MenuNavigationComponent.cs` | Dùng `IAuthorizationService.AuthorizeAsync` để build model |
| `Web/Views/Shared/Components/MenuNavigation/Default.cshtml` | Ẩn/hiện menu item dựa vào model |

### Phase 6 — Admin UI Quản lý quyền

| File | Thay đổi |
|---|---|
| NEW: `Application.Contracts/Security/IPermissionAppService.cs` | `GetRolePermissionsAsync`, `UpdateRolePermissionsAsync` |
| NEW: `Application.Contracts/Dtos/Security/PermissionAppDtos.cs` | DTOs |
| NEW: `Application.Services/Security/PermissionAppService.cs` | Implementation |
| NEW: `Web.Contracts/Queries/Models/Security/GetRolePermissionsQuery.cs` | |
| NEW: `Web.Framework/Queries/Handlers/Security/RolePermissionQueryHandler.cs` | |
| NEW: `Web.Contracts/Commands/Models/Security/UpdateRolePermissionsCommand.cs` | |
| NEW: `Web.Framework/Commands/Handlers/Security/UpdateRolePermissionsCommandHandler.cs` | Gọi AppService + InvalidateCache |
| `Web/Controllers/UserManagementController.cs` | Thêm `Permissions()` GET + POST |
| NEW: `Web/Views/UserManagement/Permissions.cshtml` | Giao diện matrix checkbox |
| NEW: `Web/Services/Users/IUserManagementModelFactory.cs` (update) | Thêm `PreparePermissionsModel` |

---

## Thứ tự triển khai & điểm kiểm tra

```
Phase 1: SalesStaff + SystemPermissions constants
  ✓ Build passes
  ✓ SalesStaff có thể được seed

Phase 2: PermissionAuthorizationHandler
  ✓ Build passes
  ✓ Tất cả permission policies được đăng ký
  ✓ Admin có thể truy cập mọi route

Phase 3: Seeder
  ✓ Chạy app: Permission + RolePermission được tạo đúng
  ✓ Đúng số lượng permission records

Phase 4: Apply to controllers
  ✓ WarehouseManager KHÔNG tạo được đơn bán
  ✓ SalesStaff KHÔNG vào được trang kho
  ✓ Cashier KHÔNG tạo được đơn nhập
  ✓ DeliveryStaff chỉ vào được app mobile + xem chuyến

Phase 5: Menu
  ✓ DeliveryStaff: menu chỉ thấy "App Giao hàng"
  ✓ Cashier: không thấy menu Kho, Nhập hàng

Phase 6: Admin UI
  ✓ Admin vào UserManagement → Permissions thấy matrix
  ✓ Tick/bỏ tick → save → cache invalidate → quyền thay đổi ngay
```

---

## Lưu ý quan trọng

1. **Migration**: Bảng `Permission` và `RolePermission` đã có sẵn trong DB (đã có EF mapping). Chỉ cần seed data, **không cần tạo migration mới**.

2. **Admin luôn có full quyền**: Handler kiểm tra role Admin trước, Succeed ngay mà không cần query DB.

3. **Cache invalidation**: Khi update RolePermission, phải xóa cache của role đó. Handler trong Phase 6 có trách nhiệm này.

4. **Backward compat**: Policy cũ `ManageUserRoles`, `ViewDeliveryRuns`... sẽ được thay dần bằng permission policies. Có thể giữ song song trong thời gian chuyển đổi.

5. **SalesStaff.Orders.Cancel**: Ban đầu không có quyền Cancel — chỉ Admin mới hủy đơn. Điều chỉnh qua UI nếu cần.
