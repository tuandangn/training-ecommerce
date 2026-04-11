using FluentValidation;
using NamEcommerce.Web.Models.Orders;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class EditOrderItemValidator : AbstractValidator<EditOrderItemModel>
{
    public EditOrderItemValidator()
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage("Không tìm thấy ID đơn hàng.");

        RuleFor(m => m.ItemId)
            .NotEmpty().WithMessage("Không tìm thấy ID hàng hóa.");
    }
}
