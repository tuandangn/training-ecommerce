using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Catalog;

[Serializable]
public sealed class UpdateProductCommand : IRequest<UpdateProductResultModel>
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? ShortDesc { get; set; }
    public Guid? CategoryId { get; set; }
    public IList<Guid> VendorIds { get; set; } = [];
    public Guid? UnitMeasurementId { get; set; }
    public int DisplayOrder { get; set; }
    public decimal? NewUnitPrice { get; set; }
    public string? ChangePriceReason { get; set; }
    public FileInfoModel? ImageFile { get; set; }
}
