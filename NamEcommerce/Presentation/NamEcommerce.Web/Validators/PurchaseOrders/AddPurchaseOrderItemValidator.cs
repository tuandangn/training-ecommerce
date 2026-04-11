using FluentValidation;
using NamEcommerce.Web.Models.PurchaseOrders;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class AddPurchaseOrderItemValidator : AbstractValidator<AddPurchaseOrderItemModel>
{
    public AddPurchaseOrderItemValidator()
    {
        RuleFor(m => m.PurchaseOrderId).NotEmpty().WithMessage("Không tìm thấy đơn hàng nhập.");
        RuleFor(m => m.ProductId).NotEmpty().WithMessage("Vui lòng chọn hàng hóa.");
        RuleFor(m => m.UnitCost).GreaterThan(0).WithMessage("Đơn giá phải lớn hơn 0.");
        RuleFor(m => m.Quantity).GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
    }
}
