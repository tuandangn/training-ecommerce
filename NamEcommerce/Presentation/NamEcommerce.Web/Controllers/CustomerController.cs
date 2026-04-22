using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.Customers;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Queries.Models.Customers;
using NamEcommerce.Web.Models.Customers;

namespace NamEcommerce.Web.Controllers;

public sealed class CustomerController : BaseAuthorizedController
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;

    public CustomerController(AppConfig appConfig, IMediator mediator)
    {
        _appConfig = appConfig;
        _mediator = mediator;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(CustomerListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? _appConfig.DefaultPageSize;
        if (pageNumber <= 0) pageNumber = 1;

        var model = await _mediator.Send(new GetCustomerListQuery
        {
            Keywords = searchModel?.Keywords,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        ViewBag.Keywords = searchModel?.Keywords;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        var pageIndex = 0;
        var pageSize = int.MaxValue;

        var result = await _mediator.Send(new GetCustomerListQuery
        {
            Keywords = q,
            PageIndex = pageIndex,
            PageSize = pageSize
        });

        return Json(result.Data.Items.Select(it => new
        {
            id = it.Id,
            name = it.FullName,
            phone = it.PhoneNumber,
            address = it.Address
        }));
    }

    public IActionResult Create()
    {
        return View(new CreateCustomerModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _mediator.Send(new CreateCustomerCommand
        {
            FullName = model.FullName!,
            PhoneNumber = model.PhoneNumber!,
            Email = model.Email,
            Address = model.Address,
            Note = model.Note
        });

        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            return View(model);
        }

        TempData[ViewConstants.CustomerSuccessMessage] = LocalizeError("Msg.SaveSuccess");
        return RedirectToAction(nameof(List));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = id });
        if (customer == null)
        {
            TempData[ViewConstants.CustomerErrorMessage] = LocalizeError("Error.CustomerIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var model = new EditCustomerModel
        {
            Id = customer.Id,
            FullName = customer.FullName,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Address = customer.Address,
            Note = customer.Note
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditCustomerModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _mediator.Send(new UpdateCustomerCommand
        {
            Id = model.Id,
            FullName = model.FullName!,
            PhoneNumber = model.PhoneNumber!,
            Email = model.Email,
            Address = model.Address,
            Note = model.Note
        });

        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            return View(model);
        }

        TempData[ViewConstants.CustomerSuccessMessage] = LocalizeError("Msg.SaveSuccess");
        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteCustomerCommand { Id = id });

        if (!result.Success)
            TempData[ViewConstants.CustomerErrorMessage] = LocalizeError(result.ErrorMessage!);
        else
            TempData[ViewConstants.CustomerSuccessMessage] = LocalizeError("Msg.DeleteSuccess");
        return RedirectToAction(nameof(List));
    }
}
