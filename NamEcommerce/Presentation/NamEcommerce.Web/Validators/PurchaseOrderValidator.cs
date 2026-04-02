using FluentValidation;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Validators;

public sealed class PurchaseOrderValidator : AbstractValidator<PurchaseOrderModel>
{
    public PurchaseOrderValidator()
    {
        
    }
}
