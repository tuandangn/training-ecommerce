# Phân tích Navigation trong NamEcommerce.Web

## 📋 Tổng quan
NamEcommerce.Web sử dụng **ASP.NET Core MVC** với hệ thống điều hướng phân cấp dựa trên:
- **View Components** cho menu động
- **Extension Methods** để kiểm tra route hiện tại
- **Bootstrap 5** cho UI navbar
- **Convention-based routing** (Controller/Action/Id)

---

## 🏗️ Kiến trúc Navigation

### 1. **Routing Configuration**

#### File: `Program.cs`
```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

**Chi tiết:**
- Pattern: `{controller}/{action}/{id?}`
- Default: Controller=Home, Action=Index
- Route ID là tùy chọn

**Các Controllers chính:**
- `HomeController` (Trang chủ)
- `OrderController` (Quản lý Đơn hàng)
- `DeliveryNoteController` (Phiếu xuất kho)
- `PreparationController` (Chuẩn bị hàng)
- `PurchaseOrderController` (Đơn nhập)
- `InventoryController` (Tồn kho)
- `CustomerController` (Khách hàng)
- `CategoryController` (Danh mục)
- `ProductController` (Hàng hóa)
- `WarehouseController` (Kho hàng)
- `VendorController` (Nhà cung cấp)
- `ReportController` (Báo cáo)
- `ExpenseController` (Chi phí)
- `UserController` (Người dùng)

---

### 2. **Layout Structure**

#### File: `Views/Shared/_Layout.cshtml`

```html
<header>
  <nav class="navbar navbar-expand-lg navbar-main">
    <!-- Brand -->
    <a class="navbar-brand">NamEcommerce</a>
    
    <!-- Navigation Menu (View Component) -->
    @await Component.InvokeAsync("MenuNavigationComponent")
    
    <!-- User Links (View Component) -->
    @await Component.InvokeAsync("UserHeaderLinksComponent")
  </nav>
</header>

<main>
  @RenderBody()
</main>

<footer>© 2026 NamEcommerce</footer>
```

**Cấu trúc:**
```
┌─────────────────────────────────────┐
│ Header (Navigation Bar)             │
│  - Logo / Brand                     │
│  - Menu Navigation Component        │
│  - User Links Component             │
├─────────────────────────────────────┤
│ Main Content Area                   │
│  @RenderBody()                      │
├─────────────────────────────────────┤
│ Footer                              │
└─────────────────────────────────────┘
```

---

### 3. **Menu Navigation Component**

#### File: `Components/MenuNavigationComponent.cs`
```csharp
public sealed class MenuNavigationComponent : ViewComponent
{
    public IViewComponentResult Invoke() => View();
}
```

#### File: `Views/Shared/Components/MenuNavigationComponent/Default.cshtml`

**Menu Hierarchy:**

```
📌 Trang Chủ
├─ Bán Hàng (Dropdown)
│  ├─ Đơn Hàng → Order/List
│  ├─ Phiếu Xuất Kho → DeliveryNote/List
│  └─ Chuẩn Bị Hàng → Preparation/List
│
├─ Nhập Xuất (Dropdown)
│  ├─ Đơn Nhập → PurchaseOrder/List
│  └─ Tồn Kho → Inventory/StockList
│
├─ Khách Hàng (Dropdown)
│  ├─ Khách Hàng → Customer/List
│  └─ Công Nợ Khách Hàng → CustomerDebt/List
│
├─ Hàng Hóa (Dropdown)
│  ├─ Hàng Hóa → Product/Index
│  ├─ Danh Mục → Category/Index
│  ├─ Kho Hàng → Warehouse/Index
│  └─ Nhà Cung Cấp → Vendor/Index
│
├─ Tài Chính & Báo Cáo (Dropdown)
│  ├─ Báo Cáo Mua Bán → Report/Financial
│  └─ Quản Lý Chi Phí (OPEX) → Expense/Index
│
└─ Cài Đặt (Dropdown)
   ├─ Đơn Vị Tính → UnitMeasurement/Index
   └─ Người Dùng → User/Index
```

---

## 🔍 Active State Detection

### Cơ chế xác định Menu Item Active

#### File: `Contracts/Extensions/WebHelperExtensions.cs`

```csharp
public static class WebHelperExtensions
{
    public static bool IsMatchRouteInfo(string controller, string? action = null)
    {
        var routeData = httpContext.GetRouteData();
        var currentController = routeData.Values["controller"]?.ToString();
        var isControllerMatched = string.Equals(
            currentController, controller, 
            StringComparison.OrdinalIgnoreCase);
        
        if (string.IsNullOrEmpty(action)) 
            return isControllerMatched;
        
        var currentAction = routeData.Values["action"]?.ToString();
        return isControllerMatched && 
               string.Equals(currentAction, action, 
               StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsHasController(this IWebHelper webHelper, 
                                       params string[] controllers)
    {
        foreach (var controller in controllers)
        {
            if (webHelper.IsMatchRouteInfo(controller))
                return true;
        }
        return false;
    }

    public static bool IsLoginPage(this IWebHelper webHelper) 
        => webHelper.IsMatchRouteInfo("User", "Login");

    public static bool IsRegisterPage(this IWebHelper webHelper) 
        => webHelper.IsMatchRouteInfo("User", "Register");
}
```

### Ứng dụng trong View

```html
<!-- Menu Item: Đơn Hàng -->
<a class="dropdown-item @(WebHelper.IsMatchRouteInfo("Order") ? "active" : "")"
   asp-controller="Order" asp-action="List">Đơn Hàng</a>

<!-- Dropdown: Bán Hàng (Active nếu đang trên Order, DeliveryNote, hay Preparation) -->
<a class="nav-link dropdown-toggle 
         @(WebHelper.IsHasController("Order", "Preparation", "DeliveryNote") ? "active" : "")"
   href="#" data-bs-toggle="dropdown">Bán Hàng</a>
```

**Logic:**
- `IsMatchRouteInfo("Order")` → True nếu Controller hiện tại là "Order"
- `IsHasController("Order", "Preparation", "DeliveryNote")` → True nếu controller hiện tại là bất kỳ cái nào trong 3 cái

---

## 🎨 CSS Classes

### Active State Styling

```css
/* Active Link */
.nav-link.active {
    background-color: ...;
    color: ...;
}

/* Dropdown Toggle (tự động khi có active item bên trong) */
.dropdown-toggle.active {
    ...
}
```

### Bootstrap Classes

- `.navbar-nav` - Container cho nav items
- `.nav-item` - Individual nav item
- `.nav-link` - Link styling
- `.dropdown-toggle` - Dropdown button
- `.dropdown-menu` - Dropdown container
- `.dropdown-item` - Item trong dropdown
- `.active` - Active state

---

## 📱 Responsive Behavior

### Mobile Menu

```html
<button class="navbar-toggler" type="button" data-bs-toggle="collapse"
        data-bs-target="#mainNavbar">
    <span class="navbar-toggler-icon"></span>
</button>

<div class="collapse navbar-collapse" id="mainNavbar">
    <!-- Menu items -->
</div>
```

**Behavior:**
- Trên mobile: Menu collapse thành hamburger menu
- Toggle bằng nút hamburger
- Bootstrap tự động xử lý responsive

---

## 🔐 User Links Component

#### File: `Components/UserHeaderLinksComponent.cs`

```csharp
public sealed class UserHeaderLinksComponent : ViewComponent
{
    public IViewComponentResult Invoke() => View();
}
```

**Thường chứa:**
- Thông tin user đang đăng nhập
- Dropdown menu user (Profile, Settings, Logout)
- Login/Register links (nếu chưa đăng nhập)

---

## 📌 Navigation Flow

### Từ Menu Click đến Page Display

```
1. User clicks on Menu Item
   └─> asp-controller="Order" asp-action="List"
   
2. Route Resolution
   └─> /Order/List
   
3. URL Pattern Matching
   └─> {controller=Home}/{action=Index}/{id?}
   └─> Maps to: controller="Order", action="List"
   
4. Controller Action Execution
   └─> OrderController.List()
   
5. View Rendering
   └─> Views/Order/List.cshtml
   
6. Layout with Components
   └─> _Layout.cshtml
   └─> MenuNavigationComponent (Detects current route)
   └─> UserHeaderLinksComponent
   
7. Active State Applied
   └─> WebHelper.IsMatchRouteInfo("Order") = true
   └─> CSS class "active" applied to menu item
```

---

## 🛠️ Service Injection

### File: `Program.cs`

```csharp
// Web Helper Service (dùng để detect active menu)
services.AddScoped<IWebHelper, WebHelper>();

// Information Service (dùng để display app name)
services.AddScoped<IInformationService, InformationService>();

// View Imports
// IWebHelper được inject vào views qua @inject IWebHelper WebHelper
// IInformationService được inject vào views qua @inject IInformationService Information
```

### File: `Views/_ViewImports.cshtml`

```html
@inject IInformationService Information
<!-- IWebHelper được inject tự động khi dùng trong view -->
```

---

## 📊 URL Patterns

### Các Pattern thường thấy

| URL | Controller | Action | ID | Mô tả |
|-----|-----------|--------|-----|-------|
| `/` | Home | Index | - | Trang chủ |
| `/Order/List` | Order | List | - | Danh sách đơn hàng |
| `/Order/Details/123` | Order | Details | 123 | Chi tiết đơn hàng |
| `/DeliveryNote/Create` | DeliveryNote | Create | - | Tạo phiếu xuất |
| `/Product/Index` | Product | Index | - | Danh sách sản phẩm |
| `/Inventory/StockList` | Inventory | StockList | - | Danh sách tồn kho |
| `/Report/Financial` | Report | Financial | - | Báo cáo tài chính |

---

## ⚡ Các tính năng Navigation

### 1. **Breadcrumb Navigation**
- Có thể thêm vào chi tiết pages
- Giúp user hiểu vị trí hiện tại

### 2. **Active State Indicator**
- Tự động highlight menu item hiện tại
- Hỗ trợ multi-level (dropdown active nếu có sub-item active)

### 3. **SEO Friendly URLs**
- Readable URLs: `/Order/List` thay vì `/page.aspx?id=123`
- Convention-based routing dễ maintain

### 4. **Mobile Responsive**
- Hamburger menu trên mobile
- Bootstrap native support

### 5. **User Context Aware**
- Có thể hide/show menu items dựa trên user permissions
- User info header component

---

## 🔄 Navigation Best Practices trong Codebase

### 1. **View: Creating Navigation Links**

```html
<!-- Using asp-controller, asp-action, asp-route-* -->
<a asp-controller="Order" asp-action="Details" asp-route-id="@order.Id">
    @order.Code
</a>

<!-- Với parameters -->
<a asp-controller="Inventory" asp-action="List" 
   asp-route-warehouseId="@warehouse.Id"
   asp-route-keywords="@searchKeywords">
    Xem kho
</a>
```

### 2. **Controller: Redirect After Action**

```csharp
public class OrderController : BaseAuthorizedController
{
    [HttpPost]
    public IActionResult Create(CreateOrderModel model)
    {
        // Process...
        return RedirectToAction("List");
    }
    
    public IActionResult BackToHome() 
        => RedirectToAction("Index", "Home");
}
```

### 3. **View: Detecting Current Location**

```html
<!-- Simple check -->
@if (WebHelper.IsMatchRouteInfo("Order", "List"))
{
    <span>Bạn đang xem danh sách đơn hàng</span>
}

<!-- Multiple controllers check -->
@if (WebHelper.IsHasController("Order", "DeliveryNote", "Preparation"))
{
    <span>Bạn đang ở module Bán hàng</span>
}
```

---

## 📝 Common Navigation Issues & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| Menu item không active | Route không match | Check controller name case sensitivity |
| Dropdown không expand | Missing data-bs-toggle | Verify Bootstrap JS loaded |
| Links không work | Wrong controller/action name | Verify controller exists |
| Mobile menu không collapse | Missing Bootstrap JS | Include bootstrap.bundle.js |
| Active state không update | Browser cache | Hard refresh (Ctrl+Shift+R) |

---

## 📂 File Structure Liên quan Navigation

```
Presentation/NamEcommerce.Web/
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml ..................... Master layout
│   │   ├── _ViewImports.cshtml ............... Global usings
│   │   └── Components/
│   │       ├── MenuNavigationComponent/
│   │       │   ├── MenuNavigationComponent.cs
│   │       │   └── Default.cshtml ........... Menu HTML
│   │       └── UserHeaderLinksComponent/
│   │           └── Default.cshtml ........... User menu
│   └── [Controller]/
│       └── [Action].cshtml ................... Page views
│
├── Controllers/
│   ├── BaseController.cs ..................... Base class
│   ├── BaseAuthorizedController.cs .......... Authorized base
│   └── [Feature]Controller.cs ............... Controllers
│
├── Components/
│   ├── MenuNavigationComponent.cs
│   └── UserHeaderLinksComponent.cs
│
└── Program.cs ............................. DI & routing

Presentation/NamEcommerce.Web.Contracts/
└── Extensions/
    └── WebHelperExtensions.cs ............. Navigation helpers

Presentation/NamEcommerce.Web.Framework/
└── Services/
    └── WebHelper.cs ..................... Route detection
```

---

## 🎯 Key Takeaways

1. **Convention-based Routing**: Sử dụng đặc tập convention của ASP.NET Core
2. **View Components**: MenuNavigationComponent là View Component (không phải Partial)
3. **Active Detection**: Extension methods kiểm tra route hiện tại
4. **Bootstrap 5**: Responsive menu tích hợp Bootstrap
5. **Dependency Injection**: IWebHelper được inject vào views
6. **Clean URLs**: RESTful pattern URLs dễ nhớ và SEO-friendly

---

## 🔗 Navigation Improvement Ideas

### 1. **Breadcrumb Component**
```html
Home > Bán Hàng > Đơn Hàng > Chi tiết
```

### 2. **Search Navigation**
```html
<!-- Quick search bar -->
<input type="search" placeholder="Tìm kiếm...">
```

### 3. **Sidebar Navigation** (thay thế dropdown)
```html
<!-- Collapsible sidebar -->
<aside class="sidebar">
    <nav><!-- Menu items --></nav>
</aside>
```

### 4. **Menu Customization**
```csharp
// Dựa trên user permissions
@if (User.HasPermission("view_orders"))
{
    <!-- Show Order menu -->
}
```

### 5. **Analytics Integration**
```javascript
// Track menu clicks
$('.nav-link').on('click', function() {
    analytics.track('menu_click', {
        menu: $(this).text()
    });
});
```

---

## 📚 Related Files & Classes

| File | Mục đích |
|------|---------|
| `Program.cs` | Cấu hình routing & DI |
| `_Layout.cshtml` | Master layout |
| `MenuNavigationComponent.cs/Default.cshtml` | Main menu |
| `UserHeaderLinksComponent.cs` | User menu |
| `WebHelper.cs` | Route detection |
| `WebHelperExtensions.cs` | Helper methods |
| `BaseController.cs` | Base class cho controllers |
| `_ViewImports.cshtml` | Global view imports |

