# NamEcommerce - Tài liệu hướng dẫn hệ thống cho AI

Tài liệu này cung cấp cái nhìn tổng quan về kiến trúc, cấu trúc dự án và cách thức vận hành của hệ thống NamEcommerce.

## 1. Mô hình Kiến trúc (Architecture Model)

Hệ thống được xây dựng theo kiến trúc **Clean Architecture** kết hợp với các nguyên lý của **Domain-Driven Design (DDD)**. Hệ thống được chia thành các lớp (Layers) rõ rệt để đảm bảo tính module và khả năng bảo trì:

- **1. Domain Layer (Lõi):** Chứa các Entity, Value Object, Domain Service và các Business Logic thuần túy nhất. Không phụ thuộc vào bất kỳ layer nào khác.
- **2. Application Layer:** Chứa logic thực thi các Use Case của hệ thống. Chứa các Application Services, Contracts (Interfaces), DTOs và Mapping.
- **3. Infrastructure Layer:** Chứa các implementation cụ thể về persistence (SQL Server qua EF Core, MongoDB) và các dịch vụ bên thứ ba.
- **4. Presentation Layer:** Chứa các điểm đầu vào của người dùng (Web UI, REST API, GraphQL).

## 2. Cấu trúc Dự án (Project Structure)

Thư mục gốc chứa file solution `.sln` và các thư mục layer chính:

- `/Domain`:
    - `NamEcommerce.Domain`: Chứa các thực thể chính (Entities) phân chia theo module (Catalog, Orders, Inventory, Finance...).
    - `NamEcommerce.Domain.Shared`: Chứa các base class (`AppEntity`, `AppAggregateEntity`), Enums và Constants dùng chung.
    - `NamEcommerce.Domain.Services`: Chứa các logic nghiệp vụ phức tạp liên quan đến nhiều thực thể.
- `/Application`:
    - `NamEcommerce.Application.Services`: Implementation của logic nghiệp vụ cho từng module.
    - `NamEcommerce.Application.Contracts`: Định nghĩa các Interface cho service và các DTO (Data Transfer Object).
- `/Infrastructure`:
    - `NamEcommerce.Data.SqlServer`: Cấu hình Entity Framework, Mappings và SQL Server Repositories.
- `/Presentation`:
    - `NamEcommerce.Web`: Giao diện quản trị/người dùng chính (ASP.NET Core MVC). Đây là entry point chính cho hệ thống ERP/Ecommerce.
    - `NamEcommerce.Web.Contracts`: Định nghĩa các Models, Queries, Commands.
    - `NamEcommerce.Web.Framework`: Thực thi các Queries, Commands thông qua handlers của MediatR.
- `/Migrations`: Chứa các file migration của database.
- `/Tests`: Chứa Unit Tests, Integration Tests và E2E Tests (Playwright).

## 3. Các Module Chính (Core Modules)

- **Catalog:** Quản lý sản phẩm, danh mục và đơn vị hàng hóa.
- **Orders:** Xử lý đơn bán hàng, theo dõi thanh toán và trạng thái giao hàng (Order → DeliveryNote → trừ tồn kho + ghi công nợ khách hàng).
- **Inventory & PurchaseOrders:** Quản lý kho (Warehouses), nhập hàng (PurchaseOrder → GoodsReceipt → cộng tồn kho + ghi công nợ NCC), kiểm kê và điều phối hàng tồn kho.
- **Returns:** *(Chưa implement — xem `RETURNS_MODULE_PLAN.md`)* Xử lý trả hàng hai chiều: CustomerReturn (khách trả về) và VendorReturn (cửa hàng trả NCC). Có bước kiểm tra chất lượng (Inspecting) trước khi cập nhật tồn kho và công nợ.
- **Finance:** Quản lý thu chi (Expenses), báo cáo tài chính và dòng tiền (với module tài chính chuyên sâu).
- **Customers & Users:** Quản lý thông tin khách hàng và hệ thống phân quyền nhân viên.

## 4. Cách thức Hoạt động (Operations)

- **Data Access:** Hệ thống sử dụng mẫu thiết kế **Repository Pattern** để trừu tượng hóa việc truy cập dữ liệu. Database chính là SQL Server thông qua EF Core.
- **Business Flow:** Yêu cầu từ người dùng qua Presentation -> Gọi Application Service qua Contract -> Application Service gọi Domain Logic hoặc Repository -> Trả về kết quả qua DTO.
- **Security:** Quản lý phân quyền dựa trên Role và User định danh trong module Security.
- **UI/UX:** Giao diện được thiết kế theo hướng chuyên nghiệp, hỗ trợ đầy đủ thiết bị di động (Responsive). Các chức năng quản lý được tối ưu hóa cho hiệu suất cao.

## 5. Công cụ và Công nghệ (Tech Stack)

- **Backend:** .NET (C#)
- **ORM:** Entity Framework Core
- **Database:** SQL Server (Chính)
- **UI Framework:** ASP.NET Core MVC, Vanilla CSS/JS
- **Testing:** xUnit, Playwright
- **Context:** Hệ thống hiện đang được triển khai cho đơn vị "VLXD Tuấn Khôi".

## 6. Hướng dẫn viết module hoặc mở rộng module hiện tại

1. Xây dựng Domain Entity chuẩn chỉ, lưu ý về việc bảo mật truy xuất dữ liệu.
- Domain Entity chỉ được thao tác bởi cách Domain Manager ví dụ như Category chỉ được tạo, cập nhật, xóa bằng CategoryManager.
- Các Domain Manager truy cập đc vào các phần tử 'internal' của Domain Entity thông qua cơ chế.
- Hoạt động trên Domain Manager phải sử dụng các DTO để nhận dữ liệu đầu vào và trả về kết quả.
- Mọi method trên Domain Manager được các lập trình viên viết bằng cách sử dụng TDD (Test Driven Development) nên khi thay đổi thêm mới phải đi kèm với unit test tương ứng.
- Domain Manager được đặt tên có hậu là Manager ví dụ như ICategoryManager, CategoryManager. Dto của Domain Manager được đặt tên có hậu là Dto ví dụ như CategoryDto.
- Dto của Domain Manager điều có method Verify() để kiểm tra dữ liệu đầu vào, throw exception nếu dữ liệu không hợp lệ.
- Sử dụng IRepository<T> để thêm, xóa, sửa dữ liệu. Sử dụng IEntityDataReader<T> để đọc dữ liệu.
- Để trả về Dto từ Domain Manager thì sử dụng extension method ToDto() của Domain Entity viết trong thư mục Extensions.

2. Xây dựng Application Service chuẩn chỉ.
- Application Service là nơi chứa logic nghiệp vụ của hệ thống.
- Application Service sử dụng Domain Manager thông qua các interface.
- Mọi hoạt động trên Application Service phải sử dụng các DTO để nhận dữ liệu đầu vào và trả về kết quả.
- Application Service được đặt tên có hậu là AppService ví dụ như ICategoryAppService, CategoryAppService. Dto của Application Service được đặt tên có hậu là AppDto ví dụ như CategoryAppDto.
- Dto của Application Service điều có method Validate() để kiểm tra dữ liệu đầu vào, trả về (valid, errorMessage) nếu dữ liệu không hợp lệ
- Để trả về Dto từ Application Service thì sử dụng extension method ToDto() của DTO trả về từ Domain Manager, viết trong thư mục Extensions.

3. Xây dựng Presentation Layer chuẩn chỉ.
- Presentation Layer là nơi chứa giao diện người dùng của hệ thống.
- Presentation Layer được sử dụng bởi người dùng để tương tác với hệ thống.
- Presentation Layer sử dụng mô hình MVC, unobtrusive validation.
- Model phải có class Validator sử dụng Fluent Validation. Ví dụ: CreateCategoryModel thì có CreateCategoryValidator kế thừa từ AbstractValidator<CreateCategoryModel> để validate dữ liệu.
- Controller sửa dụng ModelFactory để tạo model, code trong controller phải ngắn gọn dễ đọc. Ví dụ Order thì dùng IOrderModelFactory, OrderModelFactory.
- Controller giao tiếp với phần còn lại của hệ thống bằng MediatR. Sử dụng IMediator để gửi command và query.
- Các query handler có quyền sử dụng Application Service để lấy dữ liệu phù hợp cho việc hiển thị, thao tác người dùng.
- Các command handler có quyền sử dụng Application Service để thực hiện các thao tác người dùng.
- Các command và query sử dụng các Model class. Ví dụ: tạo mới danh mục thì dùng CreateCategoryModel, cập nhật danh mục thì dùng UpdateCategoryModel, xóa danh mục thì dùng DeleteCategoryModel.

4. Lưu ý khác:
- KHÔNG chạy các câu lệnh để tạo Migration. Điều này tôi sẽ tự làm.
- KHÔNG chạy các câu lệnh để Update database. Điều này tôi sẽ tự làm.
- LUÔN ĐỌC FILE NÀY và ghi nhớ trước khi lên kế hoạch làm bất kỳ điều gì.
