using FluentValidation;
using NamEcommerce.Web.Models.Orders;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class DeleteOrderItemValidator : AbstractValidator<DeleteOrderItemModel>
{
    public DeleteOrderItemValidator()
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage("Không tìm thấy order ID.");

        RuleFor(m => m.ItemId)
            .NotEmpty().WithMessage("Không tìm thấy order item ID.");
    }
}
