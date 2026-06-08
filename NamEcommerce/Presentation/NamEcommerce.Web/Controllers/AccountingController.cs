using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Helpers;

namespace NamEcommerce.Web.Controllers;

[Authorize(Policy = SystemPermissions.Finance.Accounting)]
public class AccountingController(
    IMediator mediator,
    IAccountingSetupAppService setupAppService,
    IBankAccountAppService bankAccountAppService,
    IFixedAssetAppService fixedAssetAppService,
    ICashBookService cashBookService,
    IAccountingReportService reportService) : BaseAuthorizedController
{
    // ── Setup ─────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Setup()
    {
        var model = await setupAppService.GetSetupAsync().ConfigureAwait(false);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Setup(SaveAccountingSetupCommand command)
    {
        var result = await mediator.Send(command).ConfigureAwait(false);
        if (!result.Success) AddLocalizedModelError(result.ErrorMessage);
        else TempData["SuccessMessage"] = "Đã lưu khai báo kế toán.";
        return RedirectToAction(nameof(Setup));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize()
    {
        var result = await mediator.Send(new FinalizeAccountingSetupCommand()).ConfigureAwait(false);
        if (!result.Success) AddLocalizedModelError(result.ErrorMessage);
        else TempData["SuccessMessage"] = "Đã xác nhận và khóa khai báo kế toán.";
        return RedirectToAction(nameof(Setup));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProvision([FromBody] UpdateCorporateTaxProvisionCommand command)
    {
        var result = await mediator.Send(command).ConfigureAwait(false);
        return Json(new { success = result.Success, message = result.ErrorMessage });
    }

    // ── Bank Accounts ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> BankAccounts()
    {
        var accounts = await bankAccountAppService.GetBankAccountsAsync(includeInactive: true).ConfigureAwait(false);
        return View(accounts);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBankAccount([FromBody] CreateBankAccountCommand command)
    {
        var result = await mediator.Send(command).ConfigureAwait(false);
        return Json(new { success = result.Success, message = result.ErrorMessage });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateBankAccount([FromBody] UpdateBankAccountCommand command)
    {
        var result = await mediator.Send(command).ConfigureAwait(false);
        return Json(new { success = result.Success, message = result.ErrorMessage });
    }

    [HttpPost]
    public async Task<IActionResult> SetDefaultBankAccount(Guid id)
    {
        var result = await mediator.Send(new SetDefaultBankAccountCommand(id)).ConfigureAwait(false);
        return Json(new { success = result.Success, message = result.ErrorMessage });
    }

    [HttpPost]
    public async Task<IActionResult> DeactivateBankAccount(Guid id)
    {
        var result = await mediator.Send(new DeactivateBankAccountCommand(id)).ConfigureAwait(false);
        return Json(new { success = result.Success, message = result.ErrorMessage });
    }

    [HttpPost]
    public async Task<IActionResult> ActivateBankAccount(Guid id)
    {
        var result = await mediator.Send(new ActivateBankAccountCommand(id)).ConfigureAwait(false);
        return Json(new { success = result.Success, message = result.ErrorMessage });
    }

    // ── Fixed Assets ──────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> FixedAssets(int? status = null)
    {
        var model = await fixedAssetAppService.GetFixedAssetsAsync(status).ConfigureAwait(false);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> FixedAssetSchedule(Guid id)
    {
        var schedule = await fixedAssetAppService.GetDepreciationScheduleAsync(id).ConfigureAwait(false);
        return Json(schedule);
    }

    [HttpPost]
    public async Task<IActionResult> CreateFixedAsset([FromBody] CreateFixedAssetCommand command)
    {
        var result = await mediator.Send(command).ConfigureAwait(false);
        return Json(new { success = result.Success, message = result.ErrorMessage });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateFixedAsset([FromBody] UpdateFixedAssetCommand command)
    {
        var result = await mediator.Send(command).ConfigureAwait(false);
        return Json(new { success = result.Success, message = result.ErrorMessage });
    }

    [HttpPost]
    public async Task<IActionResult> DisposeFixedAsset([FromBody] DisposeFixedAssetCommand command)
    {
        var result = await mediator.Send(command).ConfigureAwait(false);
        return Json(new { success = result.Success, message = result.ErrorMessage });
    }

    // ── Reports ───────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> CashBook(int? year, int? month, int? quarter, Guid? bankAccountId)
    {
        var y = year ?? DateTime.Today.Year;
        var m = month ?? (quarter.HasValue ? null : (int?)DateTime.Today.Month);
        var period = m.HasValue ? AccountingPeriod.ForMonth(y, m.Value)
            : quarter.HasValue ? AccountingPeriod.ForQuarter(y, quarter.Value)
            : AccountingPeriod.ForYear(y);
        var model = await cashBookService.GetCashBookAsync(period, bankAccountId).ConfigureAwait(false);
        ViewBag.Year = y; ViewBag.Month = m; ViewBag.Quarter = quarter;
        ViewBag.BankAccounts = await bankAccountAppService.GetBankAccountsAsync(includeInactive: false).ConfigureAwait(false);
        ViewBag.BankAccountId = bankAccountId;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> IncomeStatement(int? year, int? month, int? quarter, bool compare = false)
    {
        var y = year ?? DateTime.Today.Year;
        var m = month ?? (quarter.HasValue ? null : (int?)DateTime.Today.Month);
        var period = m.HasValue ? AccountingPeriod.ForMonth(y, m.Value)
            : quarter.HasValue ? AccountingPeriod.ForQuarter(y, quarter.Value)
            : AccountingPeriod.ForYear(y);
        var model = await reportService.GetIncomeStatementAsync(period).ConfigureAwait(false);
        ViewBag.Year = y; ViewBag.Month = m; ViewBag.Quarter = quarter; ViewBag.Compare = compare;

        if (compare)
        {
            var priorPeriod = m.HasValue ? AccountingPeriod.ForMonth(m.Value == 1 ? y - 1 : y, m.Value == 1 ? 12 : m.Value - 1)
                : quarter.HasValue ? (quarter.Value == 1 ? AccountingPeriod.ForQuarter(y - 1, 4) : AccountingPeriod.ForQuarter(y, quarter.Value - 1))
                : AccountingPeriod.ForYear(y - 1);
            ViewBag.PriorModel = await reportService.GetIncomeStatementAsync(priorPeriod).ConfigureAwait(false);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CashFlow(int? year, int? month, int? quarter, bool compare = false)
    {
        var y = year ?? DateTime.Today.Year;
        var m = month ?? (quarter.HasValue ? null : (int?)DateTime.Today.Month);
        var period = m.HasValue ? AccountingPeriod.ForMonth(y, m.Value)
            : quarter.HasValue ? AccountingPeriod.ForQuarter(y, quarter.Value)
            : AccountingPeriod.ForYear(y);
        var model = await reportService.GetCashFlowStatementAsync(period).ConfigureAwait(false);
        ViewBag.Year = y; ViewBag.Month = m; ViewBag.Quarter = quarter; ViewBag.Compare = compare;

        if (compare)
        {
            var priorPeriod = m.HasValue ? AccountingPeriod.ForMonth(m.Value == 1 ? y - 1 : y, m.Value == 1 ? 12 : m.Value - 1)
                : quarter.HasValue ? (quarter.Value == 1 ? AccountingPeriod.ForQuarter(y - 1, 4) : AccountingPeriod.ForQuarter(y, quarter.Value - 1))
                : AccountingPeriod.ForYear(y - 1);
            ViewBag.PriorModel = await reportService.GetCashFlowStatementAsync(priorPeriod).ConfigureAwait(false);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> BalanceSheet(DateTime? asOf)
    {
        var date = asOf ?? DateTime.Today;
        var model = await reportService.GetBalanceSheetAsync(date).ConfigureAwait(false);
        ViewBag.AsOf = date;
        return View(model);
    }

    // ── Excel exports ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ExportIncomeStatementExcel(int? year, int? month, int? quarter)
    {
        var y = year ?? DateTime.Today.Year;
        var m = month ?? (quarter.HasValue ? null : (int?)DateTime.Today.Month);
        var period = m.HasValue ? AccountingPeriod.ForMonth(y, m.Value)
            : quarter.HasValue ? AccountingPeriod.ForQuarter(y, quarter.Value)
            : AccountingPeriod.ForYear(y);
        var model = await reportService.GetIncomeStatementAsync(period).ConfigureAwait(false);
        var bytes = AccountingExcelExporter.ExportIncomeStatement(model);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"B02_KetQuaHDKD_{period.Display.Replace("/", "-")}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportCashFlowExcel(int? year, int? month, int? quarter)
    {
        var y = year ?? DateTime.Today.Year;
        var m = month ?? (quarter.HasValue ? null : (int?)DateTime.Today.Month);
        var period = m.HasValue ? AccountingPeriod.ForMonth(y, m.Value)
            : quarter.HasValue ? AccountingPeriod.ForQuarter(y, quarter.Value)
            : AccountingPeriod.ForYear(y);
        var model = await reportService.GetCashFlowStatementAsync(period).ConfigureAwait(false);
        var bytes = AccountingExcelExporter.ExportCashFlow(model);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"B03_LuuChuyenTienTe_{period.Display.Replace("/", "-")}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportBalanceSheetExcel(DateTime? asOf)
    {
        var date = asOf ?? DateTime.Today;
        var model = await reportService.GetBalanceSheetAsync(date).ConfigureAwait(false);
        var bytes = AccountingExcelExporter.ExportBalanceSheet(model);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"B01_CDKT_{date:yyyyMMdd}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportCashBookExcel(int? year, int? month, int? quarter, Guid? bankAccountId)
    {
        var y = year ?? DateTime.Today.Year;
        var m = month ?? (quarter.HasValue ? null : (int?)DateTime.Today.Month);
        var period = m.HasValue ? AccountingPeriod.ForMonth(y, m.Value)
            : quarter.HasValue ? AccountingPeriod.ForQuarter(y, quarter.Value)
            : AccountingPeriod.ForYear(y);
        var model = await cashBookService.GetCashBookAsync(period, bankAccountId).ConfigureAwait(false);
        var bytes = AccountingExcelExporter.ExportCashBook(model);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"SoQuy_{period.Display.Replace("/", "-")}.xlsx");
    }
}
