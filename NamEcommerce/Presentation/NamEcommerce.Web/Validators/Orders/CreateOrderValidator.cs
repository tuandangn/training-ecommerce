using FluentValidation;
using NamEcommerce.Web.Models.Orders;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderModel>
{
    public CreateOrderValidator()
    {
        RuleFor(p => p.CustomerId)
            .NotEmpty().WithMessage("Vui lòng chọn khách hàng.");

        RuleFor(p => p.OrderDiscount)
            .GreaterThanOrEqualTo(0).WithMessage("Giảm giá phải lớn hơn hoặc bằng 0.");

        RuleFor(p => p.ExpectedShippingDate)
            .GreaterThanOrEqualTo(DateTime.Now.Date).WithMessage("Ngày giao dự kiến phải lớn hơn hiện tại.");

        RuleFor(p => p.ShippingAddress)
            .NotEmpty().WithMessage("Vui lòng nhập địa chỉ giao hàng.")
            .MaximumLength(1000).WithMessage("Địa chỉ giao hàng tối đa 1000 ký tự.");

        RuleFor(p => p.Items)
            .NotEmpty().WithMessage("Vui lòng nhập hàng hóa.");
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
