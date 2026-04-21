# Kế hoạch Globalization & Localization — NamEcommerce

> Ngày lập: 2026-04-21  
> Hệ thống: VLXD Tuấn Khôi — ASP.NET Core MVC (.NET 10), Clean Architecture + DDD

---

## 1. Hiện trạng (As-Is)

### ✅ Đã có

| Thành phần | Trạng thái |
|---|---|
| `services.AddLocalization()` trong `Program.cs` | Đã đăng ký |
| `CultureConfig` (DefaultCulture, SupportedCultures, FlatpickrLocale/DateFormat) | Đã có, bind từ `appsettings.json` |
| `UseRequestLocalization` middleware | Đã wire, culture mặc định `vi-VN` |
| `DecimalModelBinder` | Parse decimal kiểu VN (dấu `.` nghìn, `,` thập phân) |
| `DecimalFormatHelper` | Format hiển thị tiền / số lượng theo chuẩn VN |
| `DateTimeHelper.ToUniversalTime()` / `.ToLocalTime()` | Chuyển đổi UTC ↔ giờ địa phương |
| HTML `lang="vi"` trong `_Layout.cshtml` | Đã có |
| Flatpickr locale `vn` | Đã config |

### ❌ Chưa có / Cần làm

| Thành phần | Vấn đề |
|---|---|
| Resource files (`.resx`) | Không tồn tại — chưa có cơ sở để localize |
| `IStringLocalizer<T>` | Chưa được inject vào bất kỳ View / Validator nào |
| UI strings trong Views | Hardcoded tiếng Việt trực tiếp trong `.cshtml` |
| FluentValidation messages | Hardcoded tiếng Việt trong từng Validator |
| Enum display strings | Hardcoded trong Extension methods (`OrderStatusExtensions.cs`, ...) |
| AppService error messages | Hardcoded tiếng Việt trong từng AppService |
| Timezone configuration | Chưa tường minh (cần đảm bảo `SE Asia Standard Time`) |
| `AddLocalizationOptions` với `ResourcesPath` | Chưa set, dẫn đến resource lookup sai đường dẫn |

---

## 2. Mục tiêu

Hệ thống hiện tại phục vụ một cửa hàng, **ngôn ngữ duy nhất là tiếng Việt**. Vì vậy mục tiêu không phải đa ngôn ngữ mà là:

1. **Globalization**: Đảm bảo format số, ngày, tiền tệ luôn đúng chuẩn `vi-VN` — nhất quán từ server render đến client-side.
2. **Localization Infrastructure**: Tách strings ra resource files → dễ bảo trì, tránh lặp, và **sẵn sàng mở rộng** thêm ngôn ngữ sau này nếu cần.
3. **Standardize error messages**: Tập trung error/validation messages vào một nơi — không còn hardcoded rải rác.

---

## 3. Kiến trúc tổng thể

```
┌─────────────────────────────────────────────────────────────────┐
│  NamEcommerce.Web (Presentation)                                │
│                                                                  │
│  Resources/                                                      │
│  ├── SharedResource.vi-VN.resx      ← Buttons, labels chung    │
│  ├── ValidationResource.vi-VN.resx  ← Fluent validation msgs   │
│  └── {Module}/                                                   │
│      ├── CatalogResource.vi-VN.resx                            │
│      ├── OrderResource.vi-VN.resx                              │
│      └── ...                                                     │
│                                                                  │
│  Services/                                                       │
│  └── Localization/                                              │
│      └── ILocalizationService (optional wrapper)               │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  NamEcommerce.Application.Contracts                             │
│                                                                  │
│  Errors/                                                         │
│  ├── CatalogErrors.cs               ← Error message constants  │
│  ├── OrderErrors.cs                                             │
│  └── ...                                                         │
└─────────────────────────────────────────────────────────────────┘
```

**Nguyên tắc phân tầng:**
- **Domain Layer**: Exception messages là kỹ thuật (tiếng Anh) — không localize ở đây.
- **Application Layer**: Error messages là constants (`string`) — tập trung trong class `*Errors`.
- **Presentation Layer**: UI strings, validation messages → dùng `IStringLocalizer<T>` với `.resx`.

---

## 4. Phase 1 — Globalization: Chuẩn hóa Format

### 4.1 CultureInfo vi-VN

Cấu hình `vi-VN` sẵn có. Cần đảm bảo:

```json
// appsettings.json
"CultureConfig": {
  "DefaultCulture": "vi-VN",
  "SupportedCultures": [ "vi-VN" ],
  "FlatpickrLocale": "vn",
  "FlatpickrDateFormat": "d/m/Y",
  "FlatpickrDateTimeFormat": "d/m/Y H:i"
}
```

`CultureInfo("vi-VN")` đặc điểm:
- Số thập phân: dấu `,` (1.234,56)
- Phân cách nghìn: dấu `.` (1.234.567)
- Ký hiệu tiền: `₫` / `đ`
- Định dạng ngày: `dd/MM/yyyy`

> `DecimalFormatHelper` đang implement đúng chuẩn này — giữ nguyên.

### 4.2 Timezone — SE Asia Standard Time (UTC+7)

Cần tường minh timezone trong `Program.cs`:

```csharp
// Thêm vào ConfigureServices hoặc tạo AppTimeZone helper
// trong NamEcommerce.Domain.Shared hoặc Web.Contracts
public static class AppTimeZone
{
    // Windows: "SE Asia Standard Time"
    // Linux/Docker: "Asia/Ho_Chi_Minh"
    public static readonly TimeZoneInfo Vietnam =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows()
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh");
}
```

Cập nhật `DateTimeHelper` để sử dụng `AppTimeZone.Vietnam` thay vì `TimeZoneInfo.Local`.

### 4.3 Date Format — Server-side Rendering

Khi render ngày trong View hiện tại dùng kiểu:
```csharp
@item.CreatedOn.ToString("dd/MM/yyyy")
```

Nên chuẩn hóa thành extension method để dùng thống nhất:

```csharp
// NamEcommerce.Web/Extensions/DateTimeExtensions.cs
public static class DateTimeExtensions
{
    public static string DisplayDate(this DateTime dt)
        => dt.ToString("dd/MM/yyyy");

    public static string DisplayDateTime(this DateTime dt)
        => dt.ToString("dd/MM/yyyy HH:mm");

    public static string DisplayDate(this DateTime? dt)
        => dt.HasValue ? dt.Value.DisplayDate() : string.Empty;
}
```

---

## 5. Phase 2 — Infrastructure Localization: Resource Files

### 5.1 Cấu trúc Resource Files

```
NamEcommerce.Web/
└── Resources/
    ├── SharedResource.cs                   ← Marker class (empty)
    ├── SharedResource.vi-VN.resx           ← Common strings
    ├── ValidationResource.cs               ← Marker class
    ├── ValidationResource.vi-VN.resx       ← Fluent validation messages
    └── Modules/
        ├── CatalogResource.cs
        ├── CatalogResource.vi-VN.resx
        ├── OrderResource.cs
        ├── OrderResource.vi-VN.resx
        ├── InventoryResource.cs
        ├── InventoryResource.vi-VN.resx
        ├── CustomerResource.cs
        ├── CustomerResource.vi-VN.resx
        ├── FinanceResource.cs
        └── FinanceResource.vi-VN.resx
```

**Marker class pattern** (bắt buộc để .NET resolve đúng resource):

```csharp
// NamEcommerce.Web/Resources/SharedResource.cs
namespace NamEcommerce.Web.Resources;
public sealed class SharedResource { }

// NamEcommerce.Web/Resources/Modules/CatalogResource.cs
namespace NamEcommerce.Web.Resources.Modules;
public sealed class CatalogResource { }
```

### 5.2 Cập nhật Program.cs

```csharp
// Thay:
services.AddLocalization();

// Thành:
services.AddLocalization(options =>
    options.ResourcesPath = "Resources");
```

### 5.3 Nội dung SharedResource.vi-VN.resx

| Key | Value |
|---|---|
| `Btn.Add` | Thêm mới |
| `Btn.Edit` | Chỉnh sửa |
| `Btn.Delete` | Xóa |
| `Btn.Save` | Lưu |
| `Btn.Cancel` | Hủy |
| `Btn.Search` | Tìm kiếm |
| `Btn.ClearFilter` | Xóa lọc |
| `Btn.Export` | Xuất file |
| `Btn.Print` | In |
| `Label.All` | Tất cả |
| `Label.None` | Không có |
| `Label.Status` | Trạng thái |
| `Label.CreatedDate` | Ngày tạo |
| `Label.Note` | Ghi chú |
| `Label.Action` | Thao tác |
| `Msg.DeleteConfirm` | Bạn muốn xóa {0}? |
| `Msg.SaveSuccess` | Lưu thành công |
| `Msg.DeleteSuccess` | Đã xóa thành công |
| `Msg.SaveFailed` | Lưu thất bại |
| `Empty.Default` | Chưa có dữ liệu |

### 5.4 Nội dung ValidationResource.vi-VN.resx

| Key | Value |
|---|---|
| `Required` | {PropertyName} không được để trống |
| `MaxLength` | {PropertyName} không được vượt quá {MaxLength} ký tự |
| `MinLength` | {PropertyName} phải có ít nhất {MinLength} ký tự |
| `GreaterThan` | {PropertyName} phải lớn hơn {ComparisonValue} |
| `GreaterThanOrEqual` | {PropertyName} phải lớn hơn hoặc bằng {ComparisonValue} |
| `LessThan` | {PropertyName} phải nhỏ hơn {ComparisonValue} |
| `InvalidFormat` | {PropertyName} có định dạng không hợp lệ |
| `MustBeSelected` | Vui lòng chọn {PropertyName} |
| `PhoneInvalid` | Số điện thoại không hợp lệ |
| `EmailInvalid` | Email không hợp lệ |

---

## 6. Phase 3 — Localization Validators (FluentValidation)

### 6.1 Cách inject IStringLocalizer vào Validator

```csharp
// NamEcommerce.Web/Validators/Catalog/CreateCategoryValidator.cs
public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryModel>
{
    public CreateCategoryValidator(IStringLocalizer<ValidationResource> V,
                                   IStringLocalizer<CatalogResource> L)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(V["Required", "Tên danh mục"])
            .MaximumLength(200).WithMessage(V["MaxLength", "Tên danh mục", 200]);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage(V["GreaterThanOrEqual", "Thứ tự", 0]);
    }
}
```

**Lưu ý**: FluentValidation tự động inject qua DI khi dùng `services.AddValidatorsFromAssembly(...)` — constructor injection hoạt động tự nhiên.

### 6.2 Đăng ký Localizer trong Program.cs

Không cần thay đổi — `IStringLocalizer<T>` đã được register tự động khi gọi `services.AddLocalization()`.

---

## 7. Phase 4 — Localization Views

### 7.1 Inject vào _ViewImports.cshtml

```cshtml
@* _ViewImports.cshtml *@
@using Microsoft.Extensions.Localization
@using NamEcommerce.Web.Resources
@using NamEcommerce.Web.Resources.Modules

@inject IStringLocalizer<SharedResource> L
```

### 7.2 Sử dụng trong View

```cshtml
@* Trước (hardcoded): *@
<a asp-action="Create" class="btn btn-primary">Thêm mới</a>
<th>Tên danh mục</th>

@* Sau (localized): *@
<a asp-action="Create" class="btn btn-primary">@L["Btn.Add"]</a>
<th>@CatalogL["Category.Name"]</th>
```

**Thực tế**: Vì hệ thống chỉ có `vi-VN`, strings trong `.resx` sẽ luôn được trả về. Nếu sau này thêm `en-US`, chỉ cần thêm file `SharedResource.en-US.resx`.

### 7.3 Migrate từng bước

Không cần migrate tất cả Views cùng lúc. Ưu tiên theo thứ tự:
1. Validator messages (ảnh hưởng nhiều nhất đến UX)
2. Common shared strings (Thêm mới, Lưu, Xóa, ...)
3. Module-specific labels theo từng sprint

---

## 8. Phase 5 — Application Layer Error Messages

### 8.1 Tạo Error Constants

```csharp
// NamEcommerce.Application.Contracts/Errors/CatalogErrors.cs
namespace NamEcommerce.Application.Contracts.Errors;

public static class CatalogErrors
{
    public static class Category
    {
        public const string NameAlreadyExists = "Tên danh mục đã tồn tại";
        public const string NotFound = "Danh mục không tồn tại";
        public const string HasChildren = "Không thể xóa danh mục đang có danh mục con";
    }

    public static class Product
    {
        public const string SkuAlreadyExists = "Mã SKU đã tồn tại";
        public const string NotFound = "Sản phẩm không tồn tại";
    }
}

// NamEcommerce.Application.Contracts/Errors/OrderErrors.cs
public static class OrderErrors
{
    public const string NotFound = "Đơn hàng không tồn tại";
    public const string CannotEdit = "Không thể chỉnh sửa đơn hàng ở trạng thái này";
    public const string InsufficientStock = "Hàng tồn kho không đủ";
}
```

### 8.2 AppService sử dụng Error Constants

```csharp
// Trước:
return new CreateCategoryResultAppDto
{
    Success = false,
    ErrorMessage = "Tên danh mục đã tồn tại"
};

// Sau:
return new CreateCategoryResultAppDto
{
    Success = false,
    ErrorMessage = CatalogErrors.Category.NameAlreadyExists
};
```

---

## 9. Phase 6 — Enum Display Names

### 9.1 Hiện trạng

```csharp
// NamEcommerce.Web/Extensions/OrderStatusExtensions.cs
public static string ToDisplayName(this OrderStatus status) => status switch
{
    OrderStatus.Pending => "Chờ xử lý",
    OrderStatus.Processing => "Đang xử lý",
    // ...
};
```

### 9.2 Đề xuất — Resource-based Enum Display

```csharp
// NamEcommerce.Web/Resources/Modules/OrderResource.vi-VN.resx
// Key: OrderStatus.Pending   Value: Chờ xử lý
// Key: OrderStatus.Processing Value: Đang xử lý

// Extension method mới:
public static string ToDisplayName(this OrderStatus status,
    IStringLocalizer<OrderResource> localizer)
    => localizer[$"OrderStatus.{status}"];
```

**Hoặc giữ nguyên extension method** (đơn giản hơn, phù hợp khi chỉ 1 ngôn ngữ) và chỉ move strings sang constants:

```csharp
// NamEcommerce.Web/Constants/OrderStatusDisplayNames.cs
public static class OrderStatusDisplayNames
{
    public const string Pending = "Chờ xử lý";
    public const string Processing = "Đang xử lý";
    public const string Completed = "Hoàn thành";
    public const string Cancelled = "Đã hủy";
}
```

---

## 10. Roadmap triển khai

### Sprint 1 — Foundation (Ưu tiên cao nhất)

- [ ] Thêm `ResourcesPath = "Resources"` vào `AddLocalization` trong `Program.cs`
- [ ] Tạo cấu trúc thư mục `Resources/` trong `NamEcommerce.Web`
- [ ] Tạo Marker classes (`SharedResource.cs`, `ValidationResource.cs`, module resources)
- [ ] Tạo `SharedResource.vi-VN.resx` với common strings
- [ ] Tạo `ValidationResource.vi-VN.resx` với validation messages
- [ ] Tạo `AppTimeZone` helper, update `DateTimeHelper`
- [ ] Tạo `DateTimeExtensions` chuẩn hóa format ngày

### Sprint 2 — Validators

- [ ] Update tất cả FluentValidation validators trong `NamEcommerce.Web/Validators/`
- [ ] Inject `IStringLocalizer<ValidationResource>` vào constructor
- [ ] Kiểm tra unobtrusive validation vẫn hoạt động đúng (client-side)

### Sprint 3 — Application Errors

- [ ] Tạo `NamEcommerce.Application.Contracts/Errors/` 
- [ ] Tạo `CatalogErrors`, `OrderErrors`, `InventoryErrors`, `CustomerErrors`, `FinanceErrors`, `DebtErrors`
- [ ] Refactor AppServices để dùng Error constants

### Sprint 4 — Views (Common Strings)

- [ ] Update `_ViewImports.cshtml` inject `IStringLocalizer<SharedResource>`
- [ ] Tạo `CatalogResource.vi-VN.resx` và migrate Catalog views
- [ ] Migrate từng module theo mức độ ưu tiên

### Sprint 5 — Enum Display Names

- [ ] Tạo resource entries cho tất cả Enum display names
- [ ] Hoặc tạo Constants classes thống nhất

---

## 11. Quyết định kỹ thuật cần xác nhận

| # | Câu hỏi | Option A | Option B | Đề xuất |
|---|---|---|---|---|
| 1 | Resource file per-module hay tập trung? | Một `SharedResource.resx` duy nhất | Resource riêng từng module | **B** — dễ maintain khi scale |
| 2 | Enum display: resource hay constants? | `IStringLocalizer` + resx | Static constants class | **B** — đơn giản hơn, phù hợp 1 ngôn ngữ |
| 3 | AppService error messages: constants hay resx? | `*Errors.cs` constants | resx | **A** — không cần inject IStringLocalizer vào Application layer |
| 4 | View migration: toàn bộ hay từng bước? | Migrate hết trong 1 sprint | Migrate từng module | **B** — ít rủi ro hơn |
| 5 | Timezone: `AppTimeZone` static hay config? | Static `SE Asia Standard Time` | Config trong `appsettings.json` | **B** — linh hoạt cho môi trường dev/prod khác nhau |

---

## 12. Kiểm tra & Xác nhận

Sau khi triển khai xong mỗi Sprint, thực hiện:

1. **Sprint 1**: Chạy app, kiểm tra format ngày/số hiển thị đúng `vi-VN`
2. **Sprint 2**: Submit form thiếu dữ liệu → validator message hiển thị đúng và vẫn có client-side validation
3. **Sprint 3**: Trigger AppService error → message đúng và nhất quán
4. **Sprint 4**: Kiểm tra tất cả Views render text đúng không bị `[Missing key]`
5. **Regression**: Đảm bảo `DecimalModelBinder` vẫn parse đúng sau thay đổi culture config
