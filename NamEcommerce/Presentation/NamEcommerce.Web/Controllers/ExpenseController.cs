using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Queries.Models.Finance;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Models.Finances;

namespace NamEcommerce.Web.Controllers;

public class ExpenseController(IMediator mediator) : BaseAuthorizedController
{
    public IActionResult Index() => RedirectToAction(nameof(List));

    [Authorize(Policy = SystemPermissions.Finance.ExpensesView)]
    public async Task<IActionResult> List(
        int page = 1,
        string? keywords = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? expenseType = null,
        string? sortBy = null,
        bool sortDesc = true)
    {
        var model = await mediator.Send(new GetExpensesQuery
        {
            Page = page,
            Keywords = keywords,
            FromDate = fromDate.HasValue ? new DateTime(fromDate.Value.Year, fromDate.Value.Month, fromDate.Value.Day, 0, 0, 0, 0) : null,
            ToDate = toDate.HasValue ? new DateTime(toDate.Value.Year, toDate.Value.Month, toDate.Value.Day, 23, 59, 59, 999) : null,
            ExpenseType = expenseType,
            SortBy = sortBy,
            SortDesc = sortDesc
        });

        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = SystemPermissions.Finance.ExpensesManage)]
    public IActionResult Create()
    {
        var model = new CreateExpenseModel
        {
            IncurredDate = DateTime.Today,
            ExpenseType = ExpenseType.General
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = SystemPermissions.Finance.ExpensesManage)]
    public async Task<IActionResult> Create(CreateExpenseModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await mediator.Send(new CreateExpenseCommand
        {
            Title = model.Title!,
            Description = model.Description,
            Amount = model.AmountWithoutTax,
            ExpenseType = (int)model.ExpenseType,
            IncurredDate = model.IncurredDate
        });

        if (result.Success)
            return RedirectToAction(nameof(List));

        AddLocalizedModelError(result.ErrorMessage ?? "Error.ExpenseCreateFailed");
        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = SystemPermissions.Finance.ExpensesManage)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var dto = await mediator.Send(new GetExpenseByIdQuery(id));
        if (dto is null)
        {
            NotifyError("Error.ExpenseIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var model = new EditExpenseModel
        {
            Id = dto.Id,
            Title = dto.Title,
            Description = dto.Description,
            AmountWithoutTax = dto.AmountWithoutTax,
            TaxRate = dto.TaxRate,
            ExpenseType = (ExpenseType)dto.ExpenseType,
            IncurredDate = dto.IncurredDate,
            IsSystemGenerated = dto.IsSystemGenerated
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = SystemPermissions.Finance.ExpensesManage)]
    public async Task<IActionResult> Edit(EditExpenseModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await mediator.Send(new UpdateExpenseCommand
        {
            Id = model.Id,
            Title = model.Title!,
            Description = model.Description,
            Amount = model.AmountWithoutTax ?? 0,
            ExpenseType = (int) model.ExpenseType,
            IncurredDate = model.IncurredDate
        });

        if (result.Success) return RedirectToAction(nameof(List));

        AddLocalizedModelError(result.ErrorMessage ?? "Error.ExpenseUpdateFailed");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = SystemPermissions.Finance.ExpensesManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteExpenseCommand(id));
        return RedirectToAction(nameof(List));
    }

    [Authorize(Policy = SystemPermissions.Finance.ExpensesView)]
    public async Task<IActionResult> Budgets(int? year, int? month)
    {
        var now = DateTime.UtcNow;
        var model = await mediator.Send(new GetExpenseBudgetsQuery
        {
            Year = year ?? now.Year,
            Month = month ?? now.Month
        });

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = SystemPermissions.Finance.ExpensesManage)]
    public async Task<IActionResult> UpsertBudget(UpsertExpenseBudgetCommand command)
    {
        var result = await mediator.Send(command);
        if (result.Success)
            return RedirectToAction(nameof(Budgets), new { year = command.Year, month = command.Month });

        AddLocalizedModelError(result.ErrorMessage ?? "Error.BudgetUpsertFailed");
        return RedirectToAction(nameof(Budgets), new { year = command.Year, month = command.Month });
    }
}
