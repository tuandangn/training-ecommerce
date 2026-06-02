using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Commands.Models.Returns;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Returns;

public sealed class CreateCustomerReturnHandler : IRequestHandler<CreateCustomerReturnCommand, CreateCustomerReturnResultModel>
{
    private readonly ICustomerReturnAppService _customerReturnAppService;

    public CreateCustomerReturnHandler(ICustomerReturnAppService customerReturnAppService)
    {
        _customerReturnAppService = customerReturnAppService;
    }

    public async Task<CreateCustomerReturnResultModel> Handle(CreateCustomerReturnCommand request, CancellationToken cancellationToken)
    {
        if (request.Items.Any(i => i.QuantityDecimalPlaces == 0 && i.RequestedQuantity != Math.Floor(i.RequestedQuantity)))
        {
            return new CreateCustomerReturnResultModel
            {
                Success = false,
                ErrorMessage = "Error.QuantityMustBeInteger"
            };
        }

        var result = await _customerReturnAppService.CreateAsync(new CreateCustomerReturnAppDto
        {
            DeliveryNoteId = request.DeliveryNoteId,
            CustomerId = request.CustomerId,
            WarehouseId = null,
            Note = request.Note,
            AdditionalCost = request.AdditionalCost,
            CompensateInNextDelivery = request.CompensateInNextDelivery,
            Items = request.Items.Select(i => new CreateCustomerReturnItemAppDto
            {
                ProductId = i.ProductId,
                DeliveryNoteItemId = i.DeliveryNoteItemId,
                RequestedQuantity = i.RequestedQuantity,
                AcceptedQuantity = i.AcceptedQuantity,
                OriginalUnitPrice = i.OriginalUnitPrice,
                ReturnUnitPrice = i.ReturnUnitPrice
            }).ToList()
        }).ConfigureAwait(false);

        return new CreateCustomerReturnResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}
