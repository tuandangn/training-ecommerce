using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed record ProcessOrderWithoutPaymentCommand(Guid OrderId) : ICommand<CommonActionResultModel>;
