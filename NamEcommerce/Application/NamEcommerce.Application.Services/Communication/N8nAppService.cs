using NamEcommerce.Application.Contracts.Communication;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using System.Text;
using System.Text.Json;

namespace NamEcommerce.Application.Services.Communication;

public sealed class N8nAppService(HttpClient httpClient, IDeliveryNoteAppService deliveryNoteAppService) : IN8nAppService
{
    const string PATH = "delivery-note";

    public async Task NotifyDeliveryNoteIsConfirmed(Guid id)
    {
        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            return;

        var infoJson = new
        {
            id = deliveryNote.Id,
            code = deliveryNote.Code,
            orderId = deliveryNote.OrderId,
            orderCode = deliveryNote.OrderCode,
            shippingAddress = deliveryNote.ShippingAddress,
            customerId = deliveryNote.CustomerId,
            customerName = deliveryNote.CustomerName,
            customerPhone = deliveryNote.CustomerPhone,
            customerAddress = deliveryNote.CustomerAddress,
            note = deliveryNote.Note,
            items = deliveryNote.Items.Select(i => new
            {
                productId = i.ProductId,
                productName = i.ProductName,
                quantity = i.Quantity
            }),
            amountToCollect = deliveryNote.AmountToCollect,
            surcharge = deliveryNote.Surcharge,
            surchargeReason = deliveryNote.SurchargeReason,
            warehouseId = deliveryNote.WarehouseId,
            warehouseName = deliveryNote.WarehouseName
        };
        var requestContent = new StringContent(JsonSerializer.Serialize(infoJson), Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync(PATH, requestContent).ConfigureAwait(false);
        }
        catch(Exception ex)
        {
            //*TODO* log error
        }
    }
}
