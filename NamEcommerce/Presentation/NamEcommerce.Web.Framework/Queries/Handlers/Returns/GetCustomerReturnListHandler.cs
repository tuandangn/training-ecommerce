using MediatR;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Returns;

public sealed class GetCustomerReturnListHandler : IRequestHandler<GetCustomerReturnListQuery, CustomerReturnListModel>
{
    private readonly ICustomerReturnAppService _customerReturnAppService;

    public GetCustomerReturnListHandler(ICustomerReturnAppService customerReturnAppService)
    {
        _customerReturnAppService = customerReturnAppService;
    }

    public async Task<CustomerReturnListModel> Handle(GetCustomerReturnListQuery request, CancellationToken cancellationToken)
    {
        var (total, items) = await _customerReturnAppService.GetListAsync(
            request.CustomerId,
            request.OrderId,
            request.Status,
            request.PageIndex,
            request.PageSize
        ).ConfigureAwait(false);

        var itemModels = items.Select(dto => new CustomerReturnListModel.ItemModel(dto.Id)
        {
            Code = dto.Code,
            CustomerName = dto.CustomerName,
            OrderCode = dto.OrderCode,
            WarehouseName = dto.WarehouseName,
            Status = dto.Status,
            ReturnDate = DateTimeHelper.ToLocalTime(dto.ReturnDate),
            TotalAmount = dto.Items.Sum(i => i.AcceptedTotal),
            ItemCount = dto.Items.Count()
        }).ToList();

        return new CustomerReturnListModel
        {
            CustomerId = request.CustomerId,
            OrderId = request.OrderId,
            Status = request.Status,
            Data = PagedDataModel.Create(itemModels, request.PageIndex, request.PageSize, total)
        };
    }
}
