using MediatR;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Returns;

/// <summary>
/// Lấy danh sách phiếu xuất kho (Delivered, ToCustomer) của khách hàng — dùng cho AJAX picker.
/// </summary>
public sealed class GetDeliveryNotesByCustomerHandler
    : IRequestHandler<GetDeliveryNotesByCustomerQuery, List<DeliveryNotePickerModel>>
{
    private readonly ICustomerReturnAppService _customerReturnAppService;

    public GetDeliveryNotesByCustomerHandler(ICustomerReturnAppService customerReturnAppService)
    {
        _customerReturnAppService = customerReturnAppService;
    }

    public async Task<List<DeliveryNotePickerModel>> Handle(
        GetDeliveryNotesByCustomerQuery request, CancellationToken cancellationToken)
    {
        var dtos = await _customerReturnAppService
            .GetDeliveryNotesByCustomerAsync(request.CustomerId)
            .ConfigureAwait(false);

        return dtos.Select(d => new DeliveryNotePickerModel
        {
            Id = d.Id,
            Code = d.Code,
            DeliveredOn = DateTimeHelper.ToLocalTime(d.DeliveredOnUtc)
        }).ToList();
    }
}
