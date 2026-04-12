using FluentValidation;
using NamEcommerce.Web.Models.Orders;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class LockOrderValidator : AbstractValidator<LockOrderModel>
{
    public LockOrderValidator()
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage("Không tìm thấy ID đơn hàng.");

        RuleFor(m => m.Reason)
            .NotEmpty().WithMessage("Vui lòng nhập lý do hủy đơn.")
            .MaximumLength(1000).WithMessage("Lý do hủy đơn có độ dài không quá 1000 ký tự.");
    }
}
