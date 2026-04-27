using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;

[Serializable]
public sealed record SetGoodsReceiptToPurchaseOrderCommand(Guid Id, Guid PurchaseOrderId) : IRequest<CommonActionResultModel>;
