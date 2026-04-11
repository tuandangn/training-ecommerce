using FluentValidation;
using NamEcommerce.Web.Models.Orders;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class EditOrderDiscountValidator : AbstractValidator<EditOrderDiscountModel>
{
    public EditOrderDiscountValidator()
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage("Không tìm thấy ID đơn hàng.");

        RuleFor(m => m.OrderDiscount)
            .GreaterThanOrEqualTo(0).WithMessage("Giảm giá phải lớn hơn hoặc bằng 0.");
    }
}
