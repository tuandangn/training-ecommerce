using FluentValidation;
using NamEcommerce.Web.Models.Orders;

namespace NamEcommerce.Web.Validators;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderModel>
{
    public CreateOrderValidator()
    {
        RuleFor(p => p.CustomerId)
            .NotEmpty().WithMessage("Vui lòng chọn khách hàng.");

        RuleFor(p => p.OrderDiscount)
            .GreaterThanOrEqualTo(0).WithMessage("Giảm giá phải lớn hơn hoặc bằng 0.");

        RuleFor(p => p.ExpectedShippingDate)
            .NotEmpty().WithMessage("Vui lòng nhập ngày giao dự kiến.")
            .GreaterThanOrEqualTo(DateTime.Now.Date).WithMessage("Ngày giao dự kiến phải lớn hơn hiện tại.");
    }
}
public sealed class CreateOrderItemValidator : AbstractValidator<CreateOrderItemModel>
{
    public CreateOrderItemValidator()
    {
        RuleFor(p => p.ProductId)
            .NotEmpty().WithMessage("Vui lòng chọn hàng hóa.");

        RuleFor(p => p.Quantity)
            .NotEmpty().WithMessage("Vui lòng nhập số lượng.")
            .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");

        RuleFor(p => p.UnitPrice)
            .NotEmpty().WithMessage("Vui lòng nhập đơn giá.")
            .GreaterThanOrEqualTo(0).WithMessage("Đơn giá phải lớn hơn hoặc bằng 0.");
    }
}
