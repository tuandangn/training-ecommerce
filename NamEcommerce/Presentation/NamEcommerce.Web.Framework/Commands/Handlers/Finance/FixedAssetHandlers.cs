using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Finance;

public sealed class CreateFixedAssetHandler(IFixedAssetAppService appService)
    : IRequestHandler<CreateFixedAssetCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CreateFixedAssetCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.CreateFixedAssetAsync(new CreateFixedAssetAppDto
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            CostCenter = request.CostCenter,
            AcquisitionDate = request.AcquisitionDate,
            AcquisitionCost = request.AcquisitionCost,
            ResidualValue = request.ResidualValue,
            UsefulLifeMonths = request.UsefulLifeMonths,
            VendorInvoiceNumber = request.VendorInvoiceNumber,
            Note = request.Note
        }).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class UpdateFixedAssetHandler(IFixedAssetAppService appService)
    : IRequestHandler<UpdateFixedAssetCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(UpdateFixedAssetCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.UpdateFixedAssetAsync(
            request.Id, request.Name, request.Description, request.Note, request.CostCenter
        ).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class DisposeFixedAssetHandler(IFixedAssetAppService appService)
    : IRequestHandler<DisposeFixedAssetCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(DisposeFixedAssetCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.DisposeFixedAssetAsync(request.Id, request.DisposedOnUtc).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
