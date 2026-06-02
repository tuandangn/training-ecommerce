using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Queries.Models.Finance;

namespace NamEcommerce.Web.Controllers;

public class ExpenseController(IMediator mediator) : BaseAuthorizedController
{
    public IActionResult Index() => RedirectToAction(nameof(List));

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
            FromDate = fromDate,
            ToDate = toDate,
            ExpenseType = expenseType,
            SortBy = sortBy,
            SortDesc = sortDesc
        }).ConfigureAwait(false);

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
        => View(new CreateExpenseAppDto { IncurredDate = DateTime.Today, ExpenseType = 5 });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateExpenseAppDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var (isValid, errorMessage) = dto.Validate();
        if (!isValid)
        {
            AddLocalizedModelError(errorMessage);
            return View(dto);
        }

        var result = await mediator.Send(new CreateExpenseCommand
        {
            Title = dto.Title,
            Description = dto.Description,
            Amount = dto.Amount,
            ExpenseType = dto.ExpenseType,
            IncurredDate = dto.IncurredDate
        }).ConfigureAwait(false);

        if (result.Success) return RedirectToAction(nameof(List));

        AddLocalizedModelError(result.ErrorMessage ?? "Error.ExpenseCreateFailed");
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await mediator.Send(new GetExpenseByIdQuery(id)).ConfigureAwait(false);
        if (model is null) return NotFound();

        ViewData["IsSystemGenerated"] = model.IsSystemGenerated;
        return View(new UpdateExpenseAppDto
        {
            Id = model.Id,
            Title = model.Title,
            Description = model.Description,
            Amount = model.Amount,
            ExpenseType = model.ExpenseType,
            IncurredDate = model.IncurredDate
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateExpenseAppDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var (isValid, errorMessage) = dto.Validate();
        if (!isValid)
        {
            AddLocalizedModelError(errorMessage);
            return View(dto);
        }

        var result = await mediator.Send(new UpdateExpenseCommand
        {
            Id = dto.Id,
            Title = dto.Title,
            Description = dto.Description,
            Amount = dto.Amount,
            ExpenseType = dto.ExpenseType,
            IncurredDate = dto.IncurredDate
        }).ConfigureAwait(false);

        if (result.Success) return RedirectToAction(nameof(List));

        AddLocalizedModelError(result.ErrorMessage ?? "Error.ExpenseUpdateFailed");
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteExpenseCommand(id)).ConfigureAwait(false);
        return RedirectToAction(nameof(List));
    }

    public async Task<IActionResult> Budgets(int? year, int? month)
    {
        var now = DateTime.UtcNow;
        var model = await mediator.Send(new GetExpenseBudgetsQuery
        {
            Year = year ?? now.Year,
            Month = month ?? now.Month
        }).ConfigureAwait(false);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertBudget(UpsertExpenseBudgetCommand command)
    {
        var result = await mediator.Send(command).ConfigureAwait(false);
        if (result.Success)
            return RedirectToAction(nameof(Budgets), new { year = command.Year, month = command.Month });

        AddLocalizedModelError(result.ErrorMessage ?? "Error.BudgetUpsertFailed");
        return RedirectToAction(nameof(Budgets), new { year = command.Year, month = command.Month });
    }
}
