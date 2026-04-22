using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class EditOrderItemValidator : AbstractValidator<EditOrderItemModel>
{
    public EditOrderItemValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage(m => localizer["Error.Invalid", localizer["Label.Code"]]);

        RuleFor(m => m.ItemId)
            .NotEmpty().WithMessage(m => localizer["Error.Invalid", localizer["Label.Product"]]);

        RuleFor(m => m.Quantity)
            .GreaterThan(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.Quantity"]]);

        RuleFor(m => m.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.UnitPrice"]]);
    }
}
