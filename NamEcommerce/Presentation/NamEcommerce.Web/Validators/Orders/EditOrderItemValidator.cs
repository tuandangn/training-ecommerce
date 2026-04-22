using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class EditOrderItemValidator : AbstractValidator<EditOrderItemModel>
{
    public EditOrderItemValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage(m => localizer["Order.Id.NotFound"]);

        RuleFor(m => m.ItemId)
            .NotEmpty().WithMessage(m => localizer["Order.ItemId.NotFound"]);
    }
}
