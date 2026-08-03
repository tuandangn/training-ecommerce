using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class OrderQuickCreateCompleteValidator : AbstractValidator<OrderQuickCreateCompleteModel>
{
    public OrderQuickCreateCompleteValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(p => p.OrderId)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Order"]]);
        RuleFor(p => p.PaidAmount)
            .GreaterThan(0).WithMessage(p => localizer["Error.Invalid", localizer["Label.PaidAmount"]]);
    }
}
