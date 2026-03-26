using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.UnitMeasurements;

namespace NamEcommerce.Web.Contracts.Commands.Models.Catalog;

[Serializable]
public sealed class UpdateProductCommand : IRequest<UpdateProductResultModel>
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? ShortDesc { get; init; }
    public Guid? CategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public FileInfoModel? ImageFile { get; set; }
}
