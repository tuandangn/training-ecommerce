using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Commands.Models.Returns;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Returns;

public sealed class CreateVendorReturnHandler : IRequestHandler<CreateVendorReturnCommand, CreateVendorReturnResultModel>
{
    private readonly IVendorReturnAppService _vendorReturnAppService;

    public CreateVendorReturnHandler(IVendorReturnAppService vendorReturnAppService)
    {
        _vendorReturnAppService = vendorReturnAppService;
    }

    public async Task<CreateVendorReturnResultModel> Handle(CreateVendorReturnCommand request, CancellationToken cancellationToken)
    {
        var result = await _vendorReturnAppService.CreateAsync(new CreateVendorReturnAppDto
        {
            VendorId = request.VendorId,
            GoodsReceiptId = request.GoodsReceiptId,
            WarehouseId = request.WarehouseId,
            Note = request.Note,
            AdditionalCost = request.AdditionalCost,
            Items = request.Items.Select(i => new CreateVendorReturnItemAppDto
            {
                ProductId = i.ProductId,
                GoodsReceiptItemId = i.GoodsReceiptItemId,
                RequestedQuantity = i.RequestedQuantity,
                AcceptedQuantity = i.AcceptedQuantity,
                OriginalUnitCost = i.OriginalUnitCost,
                ReturnUnitCost = i.ReturnUnitCost
            }).ToList()
        }).ConfigureAwait(false);

        return new CreateVendorReturnResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}
