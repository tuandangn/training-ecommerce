using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Web.Contracts.Commands.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Models.Returns;
using NamEcommerce.Web.Services.Returns;

namespace NamEcommerce.Web.Controllers;

public sealed class CustomerReturnController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly ICustomerReturnModelFactory _customerReturnModelFactory;
    private readonly IEntityDataReader<DeliveryNote> _deliveryNoteDataReader;
    private readonly IEntityDataReader<Customer> _customerDataReader;

    public CustomerReturnController(IMediator mediator, ICustomerReturnModelFactory customerReturnModelFactory,
        IEntityDataReader<DeliveryNote> deliveryNoteDataReader, IEntityDataReader<Customer> customerDataReader)
    {
        _mediator = mediator;
        _customerReturnModelFactory = customerReturnModelFactory;
        _deliveryNoteDataReader = deliveryNoteDataReader;
        _customerDataReader = customerDataReader;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(CustomerReturnListSearchModel search)
    {
        var model = await _customerReturnModelFactory.PrepareCustomerReturnListModel(search);
        return View(model);
    }

    public async Task<IActionResult> Create(Guid? deliveryNoteId = null, Guid? customerId = null)
    {
        CreateCustomerReturnModel? prefilledModel = null;
        if (deliveryNoteId.HasValue || customerId.HasValue)
        {
            prefilledModel = new CreateCustomerReturnModel();

            DeliveryNote? deliveryNote = null;
            Customer? customer = null;
            if (deliveryNoteId.HasValue)
                deliveryNote = await _deliveryNoteDataReader.GetByIdAsync(deliveryNoteId.Value);
            if (deliveryNote is not null)
            {
                prefilledModel.DeliveryNoteId = deliveryNote.Id;
                prefilledModel.DeliveryNoteDisplayCode = deliveryNote.Code;

                customer = await _customerDataReader.GetByIdAsync(deliveryNote.CustomerId);
            }
            else if (customerId.HasValue)
            {
                customer = await _customerDataReader.GetByIdAsync(customerId.Value);
            }
            prefilledModel.CustomerId = customer?.Id;
            prefilledModel.CustomerDisplayName = customer?.FullName;
            prefilledModel.CustomerDisplayPhone = customer?.PhoneNumber;
            prefilledModel.CustomerDisplayAddress = customer?.Address;
        }

        var model = await _customerReturnModelFactory.PrepareCreateCustomerReturnModel(prefilledModel);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerReturnModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await _customerReturnModelFactory.PrepareCreateCustomerReturnModel(model);
            return View(model);
        }

        var returnItems = model.Items
            .Where(i => i.ProductId.HasValue && i.RequestedQuantity > 0)
            .ToList();

        if (!returnItems.Any())
        {
            AddLocalizedModelError("Error.CustomerReturn.NoItems");
            model = await _customerReturnModelFactory.PrepareCreateCustomerReturnModel(model);
            return View(model);
        }

        if (!model.CustomerId.HasValue)
        {
            AddLocalizedModelError("Error.CustomerReturn.CustomerRequired");
            model = await _customerReturnModelFactory.PrepareCreateCustomerReturnModel(model);
            return View(model);
        }

        if (!model.WarehouseId.HasValue)
        {
            AddLocalizedModelError("Error.CustomerReturn.WarehouseRequired");
            model = await _customerReturnModelFactory.PrepareCreateCustomerReturnModel(model);
            return View(model);
        }

        var result = await _mediator.Send(new CreateCustomerReturnCommand
        {
            DeliveryNoteId = model.DeliveryNoteId,
            CustomerId = model.CustomerId.Value,
            WarehouseId = model.WarehouseId.Value,
            AdditionalCost = model.AdditionalCost,
            Note = model.Note,
            Items = returnItems.Select(i => new CreateCustomerReturnItemCommand
            {
                ProductId = i.ProductId!.Value,
                DeliveryNoteItemId = i.DeliveryNoteItemId,
                RequestedQuantity = i.RequestedQuantity,
                // AcceptedQuantity không có field riêng trong form — default = RequestedQuantity (Draft)
                AcceptedQuantity = i.AcceptedQuantity > 0 ? i.AcceptedQuantity : i.RequestedQuantity,
                OriginalUnitPrice = i.OriginalUnitPrice,
                ReturnUnitPrice = i.ReturnUnitPrice
            }).ToList()
        });

        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            model = await _customerReturnModelFactory.PrepareCreateCustomerReturnModel(model);
            return View(model);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _customerReturnModelFactory.PrepareCustomerReturnDetailsModel(id);
        if (model is null)
        {
            NotifyError("Error.CustomerReturn.IsNotFound");
            return RedirectToAction(nameof(List));
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Update(Guid id, string? note, DateTime? returnDate)
    {
        var result = await _mediator.Send(new UpdateCustomerReturnCommand
        {
            Id = id,
            Note = note,
            ReturnDate = returnDate
        });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> MoveToInspecting(Guid id)
    {
        var result = await _mediator.Send(new MoveCustomerReturnToInspectingCommand { Id = id });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var result = await _mediator.Send(new ConfirmCustomerReturnCommand { Id = id });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var (success, errorMessage) = await _mediator.Send(new CancelCustomerReturnCommand { Id = id });

        if (!success)
        {
            NotifyError(errorMessage!);
        }
        else
        {
            NotifySuccess("Msg.CancelSuccess");
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetById(Guid id)
    {
        var model = await _mediator.Send(new GetCustomerReturnQuery { Id = id });
        if (model is null)
            return Json(new { success = false });

        return Json(new { success = true, data = model });
    }

    [HttpGet]
    public async Task<IActionResult> GetDeliveryNotes(Guid customerId)
    {
        var notes = await _mediator.Send(new GetDeliveryNotesByCustomerQuery { CustomerId = customerId });
        return Json(notes);
    }

    [HttpGet]
    public async Task<IActionResult> GetDeliveryNoteItems(Guid deliveryNoteId, Guid? excludeReturnId = null)
    {
        var items = await _mediator.Send(new GetDeliveryNoteItemsForReturnQuery
        {
            DeliveryNoteId = deliveryNoteId,
            ExcludeReturnId = excludeReturnId
        });
        return Json(items);
    }

    [HttpGet]
    public async Task<IActionResult> GetReturnableItems(Guid customerId, Guid? excludeReturnId = null)
    {
        var items = await _mediator.Send(new GetReturnableItemsByCustomerQuery
        {
            CustomerId = customerId,
            ExcludeReturnId = excludeReturnId
        });
        return Json(items);
    }

    [HttpGet]
    public async Task<IActionResult> GetByDeliveryNote(Guid deliveryNoteId)
    {
        var result = await _mediator.Send(new GetCustomerReturnListQuery
        {
            DeliveryNoteId = deliveryNoteId,
            PageIndex = 0,
            PageSize = 100
        });
        return Json(result.Data.Items.Select(r => new
        {
            id = r.Id,
            code = r.Code,
            status = r.Status,
            statusLabel = r.Status switch
            {
                0 => "Bản nháp",
                1 => "Đang kiểm hàng",
                2 => "Đã xác nhận",
                3 => "Đã hủy",
                _ => "?"
            },
            statusColor = r.Status switch
            {
                0 => "secondary",
                1 => "warning",
                2 => "success",
                3 => "danger",
                _ => "secondary"
            },
            returnDate = r.ReturnDate.ToLocalTime().ToString("dd/MM/yyyy"),
            totalAmount = r.TotalAmount,
            itemCount = r.ItemCount
        }));
    }
}
