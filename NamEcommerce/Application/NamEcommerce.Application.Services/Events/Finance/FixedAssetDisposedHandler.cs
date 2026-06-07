using MediatR;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Events.Finance;
using NamEcommerce.Domain.Shared.Services.Finance;

namespace NamEcommerce.Application.Services.Events.Finance;

public sealed class FixedAssetDisposedHandler(IExpenseManager expenseManager)
    : INotificationHandler<FixedAssetDisposed>
{
    public async Task Handle(FixedAssetDisposed notification, CancellationToken cancellationToken)
    {
        if (notification.RemainingBookValue <= 0) return;

        await expenseManager.CreateExpenseAsync(new CreateExpenseDto
        {
            Title = "Thanh lý TSCĐ - giá trị còn lại",
            Amount = notification.RemainingBookValue,
            ExpenseType = ExpenseType.AssetDisposal,
            IncurredDate = DateTime.UtcNow
        }).ConfigureAwait(false);
    }
}
