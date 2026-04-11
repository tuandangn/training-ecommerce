using FluentValidation;
using NamEcommerce.Web.Models.PurchaseOrders;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class EditPurchaseOrderValidator : AbstractValidator<EditPurchaseOrderModel>
{
    public EditPurchaseOrderValidator()
    {
        RuleFor(p => p.VendorId).NotEmpty().WithMessage("Vui lòng chọn nhà cung cấp.");
        RuleFor(p => p.ExpectedDeliveryDate).GreaterThanOrEqualTo(DateTime.Now).WithMessage("Ngày dự kiến phải lớn hơn hiện tại.");
        RuleFor(p => p.ShippingAmount).GreaterThanOrEqualTo(0).WithMessage("Phí vận chuyển phải lớn hơn hoặc bằng 0.");
        RuleFor(p => p.TaxAmount).GreaterThanOrEqualTo(0).WithMessage("Tổng thuế phải lớn hơn hoặc bằng 0.");
    }
}
