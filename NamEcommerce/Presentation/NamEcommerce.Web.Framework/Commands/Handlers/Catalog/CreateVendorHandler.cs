using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class CreateVendorHandler : IRequestHandler<CreateVendorCommand, CreateVendorResultModel>
{
    private readonly IVendorAppService _vendorAppService;
    private readonly IVendorDebtAppService _vendorDebtAppService;

    public CreateVendorHandler(IVendorAppService vendorAppService, IVendorDebtAppService vendorDebtAppService)
    {
        _vendorAppService = vendorAppService;
        _vendorDebtAppService = vendorDebtAppService;
    }

    public async Task<CreateVendorResultModel> Handle(CreateVendorCommand request, CancellationToken cancellationToken)
    {
        var result = await _vendorAppService.CreateVendorAsync(new CreateVendorAppDto
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            DisplayOrder = request.DisplayOrder
        }).ConfigureAwait(false);

        if (!result.Success)
            return new CreateVendorResultModel
            {
                Success = false,
                ErrorMessage = result.ErrorMessage,
                CreatedId = default
            };

        if (request.InitialDebt.HasValue && request.InitialDebt.Value > 0)
        {
            await _vendorDebtAppService.CreateInitialDebtAsync(new CreateInitialVendorDebtAppDto
            {
                VendorId = result.CreatedId!.Value,
                TotalAmount = request.InitialDebt.Value
            }).ConfigureAwait(false);
        }

        return new CreateVendorResultModel
        {
            Success = true,
            ErrorMessage = string.Empty,
            CreatedId = result.CreatedId ?? default
        };
    }
}
