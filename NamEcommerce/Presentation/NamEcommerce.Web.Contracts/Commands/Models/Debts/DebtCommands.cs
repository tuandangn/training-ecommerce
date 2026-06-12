using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Contracts.Commands.Models.Debts;

public sealed class RecordCustomerPaymentCommand : ICommand<CommonActionResultModel>
{
    public required RecordPaymentModel Model { get; init; }
}

public sealed class RecordFlexiblePaymentCommand : ICommand<CommonActionResultModel>
{
    public required RecordPaymentModel Model { get; init; }
}
