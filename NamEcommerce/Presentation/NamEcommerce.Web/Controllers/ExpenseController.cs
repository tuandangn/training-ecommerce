using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;

namespace NamEcommerce.Web.Controllers;

public class ExpenseController : BaseAuthorizedController
{
    private readonly IExpenseAppService _expenseAppService;

    public ExpenseController(IExpenseAppService expenseAppService)
    {
        _expenseAppService = expenseAppService;
    }

    public async Task<IActionResult> Index(int page = 1, string? keywords = null, DateTime? fromDate = null, DateTime? toDate = null, int? expenseType = null)
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
            ModelState.AddModelError(string.Empty, errorMessage!);
            return View(dto);
        }

        var result = await _expenseAppService.CreateExpenseAsync(dto);
        if (result.Success)
            return RedirectToAction(nameof(Index));

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Thêm mới thất bại.");
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
            ModelState.AddModelError(string.Empty, errorMessage!);
            return View(dto);
        }

        var result = await _expenseAppService.UpdateExpenseAsync(dto);
        if (result.Success)
            return RedirectToAction(nameof(Index));

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Mã lỗi cập nhật.");
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _expenseAppService.DeleteExpenseAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
