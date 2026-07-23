using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Data.SqlServer;

namespace NamEcommerce.Web.Controllers;

public sealed class AdministrationController(ICurrentUserService currentUserService, IWebHostEnvironment webHostEnvironment, IDbContext dbContext) : BaseAuthorizedController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetData()
    {
        if (!await currentUserService.IsAdminAsync())
        {
            NotifyError("Error.MustBeAdministrator");
            return RedirectToAction("Index", "Home");
        }

        if (dbContext is not NamEcommerceEfDbContext namEcommerceDbContext)
        {
            NotifyError("Error.ContextIsInvalid");
            return RedirectToAction("Index", "Home");
        }

        var filePath = Path.Combine(webHostEnvironment.ContentRootPath, "SqlFiles", "ClearData.sql");
        if (!System.IO.File.Exists(filePath))
        {
            NotifyError("Error.FileNotFound");
            return RedirectToAction("Index", "Home");
        }

        var sql = await System.IO.File.ReadAllTextAsync(filePath);
        var batches = sql.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        await using var transaction = await ((IDbContext)namEcommerceDbContext).BeginTransactionAsync();
        try
        {
            foreach (var batch in batches)
            {
                await namEcommerceDbContext.Database.ExecuteSqlRawAsync(batch).ConfigureAwait(false);
            }

            await transaction.CommitAsync().ConfigureAwait(false);
            NotifySuccess("Msg.SaveSuccess");
        }
        catch
        {
            NotifyError("Msg.SaveFailed");
            await transaction.RollbackAsync().ConfigureAwait(false);
            throw;
        }

        return RedirectToAction("Index", "Home");
    }
}
