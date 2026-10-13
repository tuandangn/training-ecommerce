using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Queries.Models.Finance;

namespace NamEcommerce.Web.Components;

public sealed class ExpenseBudgetSummaryComponent(IMediator mediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int month)
    {
        if (month < 1 || month > 12)
            throw new ArgumentException("Month is invalid");

        var model = await mediator.Send(new GetExpenseBudgetsQuery
        {
            Month = month
        });

        return View(model);
    }
}
