using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Domain.Shared.Enums.Customers;
using NamEcommerce.Web.Contracts.Commands.Models.Customers;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Queries.Models.Customers;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Models.Customers;

namespace NamEcommerce.Web.Controllers;

public sealed class CustomerController(
    AppConfig appConfig, IMediator mediator, ICurrentUserService currentUserService,
    ICustomerPortalAdminAppService customerPortalAdminAppService) : BaseAuthorizedController
{
    public IActionResult Index() => RedirectToAction(nameof(List));

    [Authorize(Policy = SystemPermissions.Customers.View)]
    public async Task<IActionResult> List(CustomerListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? appConfig.DefaultPageSize;
        if (pageNumber <= 0) pageNumber = 1;

        var model = await mediator.Send(new GetCustomerListQuery
        {
            Keywords = searchModel?.Keywords,
            Kind = searchModel?.Kind,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        var pageIndex = 0;
        var pageSize = int.MaxValue;

        var result = await mediator.Send(new GetCustomerListQuery
        {
            Keywords = q,
            PageIndex = pageIndex,
            PageSize = pageSize,
            IncludeSystem = true
        });

        return Json(result.Data.Items.Select(it => new
        {
            id = it.Id,
            name = it.FullName,
            phone = it.PhoneNumber,
            address = it.Address,
            kind = it.Kind,
            isSystem = it.IsSystem
        }));
    }

    [HttpGet]
    public async Task<IActionResult> PickItem(Guid id)
    {
        var customer = await mediator.Send(new GetCustomerByIdQuery { Id = id });
        if (customer is null)
            return NotFound();

        return Json(new
        {
            id = customer.Id,
            name = customer.FullName,
            phone = customer.PhoneNumber,
            address = customer.Address,
            kind = customer.Kind,
            isSystem = customer.IsSystem
        });
    }

    [Authorize(Policy = SystemPermissions.Customers.Manage)]
    public async Task<IActionResult> Create()
    {
        var model = new CreateCustomerModel
        {
            Kind = CustomerKind.RetailWalkIn,
            CanChangeCustomerKind = await currentUserService.IsAdminAsync()
        };
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Customers.Manage)]
    public async Task<IActionResult> Create(CreateCustomerModel model)
    {
        if (!ModelState.IsValid)
        {
            model.CanChangeCustomerKind = await currentUserService.IsAdminAsync();
            return View(model);
        }

        var result = await mediator.Send(new CreateCustomerCommand
        {
            FullName = model.FullName!,
            PhoneNumber = model.PhoneNumber!,
            Kind = (int)model.Kind,
            Email = model.Email,
            Address = model.Address ?? string.Empty,
            Note = model.Note,
            InitialDebt = model.InitialDebt
        });

        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            model.CanChangeCustomerKind = await currentUserService.IsAdminAsync();
            return View(model);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(List));
    }

    public async Task<IActionResult> QuickCreate()
    {
        var model = new CreateCustomerModel
        {
            Kind = CustomerKind.RetailWalkIn,
            CanChangeCustomerKind = await currentUserService.IsAdminAsync()
        };
        return PartialView(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Customers.Manage)]
    public async Task<IActionResult> QuickCreate(CreateCustomerModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var result = await mediator.Send(new CreateCustomerCommand
        {
            FullName = model.FullName!,
            PhoneNumber = model.PhoneNumber!,
            Email = model.Email,
            Kind = (int)model.Kind,
            Address = model.Address ?? string.Empty,
            Note = model.Note,
            InitialDebt = 0
        });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        var customer = await mediator.Send(new GetCustomerByIdQuery { Id = result.CreatedId });
        if (customer is null)
            return Json(new { success = false, message = LocalizeError("Error.CustomerIsNotFound") });

        return Json(new
        {
            success = true,
            message = LocalizeError("Msg.SaveSuccess"),
            customer = new
            {
                id = customer.Id,
                name = customer.FullName,
                phone = customer.PhoneNumber,
                address = customer.Address,
                kind = customer.Kind,
                isSystem = customer.IsSystem
            }
        });
    }

    [Authorize(Policy = SystemPermissions.Customers.Manage)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var customer = await mediator.Send(new GetCustomerByIdQuery { Id = id });
        if (customer == null)
        {
            NotifyError("Error.CustomerIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var model = new EditCustomerModel
        {
            Id = customer.Id,
            FullName = customer.FullName,
            PhoneNumber = customer.PhoneNumber,
            CanChangeCustomerKind = await currentUserService.IsAdminAsync(),
            Kind = (CustomerKind)customer.Kind,
            Email = customer.Email,
            Address = customer.Address,
            Note = customer.Note
        };

        await PopulatePortalAccountAsync(model).ConfigureAwait(false);
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Customers.Manage)]
    public async Task<IActionResult> Edit(EditCustomerModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulatePortalAccountAsync(model).ConfigureAwait(false);
            model.CanChangeCustomerKind = await currentUserService.IsAdminAsync();
            return View(model);
        }

        var result = await mediator.Send(new UpdateCustomerCommand
        {
            Id = model.Id,
            FullName = model.FullName!,
            PhoneNumber = model.PhoneNumber!,
            Kind = (int)model.Kind,
            Email = model.Email,
            Address = model.Address ?? string.Empty,
            Note = model.Note
        });

        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            await PopulatePortalAccountAsync(model).ConfigureAwait(false);
            model.CanChangeCustomerKind = await currentUserService.IsAdminAsync();
            return View(model);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Customers.Manage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await mediator.Send(new DeleteCustomerCommand { Id = id });

        if (!result.Success)
            NotifyError(result.ErrorMessage!);
        else
            NotifySuccess("Msg.DeleteSuccess");
        return RedirectToAction(nameof(List));
    }

    private async Task PopulatePortalAccountAsync(EditCustomerModel model)
    {
        if (model.Id == Guid.Empty)
            return;

        var account = await customerPortalAdminAppService.GetAccountAsync(model.Id).ConfigureAwait(false);
        if (account is null)
            return;

        model.HasPortalAccount = true;
        model.PortalAccountStatus = account.Status;
        model.HasPortalPassword = account.HasPassword;
        model.PortalPasswordSetOnUtc = account.PasswordSetOnUtc;
        model.PortalLastLoginOnUtc = account.LastLoginOnUtc;
        model.PortalUpdatedOnUtc = account.UpdatedOnUtc;
    }
}
