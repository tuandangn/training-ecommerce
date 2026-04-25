using MediatR;
using NamEcommerce.Web.Contracts.Models.Media;

namespace NamEcommerce.Web.Contracts.Queries.Models.Media;

[Serializable]
public sealed class GetPictureQuery : IRequest<PictureFileModel?>
{
    public required Guid Id { get; init; }
}
