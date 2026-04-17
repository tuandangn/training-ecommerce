using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Contracts.Commands.Models.Debts;

public sealed class RecordCustomerPaymentCommand : IRequest<CommonResultModel>
{
    public required RecordPaymentModel Model { get; init; }
}
