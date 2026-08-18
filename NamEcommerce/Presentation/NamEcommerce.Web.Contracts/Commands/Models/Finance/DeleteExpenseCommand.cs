using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Finance;

[Serializable]
public sealed record DeleteExpenseCommand(Guid Id) : ICommand<CommonActionResultModel>;
