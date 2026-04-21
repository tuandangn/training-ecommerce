# Presentation Layer — Hướng Dẫn Chi Tiết

## Luồng Request

```
Controller → ModelFactory → _mediator.Send(Query/Command)
                               → Handler (Web.Framework)
                                   → AppService (pageIndex, DateTimeUtc)
```

---

## ⚠️ Web.Contracts CHỈ phụ thuộc MediatR

- Command/Query KHÔNG ĐƯỢC dùng AppDto hay type từ project khác
- Command/Query CHỈ return Model trong `Web.Contracts/Models/{Module}/`
- Handler trong `Web.Framework` làm việc ánh xạ AppDto → Model

```csharp
// ❌ SAI
public sealed class GetXyzListQuery : IRequest<IPagedDataAppDto<XyzAppDto>>
// ✅ ĐÚNG
public sealed class GetXyzListQuery : IRequest<XyzListModel>
```

---

## ⚠️ DateTime trong Presentation

Model / Command / SearchModel KHÔNG có hậu tố `Utc`.

```csharp
// ✅ Model — LocalTime, không hậu tố Utc
public sealed record XyzModel
{
    public DateTime CreatedOn { get; init; }   // LocalTime
    public DateTime? FromDate { get; init; }   // LocalTime
}

// ✅ Handler — convert khi giao tiếp với AppService
// AppDto → Model: .ToLocalTime()
CreatedOn = appDto.CreatedOnUtc.ToLocalTime()

// Command → AppDto: DateTimeHelper.ToUniversalTime()
FromDateUtc = DateTimeHelper.ToUniversalTime(request.FromDate)
```

---

## ⚠️ Pagination trong Presentation

- **SearchModel, Command, View Model:** Dùng `PageNumber` (bắt đầu từ **1**), KHÔNG có `PageIndex`
- **Query trong Web.Contracts:** Có thể dùng `PageIndex` (giao tiếp nội bộ với Handler)
- **ModelFactory / Handler:** Chuyển đổi `PageIndex = PageNumber - 1`

```csharp
// ✅ SearchModel — PageNumber
public sealed class XyzListSearchModel
{
    public int PageNumber { get; set; } = 1;  // KHÔNG dùng PageIndex
    public int PageSize { get; set; }
}

// ✅ ModelFactory chuyển đổi
var pageIndex = Math.Max(1, searchModel.PageNumber) - 1;
await _mediator.Send(new GetXyzListQuery { PageIndex = pageIndex, PageSize = pageSize });

// ✅ Handler truyền pageIndex xuống AppService
var result = await _xyzAppService.GetXyzsAsync(request.Keywords, request.PageIndex, request.PageSize);
```

---

## Template: Models (Web.Contracts)

```csharp
// NamEcommerce.Web.Contracts/Models/{Module}/XyzModels.cs
public sealed record XyzModel
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedOn { get; init; }   // LocalTime, KHÔNG hậu tố Utc
    public DateTime? FromDate { get; init; }   // LocalTime, KHÔNG hậu tố Utc
}

public sealed record XyzListModel
{
    public required IPagedDataModel<XyzItemModel> Data { get; init; }
}
public sealed record XyzItemModel(Guid Id)
{
    public required string Name { get; init; }
    public DateTime CreatedOn { get; init; }   // LocalTime
}

public sealed record CreateXyzResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }
    public Guid CreatedId { get; set; }
}
public sealed record UpdateXyzResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }
    public Guid UpdatedId { get; set; }
}
public sealed record DeleteXyzResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }
}
```

---

## Template: Commands

```csharp
// NamEcommerce.Web.Contracts/Commands/Models/{Module}/XyzCommands.cs
[Serializable]
public sealed class CreateXyzCommand : IRequest<CreateXyzResultModel>
{
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }
    public DateTime? FromDate { get; init; }   // LocalTime, KHÔNG hậu tố Utc
}

[Serializable]
public sealed class UpdateXyzCommand : IRequest<UpdateXyzResultModel>
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }
}

[Serializable]
public sealed class DeleteXyzCommand(Guid id) : IRequest<DeleteXyzResultModel>
{
    public Guid Id { get; } = id;
}
```

---

## Template: Queries

```csharp
// NamEcommerce.Web.Contracts/Queries/Models/{Module}/XyzQueries.cs
public sealed class GetXyzQuery : IRequest<XyzModel?>
{
    public required Guid Id { get; init; }
}

public sealed class GetXyzListQuery : IRequest<XyzListModel>
{
    public string? Keywords { get; init; }
    public int PageIndex { get; init; } = 0;   // PageIndex ok — giao tiếp nội bộ Handler↔AppService
    public int PageSize { get; init; } = int.MaxValue;
    public DateTime? FromDateUtc { get; init; }  // Handler đã convert, truyền thẳng Utc vào Query
}
```

---

## Template: Handlers (Web.Framework)

```csharp
// Command Handler — convert LocalTime→UTC và map AppDto→Model
public sealed class CreateXyzHandler : IRequestHandler<CreateXyzCommand, CreateXyzResultModel>
{
    private readonly IXyzAppService _xyzAppService;
    public CreateXyzHandler(IXyzAppService xyzAppService) { _xyzAppService = xyzAppService; }

    public async Task<CreateXyzResultModel> Handle(CreateXyzCommand request, CancellationToken ct)
    {
        var result = await _xyzAppService.CreateXyzAsync(new CreateXyzAppDto
        {
            Name         = request.Name,
            DisplayOrder = request.DisplayOrder,
            FromDateUtc  = DateTimeHelper.ToUniversalTime(request.FromDate),  // LocalTime → UTC
        }).ConfigureAwait(false);

        return new CreateXyzResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId ?? default
        };
    }
}

// Query Handler — map AppDto→Model, convert UTC→LocalTime
public sealed class GetXyzHandler : IRequestHandler<GetXyzQuery, XyzModel?>
{
    private readonly IXyzAppService _xyzAppService;
    public GetXyzHandler(IXyzAppService xyzAppService) { _xyzAppService = xyzAppService; }

    public async Task<XyzModel?> Handle(GetXyzQuery request, CancellationToken ct)
    {
        var dto = await _xyzAppService.GetXyzByIdAsync(request.Id);
        if (dto is null) return null;

        return new XyzModel
        {
            Id          = dto.Id,
            Name        = dto.Name,
            DisplayOrder = dto.DisplayOrder,
            CreatedOn   = dto.CreatedOnUtc.ToLocalTime(),   // UTC → LocalTime
            FromDate    = dto.FromDateUtc?.ToLocalTime(),   // UTC → LocalTime (nullable)
        };
    }
}

// List Query Handler — chuyển PageIndex sang AppService
public sealed class GetXyzListHandler : IRequestHandler<GetXyzListQuery, XyzListModel>
{
    private readonly IXyzAppService _xyzAppService;
    public GetXyzListHandler(IXyzAppService xyzAppService) { _xyzAppService = xyzAppService; }

    public async Task<XyzListModel> Handle(GetXyzListQuery request, CancellationToken ct)
    {
        var pagedData = await _xyzAppService.GetXyzsAsync(
            request.Keywords,
            request.PageIndex,    // đã là 0-based từ ModelFactory
            request.PageSize);

        return new XyzListModel
        {
            Data = PagedDataModel.Create(
                pagedData.Items.Select(dto => new XyzItemModel(dto.Id)
                {
                    Name      = dto.Name,
                    CreatedOn = dto.CreatedOnUtc.ToLocalTime()  // UTC → LocalTime
                }),
                request.PageIndex, request.PageSize, pagedData.Pagination.TotalCount)
        };
    }
}
```

---

## Template: ModelFactory

```csharp
// NamEcommerce.Web/Services/{Module}/XyzModelFactory.cs
public sealed class XyzModelFactory : IXyzModelFactory
{
    private readonly IMediator _mediator;
    private readonly AppConfig _appConfig;

    public XyzModelFactory(IMediator mediator, AppConfig appConfig)
    {
        _mediator = mediator;
        _appConfig = appConfig;
    }

    public async Task<XyzListModel> PrepareXyzListModel(XyzListSearchModel searchModel)
    {
        var pageSize   = searchModel?.PageSize > 0 ? searchModel.PageSize : _appConfig.DefaultPageSize;
        var pageIndex  = Math.Max(1, searchModel?.PageNumber ?? 1) - 1;  // PageNumber → PageIndex

        return await _mediator.Send(new GetXyzListQuery
        {
            Keywords     = searchModel?.Keywords,
            PageIndex    = pageIndex,   // 0-based
            PageSize     = pageSize,
            FromDateUtc  = DateTimeHelper.ToUniversalTime(searchModel?.FromDate),  // LocalTime → UTC
        }).ConfigureAwait(false);
    }

    public async Task<EditXyzModel?> PrepareEditXyzModel(Guid id, EditXyzModel? oldModel = null)
    {
        var xyz = await _mediator.Send(new GetXyzQuery { Id = id }).ConfigureAwait(false);
        if (xyz is null && oldModel is null) return null;

        return oldModel ?? new EditXyzModel
        {
            Id       = xyz!.Id,
            Name     = xyz.Name,
            FromDate = xyz.FromDate   // Đã là LocalTime từ Handler
        };
    }
}
```

---

## Template: View Models + Fluent Validation

```csharp
// NamEcommerce.Web/Models/{Module}/XyzModels.cs
public sealed class CreateXyzModel
{
    public string? Name { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public DateTime? FromDate { get; set; }  // LocalTime — KHÔNG hậu tố Utc
}

public sealed class CreateXyzValidator : AbstractValidator<CreateXyzModel>
{
    public CreateXyzValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên không được để trống")
            .MaximumLength(200);
        RuleFor(x => x.FromDate).NotNull().WithMessage("Ngày bắt đầu không được để trống");
    }
}

public sealed class XyzListSearchModel
{
    public string? Keywords { get; set; }
    public int PageNumber { get; set; } = 1;  // KHÔNG dùng PageIndex
    public int PageSize { get; set; }
    public DateTime? FromDate { get; set; }   // LocalTime — KHÔNG hậu tố Utc
}
```

---

## Template: Controller

```csharp
// NamEcommerce.Web/Controllers/XyzController.cs
public sealed class XyzController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IXyzModelFactory _xyzModelFactory;

    public XyzController(IMediator mediator, IXyzModelFactory xyzModelFactory)
    {
        _mediator = mediator;
        _xyzModelFactory = xyzModelFactory;
    }

    public async Task<IActionResult> List(XyzListSearchModel searchModel)
        => View(await _xyzModelFactory.PrepareXyzListModel(searchModel));

    public async Task<IActionResult> Create()
        => View(await _xyzModelFactory.PrepareCreateXyzModel());

    [HttpPost]
    public async Task<IActionResult> Create(CreateXyzModel model)
    {
        if (!ModelState.IsValid)
            return View(await _xyzModelFactory.PrepareCreateXyzModel(model));

        var result = await _mediator.Send(new CreateXyzCommand
        {
            Name     = model.Name!,
            FromDate = model.FromDate   // LocalTime — Handler convert sang UTC
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(await _xyzModelFactory.PrepareCreateXyzModel(model));
        }

        TempData[ViewConstants.SuccessMessage] = "Thêm mới thành công!";
        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteXyzCommand(id));
        if (!result.Success)
            TempData[ViewConstants.ErrorMessage] = result.ErrorMessage;
        else
            TempData[ViewConstants.SuccessMessage] = "Xóa thành công!";
        return RedirectToAction(nameof(List));
    }
}
```

---

## Tóm tắt: 3 conventions xuyên suốt

```
                 PageNumber (1-based)   DateTime (LocalTime)   Type
Presentation  │  PageNumber            CreatedOn              Model
              │      ↓ -1                  ↓ ToUniversalTime()   ↓ map
Application   │  pageIndex (0-based)   CreatedOnUtc           AppDto
              │                            ↓ từ DB UTC
Domain/DB     │                        CreatedOnUtc (UTC)
```
