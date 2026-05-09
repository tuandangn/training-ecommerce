using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Commands.Models.Returns;
using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Returns;

public sealed class CreateCustomerReturnHandler : IRequestHandler<CreateCustomerReturnCommand, CreateCustomerReturnResultModel>
{
    private readonly ICustomerReturnAppService _customerReturnAppService;
    private readonly ICurrentUserService _currentUserService;

    public CreateCustomerReturnHandler(
        ICustomerReturnAppService customerReturnAppService,
        ICurrentUserService currentUserService)
    {
        _customerReturnAppService = customerReturnAppService;
        _currentUserService = currentUserService;
    }

    public async Task<CreateCustomerReturnResultModel> Handle(CreateCustomerReturnCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var result = await _customerReturnAppService.CreateAsync(new CreateCustomerReturnAppDto
        {
            DeliveryNoteId = request.DeliveryNoteId,
            CustomerId = request.CustomerId,
            WarehouseId = request.WarehouseId,
            Note = request.Note,
            AdditionalCost = request.AdditionalCost,
            Items = request.Items.Select(i => new CreateCustomerReturnItemAppDto
            {
                ProductId = i.ProductId,
                DeliveryNoteItemId = i.DeliveryNoteItemId,
                RequestedQuantity = i.RequestedQuantity,
                AcceptedQuantity = i.AcceptedQuantity,
                OriginalUnitPrice = i.OriginalUnitPrice,
                ReturnUnitPrice = i.ReturnUnitPrice
            }).ToList()
        }, currentUser?.Id).ConfigureAwait(false);

        return new CreateCustomerReturnResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}
