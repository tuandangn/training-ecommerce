using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Queries.Models.Finance;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Models.Finances;

namespace NamEcommerce.Web.Controllers;

public class ExpenseController(IMediator mediator, AppConfig appConfig) : BaseAuthorizedController
{
    public IActionResult Index() => RedirectToAction(nameof(List));

    [Authorize(Policy = SystemPermissions.Finance.ExpensesView)]
    public async Task<IActionResult> List(
        int page, string? keywords = null,
        DateTime? fromDate = null, DateTime? toDate = null,
        int? expenseType = null, string? sortBy = null, bool sortDesc = true)
    {
        var pageNumber = 1;
        if (page > 0) pageNumber = page;
        var pageSize = appConfig.DefaultPageSize;

        var model = await mediator.Send(new GetExpensesQuery
        {
            PageIndex = pageNumber - 1,
            PageSize = pageSize,
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
            ExpenseType = ExpenseType.General,
            AvailableTaxRates = appConfig.TaxRates,
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = SystemPermissions.Finance.ExpensesManage)]
    public async Task<IActionResult> Create(CreateExpenseModel model)
    {
        if (!ModelState.IsValid) {
            model.AvailableTaxRates = appConfig.TaxRates;
            return View(model);
        }

        if (model.ExpenseType == ExpenseType.AssetDisposal)
        {
            AddLocalizedModelError("Error.ExpenseTypeIsNotAllow");
            model.AvailableTaxRates = appConfig.TaxRates;
            return View(model);
        }

        if (model.TaxRate.HasValue && !appConfig.TaxRates.Contains(model.TaxRate.Value))
        {
            AddLocalizedModelError("Error.ExpenseTaxRateInvalid");
            model.AvailableTaxRates = appConfig.TaxRates;
            return View(model);
        }

        var result = await mediator.Send(new CreateExpenseCommand
        {
            Title = model.Title!,
            Description = model.Description,
            AmountWithoutTax = model.AmountWithoutTax,
            TaxRate = model.TaxRate,
            ExpenseType = (int)model.ExpenseType,
            IncurredDate = model.IncurredDate
        });

        if (result.Success)
        {
            NotifyError("Msg.SaveSuccess");
            return RedirectToAction(nameof(List));
        }

        AddLocalizedModelError(result.ErrorMessage ?? "Error.ExpenseCreateFailed");
        model.AvailableTaxRates = appConfig.TaxRates;
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
            IsSystemGenerated = dto.IsSystemGenerated,
            AvailableTaxRates = appConfig.TaxRates
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = SystemPermissions.Finance.ExpensesManage)]
    public async Task<IActionResult> Edit(EditExpenseModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableTaxRates = appConfig.TaxRates;
            return View(model);
        }

        var dto = await mediator.Send(new GetExpenseByIdQuery(model.Id));
        if (dto is null)
        {
            NotifyError("Error.ExpenseIsNotFound");
            return RedirectToAction(nameof(List));
        }

        if (model.ExpenseType == ExpenseType.AssetDisposal && !dto.IsSystemGenerated)
        {
            AddLocalizedModelError("Error.ExpenseTypeIsNotAllow");
            model.AvailableTaxRates = appConfig.TaxRates;
            return View(model);
        }


        if (model.TaxRate.HasValue && !appConfig.TaxRates.Contains(model.TaxRate.Value) && model.TaxRate != dto.TaxRate)
        {
            AddLocalizedModelError("Error.ExpenseTaxRateInvalid");
            model.AvailableTaxRates = appConfig.TaxRates;
            return View(model);
        }

        var result = await mediator.Send(new UpdateExpenseCommand
        {
            Id = model.Id,
            Title = model.Title!,
            Description = model.Description,
            AmountWithoutTax = model.AmountWithoutTax,
            TaxRate = model.TaxRate,
            ExpenseType = (int)model.ExpenseType,
            IncurredDate = model.IncurredDate
        });

        if (result.Success)
        {
            NotifyError("Msg.SaveSuccess");
            return RedirectToAction(nameof(List));
        }

        AddLocalizedModelError(result.ErrorMessage ?? "Error.ExpenseUpdateFailed");
        model.AvailableTaxRates = appConfig.TaxRates;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = SystemPermissions.Finance.ExpensesManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var expense = await mediator.Send(new GetExpenseByIdQuery(id));
        if (expense is null)
        {
            NotifyError("Error.ExpenseIsNotFound");
            return RedirectToAction(nameof(List));
        }
        if (expense.IsSystemGenerated)
        {
            NotifyError("Error.ExpenseCannotDeleted");
            return RedirectToAction(nameof(List));
        }

        await mediator.Send(new DeleteExpenseCommand(id));

        NotifyError("Msg.SaveSuccess");
        return RedirectToAction(nameof(List));
    }

    [Authorize(Policy = SystemPermissions.Finance.ExpensesView)]
    public async Task<IActionResult> Budgets(int? month)
    {
        var now = DateTime.Now;
        var model = await mediator.Send(new GetExpenseBudgetsQuery
        {
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
        {
            NotifyError("Msg.SaveSuccess");
            return RedirectToAction(nameof(Budgets), new { year = command.Year, month = command.Month });
        }

        AddLocalizedModelError(result.ErrorMessage ?? "Msg.OperationFailed");
        return RedirectToAction(nameof(Budgets), new
        {
            year = command.Year,
            month = command.Month
        });
    }
}
