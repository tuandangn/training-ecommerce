using MediatR;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Models.Inventory;

namespace NamEcommerce.Web.Services.Inventory;

public sealed class WarehouseModelFactory : IWarehouseModelFactory
{
    private readonly IMediator _mediator;
    private readonly AppConfig _appConfig;

    public WarehouseModelFactory(IMediator mediator, AppConfig appConfig)
    {
        _mediator = mediator;
        _appConfig = appConfig;
    }

    private static string CodePrefix => Warehouse.CODE_PREFIX;
    public string PrepareWarehouseCode(string code) => $"{CodePrefix}{code}";

    public Task<CreateWarehouseModel> PrepareCreateWarehouseModel(CreateWarehouseModel? oldModel = null)
    {
        var model = oldModel ?? new CreateWarehouseModel
        {
            IsActive = true,
            DisplayOrder = 1
        };
        model.CodePrefix = CodePrefix;

        return Task.FromResult(model);
    }

    public async Task<EditWarehouseModel?> PrepareEditWarehouseModel(Guid id, EditWarehouseModel? oldModel = null)
    {
        var warehouse = await _mediator.Send(new GetWarehouseQuery { Id = id });
        if (warehouse is null && oldModel is null)
            return null;

        var model = oldModel ?? new EditWarehouseModel
        {
            Id = warehouse!.Id,
            Code = warehouse!.Code.StartsWith(CodePrefix) ? warehouse!.Code.Substring(CodePrefix.Length) : warehouse.Code,
            Name = warehouse!.Name,
            Address = warehouse.Address,
            PhoneNumber = warehouse.PhoneNumber,
            DisplayOrder = warehouse.DisplayOrder,
            IsActive = warehouse.IsActive
        };
        model.CodePrefix = CodePrefix;

        return model;
    }

    public async Task<WarehouseListModel> PrepareWarehouseListModel(WarehouseListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;
        if (_appConfig.PageSizeOptions.Contains(pageSize)) pageSize = _appConfig.DefaultPageSize;

        var model = await _mediator.Send(new GetWarehouseListQuery
        {
            Keywords = searchModel?.Keywords,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return model;
    }
}
