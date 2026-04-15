using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Preparation;

public sealed record MarkOrderItemDeliveredCommand(
    Guid OrderId, 
    Guid OrderItemId, 
    byte[] PictureData, 
    string FileName, 
    string ContentType) : IRequest<CommonResultModel>;
