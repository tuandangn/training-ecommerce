using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class AddOrderItemValidator : AbstractValidator<AddOrderItemModel>
{
    public AddOrderItemValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage(m => localizer["Error.Invalid", localizer["Label.Code"]]);

        RuleFor(m => m.ProductId)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Product"]]);

        RuleFor(m => m.Quantity)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Quantity"]])
            .GreaterThan(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.Quantity"]]);

        RuleFor(m => m.UnitPrice)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.UnitPrice"]])
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.UnitPrice"]]);
    }
}
