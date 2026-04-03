using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.Customers;
using NamEcommerce.Web.Contracts.Models.Customers;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Customers;

public sealed class GetCustomerListHandler : IRequestHandler<GetCustomerListQuery, CustomerListModel>
{
    private readonly ICustomerAppService _customerAppService;

    public GetCustomerListHandler(ICustomerAppService customerAppService)
    {
        _customerAppService = customerAppService;
    }

    public async Task<CustomerListModel> Handle(GetCustomerListQuery request, CancellationToken cancellationToken)
    {
        var paged = await _customerAppService.GetCustomersAsync(request.Keywords, request.PageIndex, request.PageSize).ConfigureAwait(false);
        var items = paged.Items.Select(c => new CustomerListModel.CustomerItemModel(c.Id)
        {
            FullName = c.FullName,
            PhoneNumber = c.PhoneNumber
        }).ToList();

        return new CustomerListModel
        {
            Keywords = request.Keywords,
            Data = NamEcommerce.Web.Contracts.Models.Common.PagedDataModel.Create(items, paged.Pagination.PageIndex, paged.Pagination.PageSize, paged.Pagination.TotalCount)
        };
    }
}

public sealed class GetCustomerByIdHandler : IRequestHandler<GetCustomerByIdQuery, CustomerModel?>
{
    private readonly ICustomerAppService _customerAppService;

    public GetCustomerByIdHandler(ICustomerAppService customerAppService)
    {
        _customerAppService = customerAppService;
    }

    public async Task<CustomerModel?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _customerAppService.GetCustomerByIdAsync(request.Id).ConfigureAwait(false);
        if (dto == null) return null;

        return new CustomerModel
        {
            Id = dto.Id,
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Note = dto.Note,
            CreatedOnUtc = dto.CreatedOnUtc
        };
    }
}
