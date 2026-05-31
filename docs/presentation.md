# Presentation Layer — Chi tiết

## Tổng quan luồng

```
Request → Controller → _mediator.Send(Command/Query)
                              ↓
                    Handler → AppService → kết quả
                              ↓
                    ModelFactory → Prepare{Xxx}Model() → View
```

---

## Controller

```csharp
// NamEcommerce.Web/Controllers/XyzController.cs
[Area("Admin")]
public class XyzController : BaseAuthorizedController   // hoặc BaseController
{
    private readonly IMediator _mediator;
    private readonly IXyzModelFactory _xyzModelFactory;

    public XyzController(IMediator mediator, IXyzModelFactory xyzModelFactory)
    {
        _mediator = mediator;
        _xyzModelFactory = xyzModelFactory;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(ProductSearchModel searchModel) 
    {
        var model = await _xyzModelFactory.PrepareXyzListModelAsync();
        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var model = await _xyzModelFactory.PrepareCreateXyzModelAsync();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateXyzModel model)
    {
        if (!ModelState.IsValid)
            return View(await _xyzModelFactory.PrepareCreateXyzModelAsync(model));

        var command = new CreateXyzCommand { Name = model.Name!, DisplayOrder = model.DisplayOrder };
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.ErrorMessage!);
            return View(await _xyzModelFactory.PrepareCreateXyzModelAsync(model));
        }

        return RedirectToAction(nameof(Index));
    }
}
```

---

## View Models + Validators (Fluent Validation)

```csharp
// NamEcommerce.Web/Models/{Module}/XyzModels.cs
public sealed class CreateXyzModel
{
    public string? Name { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class EditXyzModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public int DisplayOrder { get; set; }
}

// Validators — file riêng
public sealed class CreateXyzValidator : AbstractValidator<CreateXyzModel>
{
    public CreateXyzValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Tên không được để trống")
            .MaximumLength(200).WithMessage("Tên không được vượt quá 200 ký tự");
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
```

---

## ModelFactory

```csharp
// NamEcommerce.Web/Services/{Module}/IXyzModelFactory.cs
public interface IXyzModelFactory
{
    Task<XyzListModel> PrepareXyzListModelAsync(string? keywords = null, int pageIndex = 0, int pageSize = 20);
    Task<CreateXyzModel> PrepareCreateXyzModelAsync(CreateXyzModel? model = null);
    Task<EditXyzModel> PrepareEditXyzModelAsync(Guid id, EditXyzModel? model = null);
}

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

    public async Task<XyzListModel> PrepareXyzListModelAsync(string? keywords = null, int pageIndex = 0, int pageSize = 20)
    {
        // checking input arguments
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;
        if (_appConfig.PageSizeOptions.Contains(pageSize)) pageSize = _appConfig.DefaultPageSize;

        var query = new GetXyzListQuery { Keywords = keywords, PageIndex = pageIndex, PageSize = pageSize };
        return await _mediator.Send(query);
    }

    public Task<CreateXyzModel> PrepareCreateXyzModelAsync(CreateXyzModel? model = null)
    {
        var parentOptions = await _mediator.Send(new GetCategoryOptionListQuery()).ConfigureAwait(false);
        var model = oldModel ?? new CreateXyzModel
        {
            DisplayOrder = 1,
            AvailableParents = parentOptions
        };
        if (oldModel is not null)
            model.AvailableParents = parentOptions;

        return model;
    }

    public async Task<EditXyzModel> PrepareEditXyzModelAsync(Guid id, EditXyzModel? model = null)
    {   
        //quick check existing
        var xyz = await _mediator.Send(new GetCategoryQuery { Id = id }).ConfigureAwait(false);
        if (xyz is null && oldModel is null)
            return null;

        //prepare additional information
        var parentOptions = await _mediator.Send(new GetCategoryOptionListQuery
        {
            ExcludedCategoryId = id
        }).ConfigureAwait(false);
        var model = oldModel ?? new EditXyzModel
        {
            Id = xyz!.Id,
            Name = xyz.Name,
            ParentId = xyz.ParentId,
            DisplayOrder = xyz.DisplayOrder,
            AvailableParents = parentOptions
        };

        if (oldModel is not null)
            model.AvailableParents = parentOptions;

        return model;
    }
}
```

---

## Commands & Queries

```csharp
// NamEcommerce.Web.Contracts/Commands/Models/{Module}/CreateXyzCommand.cs
public sealed class CreateXyzCommand : IRequest<CreateXyzResultModel>
{
    public string? Name { get; set; }
    public int DisplayOrder { get; set; }
}

// NamEcommerce.Web.Contracts/Queries/Models/{Module}/GetXyzListQuery.cs
public sealed class GetXyzListQuery : IRequest<XyzListModel>
{
    public string? Keywords { get; set; }
    public Guid? CategoryId { get; set; }

    public required int PageIndex { get; init; }
    public required int PageSize { get; init; }

}

// NamEcommerce.Web.Contracts/Models/{Module}/XyzModels.cs
public sealed class CreateXyzResultModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CreatedId { get; set; }
}

public sealed class XyzListModel
{
    public string? Keywords { get; set; }
    public required IPagedDataModel<XyzModel> Xyzs { get; set; }

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required string Name { get; init; }
        public required Guid? ParentId { get; set; }
        public required string? ParentName { get; init; }
    }
}
```

---

## Handlers

```csharp
// NamEcommerce.Web.Framework/Commands/Handlers/{Module}/CreateXyzHandler.cs
public sealed class CreateXyzHandler : IRequestHandler<CreateXyzCommand, CreateXyzResultModel>
{
    private readonly IXyzAppService _xyzAppService;
    public CreateXyzHandler(IXyzAppService xyzAppService) => _xyzAppService = xyzAppService;

    public async Task<CreateXyzResultModel> Handle(CreateXyzCommand request, CancellationToken cancellationToken)
    {
        var dto = new CreateXyzAppDto { Name = request.Name!, DisplayOrder = request.DisplayOrder };
        var result = await _xyzAppService.CreateXyzAsync(dto);
        return new CreateXyzResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}

// NamEcommerce.Web.Framework/Queries/Handlers/{Module}/GetXyzListHandler.cs
public sealed class GetXyzListHandler : IRequestHandler<GetXyzListQuery, XyzListModel>
{
    private readonly IXyzAppService _xyzAppService;
    public GetXyzListHandler(IXyzAppService xyzAppService) => _xyzAppService = xyzAppService;

    public async Task<XyzListModel> Handle(GetXyzListQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await _xyzAppService.GetXyzAsync(request.Keywords, request.PageIndex, request.PageSize);

        var breadcrumbOptions = request.BreadcrumbOpts;
        if (string.IsNullOrEmpty(request.BreadcrumbOpts.Separator))
            breadcrumbOptions.Separator = _appConfig.BreadcrumbSeparator;

        var xyzItems = new List<XyzListModel.ItemModel>();
        foreach (var item in pagedData){
            var parent = ...;
            xyzItems.Add(new XyzListModel.ItemModel(item.Id){
                Name = item.Name,
                ParentId = item.ParentId,
                ParentName = parent?.Name
            });
        }

        var model = new XyzListModel
        {
            Keywords = request.Keywords,
            BreadcrumbOpts = breadcrumbOptions,
            Data = PagedDataModel.Create(xyzItems, request.PageIndex, request.PageSize, pagedData.Pagination.TotalCount)
        };
    }
}
```

---

## Lưu ý quan trọng

- `BaseAuthorizedController` yêu cầu đăng nhập, `BaseController` không
- Controller **không inject AppService** — chỉ `IMediator` + `IModelFactory`
- ModelFactory **không inject AppService** — chỉ `IMediator` + `AppConfig`
- Exception: `DeliveryNoteModelFactory` inject AppService trực tiếp (pattern cũ, không làm theo)
- ASP.NET Unobtrusive Validation cần validators đăng ký qua DI
