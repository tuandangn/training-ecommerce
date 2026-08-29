using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Settings;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Web.Framework.Services;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Services.PurchaseOrders;

namespace NamEcommerce.Web.Controllers;

public sealed class PurchaseOrderController(IMediator mediator,
    IPurchaseOrderModelFactory purchaseOrderModelFactory, IPurchaseOrderAppService purchaseOrderAppService,
    PurchaseOrderSettings purchaseOrderSettings, AppConfig appConfig) : BaseAuthorizedController
{
    public IActionResult Index() => RedirectToAction(nameof(List));

    [Authorize(Policy = SystemPermissions.PurchaseOrders.View)]
    public async Task<IActionResult> List(PurchaseOrderListSearchModel searchModel)
    {
        var model = await purchaseOrderModelFactory.PreparePurchaseOrderListModel(searchModel);
        return View(model);
    }

    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> Create()
    {
        var model = await purchaseOrderModelFactory.PrepareCreatePurchaseOrderModel();
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> Create(CreatePurchaseOrderModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await purchaseOrderModelFactory.PrepareCreatePurchaseOrderModel(model);
            return View(model);
        }

        var productInfos = model.Items.Count == 0 ? []
            : await mediator.Send(new GetProductsByIdsForOrderQuery
            {
                Ids = model.Items.Select(i => i.ProductId).OfType<Guid>().Distinct().ToList()
            });

        if (model.Items.Count > 0)
        {
            if (model.Items.Any(item => !productInfos.Any(p => p.Id == item.ProductId)))
            {
                AddLocalizedModelError("Error.ProductIsNotFound");
                model = await purchaseOrderModelFactory.PrepareCreatePurchaseOrderModel(model);
                return View(model);
            }

            if (model.VendorId.HasValue)
            {
                var isInvalid = false;

                var candidateVendorIds = productInfos.SelectMany(p => p.AvailableVendors).Select(v => v.Id).Distinct().ToList();
                var validVendorIds = candidateVendorIds.Where(vendorId => productInfos.All(p => p.AvailableVendors.Any(v => v.Id == vendorId))).ToList();

                model.NotHasAppropriatedVendor = isInvalid = validVendorIds.Count == 0;

                if (model.VendorId.HasValue && !validVendorIds.Contains(model.VendorId.Value))
                {
                    AddLocalizedModelError("Error.PurchaseOrder.VendorIsNotAppropriate");
                    model.VendorId = null;
                    isInvalid = true;
                }

                if (isInvalid)
                {
                    model = await purchaseOrderModelFactory.PrepareCreatePurchaseOrderModel(model);
                    return View(model);
                }
            }
        }

        var result = await mediator.Send(new CreatePurchaseOrderCommand
        {
            PlacedOn = model.PlacedOn,
            VendorId = model.VendorId!.Value,
            WarehouseId = model.WarehouseId,
            Note = model.Note,
            ExpectedDeliveryDate = model.ExpectedDeliveryDate,
            Items = model.Items.Select(i => new CreatePurchaseOrderItemCommand
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity ?? 0,
                UnitCost = i.UnitCost ?? 0
            }).ToList()
        });

        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            model = await purchaseOrderModelFactory.PrepareCreatePurchaseOrderModel(model);
            return View(model);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    [Authorize(Policy = SystemPermissions.PurchaseOrders.View)]
    public async Task<IActionResult> ShortageAggregation([FromQuery] GetShortageAggregationQuery query)
    {
        var model = await mediator.Send(query);
        return View(model);
    }

    public async Task<IActionResult> CheckExistingDrafts([FromBody] CheckExistingDraftsCommand command)
    {
        var result = await mediator.Send(command);
        return Json(result);
    }

    public async Task<IActionResult> CheckRelatedPurchaseOrders([FromBody] CheckRelatedPurchaseOrdersCommand command)
    {
        var result = await mediator.Send(command);
        return Json(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> CreateFromShortage([FromBody] CreatePurchaseOrdersFromShortageCommand command)
    {
        var result = await mediator.Send(command);
        if (!result.Success)
            return this.JsonError(LocalizeError(result.ErrorMessage!));

        if (purchaseOrderSettings.CreateFromShortageAutoApproved)
        {
            foreach (var purchaseOrderInfo in result.Items)
            {
                if (!purchaseOrderInfo.CreatedNew)
                    continue;
                await mediator.Send(new SubmitsPurchaseOrderCommand { PurchaseOrderId = purchaseOrderInfo.PurchaseOrderId });
                await mediator.Send(new ApprovesPurchaseOrderCommand { PurchaseOrderId = purchaseOrderInfo.PurchaseOrderId });
            }
        }

        return this.JsonOk(result.Items, Localizer["Msg.PurchaseOrders.CreateSuccess", result.Items.Count].Value);
    }


    [Authorize(Policy = SystemPermissions.PurchaseOrders.QuickCreate)]
    public async Task<IActionResult> QuickCreate()
    {
        var model = await purchaseOrderModelFactory.PrepareQuickCreatePurchaseOrderModel();
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.QuickCreate)]
    public async Task<IActionResult> QuickCreate(PurchaseOrderQuickCreateModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await purchaseOrderModelFactory.PrepareQuickCreatePurchaseOrderModel(model);
            return View(model);
        }

        if (model.VendorId.HasValue)
        {
            var vendor = await mediator.Send(new GetVendorQuery { Id = model.VendorId.Value });
            if (vendor is null)
            {
                AddLocalizedModelError("Error.VendorIsNotFound");
                model = await purchaseOrderModelFactory.PrepareQuickCreatePurchaseOrderModel(model);
                return View(model);
            }
        }

        if (model.Items.Count > 0)
        {
            var productInfos = await mediator.Send(new GetProductsByIdsForOrderQuery
            {
                Ids = model.Items.Select(i => i.ProductId).OfType<Guid>().Distinct().ToList()
            });

            if (model.Items.Any(item => !productInfos.Any(p => p.Id == item.ProductId)))
            {
                AddLocalizedModelError("Error.ProductIsNotFound");
                model = await purchaseOrderModelFactory.PrepareQuickCreatePurchaseOrderModel(model);
                return View(model);
            }

            if (model.VendorId.HasValue)
            {
                var isInvalid = false;

                var candidateVendorIds = productInfos.SelectMany(p => p.AvailableVendors).Select(v => v.Id).Distinct().ToList();
                var validVendorIds = candidateVendorIds.Where(vendorId => productInfos.All(p => p.AvailableVendors.Any(v => v.Id == vendorId))).ToList();

                model.NotHasAppropriatedVendor = isInvalid = validVendorIds.Count == 0;

                if (model.VendorId.HasValue && !validVendorIds.Contains(model.VendorId.Value))
                {
                    AddLocalizedModelError("Error.PurchaseOrder.VendorIsNotAppropriate");
                    model.VendorId = null;
                    isInvalid = true;
                }

                if (isInvalid)
                {
                    model = await purchaseOrderModelFactory.PrepareQuickCreatePurchaseOrderModel(model);
                    return View(model);
                }
            }

            if (model.IsPaid)
            {
                if (model.Total < model.PaidAmount)
                {
                    AddLocalizedModelError("Error.PaidAmountExceedsOrderTotal");
                    model = await purchaseOrderModelFactory.PrepareQuickCreatePurchaseOrderModel(model);
                    return View(model);
                }
            }
        }

        if (model.IsReceived && model.TaxRate.HasValue && !appConfig.TaxRates.Contains(model.TaxRate.Value))
        {
            AddLocalizedModelError("Error.TaxRateIsNotAllowed");
            model = await purchaseOrderModelFactory.PrepareQuickCreatePurchaseOrderModel(model);
            return View(model);
        }


        var result = await mediator.Send(prepareQuickCreateCommand());
        if (result.Success)
        {
            NotifySuccess("Msg.SaveSuccess");
            return RedirectToAction(nameof(Details), new { id = result.CreatedId });
        }

        NotifyError(result.ErrorMessage!);
        model = await purchaseOrderModelFactory.PrepareQuickCreatePurchaseOrderModel(model);
        return View(model);

        //local method
        PurchaseOrderQuickCreateCommand prepareQuickCreateCommand()
        {
            var command = new PurchaseOrderQuickCreateCommand
            {
                PlacedOn = model.PlacedOn,
                VendorId = model.VendorId!.Value,
                IsPaid = model.IsPaid,
                IsReceived = model.IsReceived,
                Note = model.Note,
                Items = model.Items.Select(item => new PurchaseOrderQuickCreateCommand.PurchaseOrderQuickCreateItemModel
                {
                    ProductId = item.ProductId!.Value,
                    Quantity = item.Quantity!.Value,
                    UnitCost = item.UnitCost
                }).ToList()
            };
            if (model.IsReceived)
            {
                foreach (var item in command.Items)
                {
                    item.WarehouseId = model.DefaultWarehouseId;
                }
                command.DefaultWarehouseId = model.DefaultWarehouseId;
                command.ReceivedOn = model.ReceivedOn;
                command.PictureIds = model.PictureIds;
                command.ShippingAmount = model.ShippingAmount;
                command.TaxRate = model.TaxRate;
            }
            else
            {
                command.ExpectedDeliveryDate = model.ExpectedDeliveryDate;
            }
            if (model.IsPaid)
            {
                command.PaymentInfo = new PurchaseOrderQuickCreateCommand.PurchaseOrderQuickCreatePaymentModel
                {
                    PaidAmount = model.PaidAmount!.Value,
                    PaymentMethod = (int)model.PaymentMethod,
                    BankAccountId = model.PaymentMethod == PaymentMethod.BankTransfer ? model.BankAccountId : null
                };
            }

            return command;
        }
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> Edit(EditPurchaseOrderModel model)
    {
        if (!ModelState.IsValid)
        {
            NotifyError("Error.InvalidRequest", GetErrorMessage());
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        var purchaseOrder = await mediator.Send(new GetPurchaseOrderQuery { Id = model.Id });
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        if (model.ExpectedDeliveryDate < DateTime.Now && model.ExpectedDeliveryDate != purchaseOrder.ExpectedDeliveryDate)
        {
            NotifyError("Error.ExpectedDeliveryDateCannotBeInPast");
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        if (!purchaseOrder.CanChangeDate && model.PlacedOn != purchaseOrder.PlacedOn)
        {
            NotifyError("Error.PurchaseOrderCannotUpdateInfo");
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        if (!purchaseOrder.CanChangeVendor && model.VendorId != purchaseOrder.VendorId)
        {
            NotifyError("Error.PurchaseOrderCannotUpdateVendor");
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        if (purchaseOrder.CanChangeFees)
        {
            if (purchaseOrder.Items.Count == 0)
            {
                if (model.ShippingAmount > 0)
                {
                    NotifyError("Error.PurchaseOrderHasNoItemsForShipping");
                    return RedirectToAction(nameof(Details), new { id = model.Id });
                }
                if (model.TaxAmount > 0)
                {
                    NotifyError("Error.PurchaseOrderHasNoItemsForTax");
                    return RedirectToAction(nameof(Details), new { id = model.Id });
                }
            }
            else
            {
                if (model.TaxAmount < 0)
                {
                    NotifyError("Error.TaxAmountCannotBeNegative");
                    return RedirectToAction(nameof(Details), new { id = model.Id });
                }
                if (model.ShippingAmount < 0)
                {
                    NotifyError("Error.ShippingAmountCannotBeNegative");
                    return RedirectToAction(nameof(Details), new { id = model.Id });
                }
            }
        }

        var updatePurchaseOrderResult = await mediator.Send(new UpdatePurchaseOrderCommand
        {
            PurchaseOrderId = model.Id,
            PlacedOn = model.PlacedOn,
            ShippingAmount = model.ShippingAmount ?? 0,
            TaxAmount = model.TaxAmount ?? 0,
            VendorId = model.VendorId,
            WarehouseId = model.WarehouseId,
            ExpectedDeliveryDate = model.ExpectedDeliveryDate,
            Note = model.Note
        });

        if (!updatePurchaseOrderResult.Success)
        {
            NotifyError(updatePurchaseOrderResult.ErrorMessage!);
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [Authorize(Policy = SystemPermissions.PurchaseOrders.View)]
    public async Task<IActionResult> Details(Guid id)
    {
        var model = await purchaseOrderModelFactory.PreparePurchaseOrderDetailsModel(id);
        if (model == null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = SystemPermissions.Debts.VendorDebtsRecordPayment)]
    public async Task<IActionResult> RecordSettlementPayment(RecordPurchaseOrderSettlementPaymentCommand command)
    {
        var result = await mediator.Send(command);
        if (result.Success)
            NotifySuccess("Msg.SaveSuccess");
        else
            NotifyError(result.ErrorMessage ?? "Error.InvalidRequest");

        return RedirectToAction(nameof(Details), new { id = command.PurchaseOrderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> Copy(Guid id)
    {
        var purchaseOrder = await purchaseOrderAppService.GetPurchaseOrderByIdAsync(id);
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var vendor = await mediator.Send(new GetVendorQuery { Id = purchaseOrder.VendorId });
        if (vendor is null)
        {
            NotifyError("Error.VendorIsNotFound");
            return RedirectToAction(nameof(Details), new { id });
        }

        if (purchaseOrder.Items.Count == 0)
        {
            NotifyError("Error.PurchaseOrderItemRequired");
            return RedirectToAction(nameof(Details), new { id });
        }

        var productInfos = await mediator.Send(new GetProductsByIdsForOrderQuery
        {
            Ids = purchaseOrder.Items.Select(i => i.ProductId).Distinct().ToList()
        });

        if (purchaseOrder.Items.Any(item => !productInfos.Any(p => p.Id == item.ProductId)))
        {
            NotifyError("Error.ProductIsNotFound");
            return RedirectToAction(nameof(Details), new { id });
        }

        var candidateVendorIds = productInfos.SelectMany(p => p.AvailableVendors).Select(v => v.Id).Distinct().ToList();
        var validVendorIds = candidateVendorIds.Where(vendorId => productInfos.All(p => p.AvailableVendors.Any(v => v.Id == vendorId))).ToList();

        if (!validVendorIds.Contains(purchaseOrder.VendorId))
        {
            NotifyError("Error.PurchaseOrder.VendorIsNotAppropriate");
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await mediator.Send(new CopyPurchaseOrderCommand(id));
        if (result.Success && result.CreatedId.HasValue)
        {
            NotifySuccess("Msg.SaveSuccess");
            return RedirectToAction(nameof(Details), new { id = result.CreatedId.Value });
        }

        NotifyError(result.ErrorMessage ?? "Error.InvalidRequest");
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> SplitsPurchaseOrder(Guid id)
    {
        var purchaseOrder = await purchaseOrderAppService.GetPurchaseOrderByIdAsync(id);
        if (purchaseOrder is null)
            return this.JsonError(LocalizeError("Error.PurchaseOrderIsNotFound"));

        if (!purchaseOrder.CanAddItems)
            return this.JsonError(LocalizeError("Error.PurchaseOrderCannotUpdateOrderItems"));

        if (!purchaseOrder.Items.Any(i => i.QuantityOrdered - i.QuantityReceived > 0))
            return this.JsonError(LocalizeError("Error.PurchaseOrder.NoItemsForSplit"));

        var model = await purchaseOrderModelFactory.PrepareSplitsPurchaseOrderModel(id);
        if (model is null)
            return this.JsonError(LocalizeError("Error.PurchaseOrderIsNotFound"));

        return PartialView(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> SplitsPurchaseOrder(SplitsPurchaseOrderModel model)
    {
        var purchaseOrder = await purchaseOrderAppService.GetPurchaseOrderByIdAsync(model.PurchaseOrderId);
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var vendor = await mediator.Send(new GetVendorQuery { Id = purchaseOrder.VendorId });
        if (vendor is null)
        {
            NotifyError("Error.VendorIsNotFound");
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        if (model.Items.Count == 0 || !model.Items.Any(item => item.Quantity > 0))
        {
            NotifyError("Error.PurchaseOrderItemRequired");
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var selectedPurchaseOrderItems = purchaseOrder.Items.Where(item => model.Items.Any(selectedItem => selectedItem.ItemId == item.Id)).ToList();
        var productInfos = await mediator.Send(new GetProductsByIdsForOrderQuery
        {
            Ids = selectedPurchaseOrderItems.Select(i => i.ProductId).Distinct().ToList()
        });

        if (selectedPurchaseOrderItems.Any(item => !productInfos.Any(p => p.Id == item.ProductId)))
        {
            NotifyError("Error.ProductIsNotFound");
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var candidateVendorIds = productInfos.SelectMany(p => p.AvailableVendors).Select(v => v.Id).Distinct().ToList();
        var validVendorIds = candidateVendorIds.Where(vendorId => productInfos.All(p => p.AvailableVendors.Any(v => v.Id == vendorId))).ToList();

        if (!validVendorIds.Contains(purchaseOrder.VendorId))
        {
            NotifyError("Error.PurchaseOrder.VendorIsNotAppropriate");
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var result = await mediator.Send(new SplitPurchaseOrderCommand
        {
            PurchaseOrderId = model.PurchaseOrderId,
            Items = model.Items.Where(item => item.Quantity > 0).Select(item => new SplitPurchaseOrderCommand.SplitItemCommand
            {
                ItemId = item.ItemId,
                Quantity = item.Quantity
            }).ToList()
        });
        if (result.Success)
        {
            NotifySuccess("Msg.SaveSuccess");
            NotifyInfo("Msg.PurchaseOrder.YouAreWatchingNewPurchaseOrder");
            return RedirectToAction(nameof(Details), new { id = result.CreatedId });
        }

        NotifyError(LocalizeError(result.ErrorMessage ?? "Error.InvalidRequest"));
        return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> AddPurchaseOrderItem(AddPurchaseOrderItemModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var purchaseOrder = await mediator.Send(new GetPurchaseOrderQuery { Id = model.PurchaseOrderId });
        if (purchaseOrder is null)
            return Json(new { success = false, message = LocalizeError("Error.PurchaseOrderIsNotFound") });

        var result = await mediator.Send(new AddPurchaseOrderItemCommand
        {
            PurchaseOrderId = model.PurchaseOrderId,
            ProductId = model.ProductId ?? default,
            Quantity = model.Quantity ?? 0,
            UnitCost = model.UnitCost ?? 0,
            Note = model.Note,
            QuantityDecimalPlaces = model.QuantityDecimalPlaces
        });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
        return Json(new { success = true, message = string.Empty });
    }

    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> ReceivesItem(Guid id, Guid itemId)
    {
        var model = await purchaseOrderModelFactory.PreparePurchaseOrderSingleReceiveModel(id, itemId);
        if (model is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        return PartialView(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> ReceivesItem(PurchaseOrderSingleReceiveItemsModel model)
    {
        if (!ModelState.IsValid)
        {
            NotifyError("Error.InvalidRequest", GetErrorMessage());
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var purchaseOrder = await purchaseOrderAppService.GetPurchaseOrderByIdAsync(model.PurchaseOrderId);
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        if (model.ReceivedOn.HasValue && DateTimeHelper.ToUniversalTime(model.ReceivedOn.Value) < purchaseOrder.PlacedOnUtc)
        {
            NotifyError("Error.ReceivedOnMustBeAfterPlacedOn");
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var result = await mediator.Send(new ReceivePurchaseOrderItemCommand
        {
            PurchaseOrderId = model.PurchaseOrderId,
            ShippingAmount = model.AdditionalShipping ?? 0,
            TaxRate = model.TaxRate,
            ReceivedOn = model.ReceivedOn,
            PictureIds = model.PictureIds,
            PurchaseOrderItemId = model.PurchaseOrderItemId,
            ReceivedQuantity = model.Quantity,
            WarehouseId = model.WarehouseId,
            ActualUnitCost = model.ActualUnitCost,
            DirectShipOrderId = model.DirectShipOrderId,
            DirectShipOrderItemId = model.DirectShipOrderItemId,
            DirectShipAddress = model.DirectShipAddress,
            DirectShipContactName = model.DirectShipContactName,
            DirectShipContactPhone = model.DirectShipContactPhone,
            DirectShipExistingAllocationId = model.DirectShipExistingAllocationId,
            QuantityDecimalPlaces = model.QuantityDecimalPlaces,
            SellingPrice = model.SellingPrice.HasValue && model.SellingPrice > 0 ? model.SellingPrice : null
        });

        if (!result.Success)
        {
            NotifyError(result.ErrorMessage!);
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
    }

    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> UpdatePurchaseOrderItem(Guid id, Guid itemId)
    {
        var purchaseOrder = await mediator.Send(new GetPurchaseOrderQuery { Id = id });
        if (purchaseOrder is null)
            return this.JsonError(Localizer["Error.PurchaseOrderIsNotFound"]);
        if (!purchaseOrder.CanAddItems)
            return this.JsonError(Localizer["Error.PurchaseOrderCannotAddItems"]);

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == itemId);
        if (purchaseOrderItem is null)
            return this.JsonError(Localizer["Error.PurchaseOrderItemIsNotFound"]);

        var model = new EditPurchaseOrderItemModel
        {
            PurchaseOrderId = purchaseOrder.Id,
            PurchaseOrderItemId = purchaseOrderItem.Id,
            Quantity = purchaseOrderItem.QuantityOrdered,
            UnitCost = purchaseOrderItem.UnitCost,
            Note = purchaseOrderItem.Note,
            ProductName = purchaseOrderItem.ProductName,
            QuantityDecimalPlaces = purchaseOrderItem.QuantityDecimalPlaces
        };

        return PartialView(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> UpdatePurchaseOrderItem(EditPurchaseOrderItemModel model)
    {
        if (!ModelState.IsValid)
        {
            NotifyError("Error.InvalidRequest", GetErrorMessage());
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var purchaseOrder = await mediator.Send(new GetPurchaseOrderQuery { Id = model.PurchaseOrderId });
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == model.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
        {
            NotifyError("Error.PurchaseOrderItemIsNotFound");
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        if (!purchaseOrder.CanAddItems)
        {
            NotifyError("Error.PurchaseOrderCannotUpdateOrderItems");
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var result = await mediator.Send(new UpdatePurchaseOrderItemCommand
        {
            PurchaseOrderId = model.PurchaseOrderId,
            PurchaseOrderItemId = model.PurchaseOrderItemId,
            Quantity = model.Quantity ?? 0,
            UnitCost = model.UnitCost ?? 0,
            Note = model.Note
        });

        if (!result.Success)
        {
            NotifyError(result.ErrorMessage!);
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
    }

    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> BulkReceiveItems(Guid id)
    {
        if (!ModelState.IsValid)
            return this.JsonError(Localizer["Error.InvalidRequest", GetErrorMessage()]);

        var purchaseOrder = await mediator.Send(new GetPurchaseOrderQuery { Id = id });
        if (purchaseOrder is null)
            return this.JsonError(Localizer["Error.PurchaseOrderIsNotFound"]);

        if (!purchaseOrder.CanReceiveGoods)
            return this.JsonError(Localizer["Error.PurchaseOrderCannotReceiveGoods"]);

        var model = await purchaseOrderModelFactory.PreparePurchaseOrderBulkReceiveModel(id);
        if (model is null)
            return this.JsonError(Localizer["Error.PurchaseOrderIsNotFound"]);

        return PartialView(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> BulkReceiveItems(PurchaseOrderBulkReceiveItemsModel model)
    {
        if (!ModelState.IsValid)
        {
            NotifyError("Error.InvalidRequest", GetErrorMessage());
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var purchaseOrder = await purchaseOrderAppService.GetPurchaseOrderByIdAsync(model.PurchaseOrderId);
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        if (model.ReceivedOn.HasValue && DateTimeHelper.ToUniversalTime(model.ReceivedOn.Value) < purchaseOrder.PlacedOnUtc)
        {
            NotifyError("Error.ReceivedOnMustBeAfterPlacedOn");
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var items = model.Items.Where(line => line.ItemId != Guid.Empty && line.Quantity > 0).ToList();
        if (items.Count == 0)
        {
            NotifyError("Error.BulkReceive.NoItemsProvided");
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var result = await mediator.Send(new BulkReceivePurchaseOrderCommand
        {
            PurchaseOrderId = model.PurchaseOrderId,
            ShippingAmount = model.AdditionalShipping ?? 0,
            TaxRate = model.TaxRate,
            ReceivedOn = model.ReceivedOn,
            PictureIds = model.PictureIds,
            Items = items.Select(line => new BulkReceiveLineCommand
            {
                ItemId = line.ItemId,
                Quantity = line.Quantity,
                WarehouseId = line.WarehouseId,
                ActualUnitCost = line.ActualUnitCost,
                DirectShipOrderId = line.DirectShipOrderId,
                DirectShipOrderItemId = line.DirectShipOrderItemId,
                DirectShipAddress = line.DirectShipAddress,
                DirectShipContactName = line.DirectShipContactName,
                DirectShipContactPhone = line.DirectShipContactPhone,
                DirectShipExistingAllocationId = line.DirectShipExistingAllocationId
            }).ToList()
        });

        if (!result.Success)
        {
            NotifyError(result.ErrorMessage!);
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var count = result.CreatedGoodsReceiptIds.Count;
        if (count > 1)
            NotifySuccess("Msg.BulkReceive.CreatedMultiple", count);
        else
            NotifySuccess("Msg.SaveSuccess");

        return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> RemovePurchaseOrderItem([FromBody] DeletePurchaseOrderItemModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, errorMessage = LocalizeError("Error.InvalidRequest", GetErrorMessage()) });

        var purchaseOrder = await mediator.Send(new GetPurchaseOrderQuery { Id = model.PurchaseOrderId });
        if (purchaseOrder is null)
            return Json(new { success = false, errorMessage = LocalizeError("Error.PurchaseOrderIsNotFound") });

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == model.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
            return Json(new { success = false, errorMessage = LocalizeError("Error.PurchaseOrderItemIsNotFound") });

        if (!purchaseOrder.CanAddItems)
            return Json(new { success = false, errorMessage = LocalizeError("Error.PurchaseOrderCannotUpdateOrderItems") });

        var result = await mediator.Send(new DeletePurchaseOrderItemCommand
        {
            PurchaseOrderId = model.PurchaseOrderId,
            ItemId = model.PurchaseOrderItemId
        });

        if (!result.Success)
            return Json(new { success = false, errorMessage = LocalizeError(result.ErrorMessage!) });
        return Json(new { success = true });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> SubmitsPurchaseOrder(Guid id)
    {
        var purchaseOrder = await mediator.Send(new GetPurchaseOrderQuery { Id = id });
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var (success, errorMessage) = await mediator.Send(new SubmitsPurchaseOrderCommand
        {
            PurchaseOrderId = id
        });

        if (success)
            NotificationService.Success(errorMessage ?? LocalizeError("Msg.SaveSuccess"));
        else
            NotifyError(errorMessage!);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Approve)]
    public async Task<IActionResult> ApprovesPurchaseOrder(Guid id)
    {
        var purchaseOrder = await mediator.Send(new GetPurchaseOrderQuery { Id = id });
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var (success, errorMessage) = await mediator.Send(new ApprovesPurchaseOrderCommand
        {
            PurchaseOrderId = id
        });

        if (success)
            NotificationService.Success(errorMessage ?? LocalizeError("Msg.SaveSuccess"));
        else
            NotifyError(errorMessage!);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Cancel)]
    public async Task<IActionResult> CancelPurchaseOrder(Guid id)
    {
        var purchaseOrder = await mediator.Send(new GetPurchaseOrderQuery { Id = id });
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var (success, errorMessage) = await mediator.Send(new CancelPurchaseOrderCommand
        {
            PurchaseOrderId = id
        });

        if (success)
            NotificationService.Success(errorMessage ?? LocalizeError("Msg.SaveSuccess"));
        else
            NotifyError(errorMessage!);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> ClosePartial(Guid id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            NotifyError("Error.PurchaseOrder.CloseReasonRequired");
            return RedirectToAction(nameof(Details), new { id });
        }

        var (success, errorMessage) = await mediator.Send(new ClosePartialPurchaseOrderCommand
        {
            PurchaseOrderId = id,
            Reason = reason
        });

        if (success)
            NotifySuccess("Msg.SaveSuccess");
        else
            NotifyError(errorMessage!);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.View)]
    public async Task<IActionResult> RecentPurchasePrices(Guid productId)
    {
        if (productId == Guid.Empty)
            return Json(Array.Empty<object>());

        var prices = await mediator.Send(new GetRecentPurchasePricesQuery { ProductId = productId });

        var result = prices.Select(p => new
        {
            vendorId = p.VendorId,
            vendorName = p.VendorName,
            unitCost = p.UnitCost,
            purchaseOrderCode = p.PurchaseOrderCode,
            purchaseDate = p.PurchaseDate.ToString("dd/MM/yyyy")
        });

        return Json(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> ChangeStatus(Guid id, int status)
    {
        if ((PurchaseOrderStatus)status == PurchaseOrderStatus.Approved)
        {
            NotifyError("Error.PurchaseOrder.ApproveRequiresPermission");
            return RedirectToAction(nameof(Details), new { id });
        }

        var purchaseOrder = await mediator.Send(new GetPurchaseOrderQuery { Id = id });
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var result = await mediator.Send(new ChangePurchaseOrderStatusCommand
        {
            PurchaseOrderId = id,
            Status = status
        });

        if (!result.Success)
            NotifyError(result.ErrorMessage!);
        else
            NotifySuccess("Msg.SaveSuccess");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.View)]
    public async Task<IActionResult> EligibleOrderItems(Guid purchaseOrderId, Guid purchaseOrderItemId)
    {
        var purchaseOrder = await purchaseOrderAppService.GetPurchaseOrderByIdAsync(purchaseOrderId);
        if (purchaseOrder is null)
            return this.JsonError(LocalizeError("Error.PurchaseOrderIsNotFound"));

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == purchaseOrderItemId);
        if (purchaseOrderItem is null)
            return this.JsonError(LocalizeError("Error.PurchaseOrderItemIsNotFound"));

        var orderItems = await purchaseOrderAppService.GetEligibleOrderItemsForPoItemAsync((purchaseOrderId, purchaseOrderItemId));
        return this.JsonOk(orderItems.Select(item => new
        {
            orderItemId = item.OrderItemId,
            orderId = item.OrderId,
            orderCode = item.OrderCode,
            customerName = item.CustomerName,
            customerPhone = item.CustomerPhone,
            shippingContactName = item.ShippingContactName,
            shippingAddress = item.ShippingAddress,
            shippingPhoneNumber = item.ShippingPhoneNumber,
            productName = item.ProductName,
            totalQuantity = item.TotalQuantity,
            allocatedOutstanding = item.AllocatedOutstanding,
            availableToAllocate = item.AvailableToAllocate
        }));
    }

    [HttpGet]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.View)]
    public async Task<IActionResult> NonDirectShipAllocations(Guid purchaseOrderId, Guid purchaseOrderItemId)
    {
        var purchaseOrder = await purchaseOrderAppService.GetPurchaseOrderByIdAsync(purchaseOrderId);
        if (purchaseOrder is null)
            return this.JsonError(LocalizeError("Error.PurchaseOrderIsNotFound"));

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == purchaseOrderItemId);
        if (purchaseOrderItem is null)
            return this.JsonError(LocalizeError("Error.PurchaseOrderItemIsNotFound"));

        var allocations = await purchaseOrderAppService.GetNonDirectShipAllocationsForPoItemAsync((purchaseOrderId, purchaseOrderItemId));
        return this.JsonOk(allocations.Select(a => new
        {
            allocationId = a.AllocationId,
            orderId = a.OrderId,
            orderItemId = a.OrderItemId,
            orderCode = a.OrderCode,
            customerName = a.CustomerName,
            customerPhone = a.CustomerPhone,
            shippingContactName = a.ShippingContactName,
            shippingAddress = a.ShippingAddress,
            shippingPhoneNumber = a.ShippingPhoneNumber,
            allocatedQty = a.AllocatedQuantity,
            remainingQty = a.RemainingQuantity
        }));
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> AllocateToOrder([FromBody] AllocateToOrderRequestModel request)
    {
        if (request is null || request.PurchaseOrderItemId == Guid.Empty || request.OrderItemId == Guid.Empty || request.Quantity <= 0)
            return Json(new { success = false, message = LocalizeError("Error.InvalidRequest") });

        var result = await mediator.Send(new AllocatePoItemForOrderItemCommand
        {
            PurchaseOrderId = request.PurchaseOrderId,
            PurchaseOrderItemId = request.PurchaseOrderItemId,
            OrderId = request.OrderId,
            OrderItemId = request.OrderItemId,
            Quantity = request.Quantity,
            DirectShipAddress = request.DirectShipAddress,
            DirectShipContactName = request.DirectShipContactName,
            DirectShipContactPhone = request.DirectShipContactPhone
        });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> ReleaseAllocationsForPurchaseOrderItem([FromBody] ReleaseAllocationsOfPurchaseOrderItemCommand command)
    {
        var result = await mediator.Send(command);
        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> MarkAllocationAsDirectShip([FromBody] MarkAllocationAsDirectShipCommand command)
    {
        if (command is null
            || command.AllocationId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.Address)
            || string.IsNullOrWhiteSpace(command.ContactPhone))
        {
            return Json(new { success = false, message = LocalizeError("Error.InvalidRequest") });
        }

        var result = await mediator.Send(command);
        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") });
    }
}
