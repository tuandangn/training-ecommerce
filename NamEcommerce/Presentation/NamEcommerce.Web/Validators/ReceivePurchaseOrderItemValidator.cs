using FluentValidation;
using NamEcommerce.Web.Models.PurchaseOrders;

namespace NamEcommerce.Web.Validators;

public sealed class ReceivePurchaseOrderItemValidator : AbstractValidator<ReceivePurchaseOrderItemModel>
{
    public ReceivePurchaseOrderItemValidator()
    {
        RuleFor(m => m.PurchaseOrderId).NotEmpty().WithMessage("Không tìm thấy đơn hàng nhập.");
        RuleFor(m => m.PurchaseOrderItemId).NotEmpty().WithMessage("Không tìm thấy hàng hóa cần nhập.");
        RuleFor(m => m.ReceivedQuantity).GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
    }
}
