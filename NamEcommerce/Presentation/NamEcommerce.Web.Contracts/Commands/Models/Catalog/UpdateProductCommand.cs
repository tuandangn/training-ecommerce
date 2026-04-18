using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Catalog;

[Serializable]
public sealed class UpdateProductCommand : IRequest<UpdateProductResultModel>
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? ShortDesc { get; init; }
    public Guid? CategoryId { get; set; }
    public Guid? UnitMeasurementId { get; set; }
    public int DisplayOrder { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public string? ChangePriceReason { get; set; }
    public FileInfoModel? ImageFile { get; set; }
}
