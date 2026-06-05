using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Finance;

public sealed class CreateFixedAssetCommand : IRequest<CommonActionResultModel>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int Category { get; init; }
    public int CostCenter { get; init; }
    public DateTime AcquisitionDate { get; init; }
    public decimal AcquisitionCost { get; init; }
    public decimal ResidualValue { get; init; }
    public int UsefulLifeMonths { get; init; }
    public string? VendorInvoiceNumber { get; init; }
    public string? Note { get; init; }
}

public sealed class UpdateFixedAssetCommand : IRequest<CommonActionResultModel>
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Note { get; init; }
    public int CostCenter { get; init; }
}

public sealed class DisposeFixedAssetCommand : IRequest<CommonActionResultModel>
{
    public required Guid Id { get; init; }
    public DateTime DisposedOnUtc { get; init; }
}
