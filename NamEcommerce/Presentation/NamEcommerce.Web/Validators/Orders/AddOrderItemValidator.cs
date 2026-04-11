using FluentValidation;
using NamEcommerce.Web.Models.Orders;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class AddOrderItemValidator : AbstractValidator<AddOrderItemModel>
{
    public AddOrderItemValidator()
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage("Không tìm thấy ID đơn hàng.");

        RuleFor(m => m.ProductId)
            .NotEmpty().WithMessage("Không tìm thấy ID hàng hóa.");

        RuleFor(m => m.Quantity)
            .NotEmpty().WithMessage("Vui lòng nhập số lượng.")
            .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");

        RuleFor(m => m.UnitPrice)
            .NotEmpty().WithMessage("Vui lòng nhập đơn giá.")
            .GreaterThanOrEqualTo(0).WithMessage("Đơn giá phải lớn hơn hoặc bằng 0.");
    }
}
