using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class DeleteOrderItemValidator : AbstractValidator<DeleteOrderItemModel>
{
    public DeleteOrderItemValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage(m => localizer["Error.Invalid", localizer["Label.Code"]]);

        RuleFor(m => m.ItemId)
            .NotEmpty().WithMessage(m => localizer["Error.Invalid", localizer["Label.Product"]]);
    }
}
