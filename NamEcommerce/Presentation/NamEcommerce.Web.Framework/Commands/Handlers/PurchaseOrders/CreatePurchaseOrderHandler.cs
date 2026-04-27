using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Web.Framework.Services;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class CreatePurchaseOrderHandler : IRequestHandler<CreatePurchaseOrderCommand, CreatePurchaseOrderResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;
    private readonly ICurrentUserService _currentUserService;

    public CreatePurchaseOrderHandler(IPurchaseOrderAppService appService, ICurrentUserService currentUserService)
    {
        _purchaseOrderAppService = appService;
        _currentUserService = currentUserService;
    }

    public async Task<CreatePurchaseOrderResultModel> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var result = await _purchaseOrderAppService.CreatePurchaseOrderAsync(new CreatePurchaseOrderAppDto
        {
            PlacedOnUtc = DateTimeHelper.ToUniversalTime(request.PlacedOn),
            VendorId = request.VendorId,
            WarehouseId = request.WarehouseId,
            Note = request.Note,
            ExpectedDeliveryDateUtc = DateTimeHelper.ToUniversalTime(request.ExpectedDeliveryDate),
            ShippingAmount = request.ShippingAmount,
            TaxAmount = request.TaxAmount,
            CreatedByUserId = currentUser?.Id,
            Items = request.Items?.Select(i => new CreatePurchaseOrderItemAppDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            }).ToList() ?? []
        }).ConfigureAwait(false);

        return new CreatePurchaseOrderResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}
