using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class EditOrderNoteValidator : AbstractValidator<EditOrderNoteModel>
{
    public EditOrderNoteValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage(m => localizer["Error.Invalid", localizer["Label.Code"]]);

        RuleFor(m => m.Note)
            .MaximumLength(1000).WithMessage(m => localizer["Error.MaxLength", localizer["Label.Note"], 1000]);
    }
}
