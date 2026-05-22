using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class CompleteOrderValidator : AbstractValidator<CompleteOrderModel>
{
    public CompleteOrderValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage(m => localizer["Error.Invalid", localizer["Label.Code"]]);
    }
}
