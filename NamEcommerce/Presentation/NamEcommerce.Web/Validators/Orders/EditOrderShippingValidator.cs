using FluentValidation;
using NamEcommerce.Web.Models.Orders;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class EditOrderShippingValidator : AbstractValidator<EditOrderShippingModel>
{
    public EditOrderShippingValidator()
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage("Không tìm thấy ID đơn hàng.");

        RuleFor(m => m.Address)
            .NotEmpty().WithMessage("Vui lòng nhập địa chỉ giao hàng.")
            .MaximumLength(1000).WithMessage("Địa chỉ giao hàng có độ dài không quá 1000 ký tự.");

        RuleFor(m => m.Note)
            .MaximumLength(1000).WithMessage("Ghi chú có độ dài không quá 1000 ký tự.");
    }
}
