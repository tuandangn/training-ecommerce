using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

public sealed record CopyPurchaseOrderCommand(Guid Id) : IRequest<CreatePurchaseOrderResultModel>;
