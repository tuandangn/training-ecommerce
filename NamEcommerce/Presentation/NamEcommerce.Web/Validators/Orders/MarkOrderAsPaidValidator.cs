using FluentValidation;
using NamEcommerce.Web.Models.Orders;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class MarkOrderAsPaidValidator : AbstractValidator<MarkOrderAsPaidModel>
{
    public MarkOrderAsPaidValidator()
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage("Không tìm thấy ID đơn hàng.");

        RuleFor(m => m.PaymentMethod)
            .NotEmpty().WithMessage("Vui lòng chọn phương thức thanh toán");

        RuleFor(m => m.Note)
            .MaximumLength(1000).WithMessage("Ghi chú có độ dài không quá 1000 ký tự.");
    }
}
