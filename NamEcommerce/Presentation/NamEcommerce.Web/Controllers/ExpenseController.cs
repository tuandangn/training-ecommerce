using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;

namespace NamEcommerce.Web.Controllers;

public class ExpenseController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IExpenseAppService _expenseAppService;

    public ExpenseController(IMediator mediator, IExpenseAppService expenseAppService)
    {
        _mediator = mediator;
        _expenseAppService = expenseAppService;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(int page = 1, string? keywords = null, DateTime? fromDate = null, DateTime? toDate = null, int? expenseType = null)
    {
        const int pageSize = 20;
        var result = await _expenseAppService.GetExpensesAsync(
            keywords: keywords,
            fromDate: fromDate,
            toDate: toDate,
            expenseType: expenseType,
            pageIndex: page - 1,
            pageSize: pageSize);

        ViewData["Keywords"] = keywords;
        ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
        ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");
        ViewData["ExpenseType"] = expenseType;

        return View(result);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateExpenseAppDto { IncurredDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateExpenseAppDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var (isValid, errorMessage) = dto.Validate();
        if (!isValid)
        {
            AddLocalizedModelError(errorMessage);
            return View(dto);
        }

        var result = await _mediator.Send(new CreateExpenseCommand
        {
            Title = dto.Title,
            Description = dto.Description,
            Amount = dto.Amount,
            ExpenseType = dto.ExpenseType,
            IncurredDate = dto.IncurredDate
        }).ConfigureAwait(false);
        if (result.Success)
            return RedirectToAction(nameof(List));

        AddLocalizedModelError(result.ErrorMessage ?? "Error.ExpenseCreateFailed");
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var expense = await _expenseAppService.GetExpenseByIdAsync(id);
        if (expense == null) return NotFound();

        var dto = new UpdateExpenseAppDto
        {
            Id = expense.Id,
            Title = expense.Title,
            Description = expense.Description,
            Amount = expense.Amount,
            ExpenseType = expense.ExpenseType,
            IncurredDate = expense.IncurredDate
        };
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateExpenseAppDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var (isValid, errorMessage) = dto.Validate();
        if (!isValid)
        {
            AddLocalizedModelError(errorMessage);
            return View(dto);
        }

        var result = await _mediator.Send(new UpdateExpenseCommand
        {
            Id = dto.Id,
            Title = dto.Title,
            Description = dto.Description,
            Amount = dto.Amount,
            ExpenseType = dto.ExpenseType,
            IncurredDate = dto.IncurredDate
        }).ConfigureAwait(false);
        if (result.Success)
            return RedirectToAction(nameof(List));

        AddLocalizedModelError(result.ErrorMessage ?? "Error.ExpenseUpdateFailed");
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteExpenseCommand(id)).ConfigureAwait(false);
        return RedirectToAction(nameof(List));
    }
}
