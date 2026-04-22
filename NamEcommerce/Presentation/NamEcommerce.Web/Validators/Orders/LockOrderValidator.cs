using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class LockOrderValidator : AbstractValidator<LockOrderModel>
{
    public LockOrderValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage(m => localizer["Order.Id.NotFound"]);

        RuleFor(m => m.Reason)
            .MaximumLength(1000).WithMessage(m => localizer["Order.Reason.MaxLength"]);
    }
}
