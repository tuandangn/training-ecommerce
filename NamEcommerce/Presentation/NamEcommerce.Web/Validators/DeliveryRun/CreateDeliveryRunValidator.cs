using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.DeliveryRun;

public sealed class CreateDeliveryRunValidator : AbstractValidator<CreateDeliveryRunModel>
{
    public CreateDeliveryRunValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.AssignedDeliveryUserId)
            .NotEmpty().WithMessage(m => localizer["Error.DeliveryUserRequired"]);

        RuleFor(m => m.DeliveryNoteIds)
            .NotEmpty().WithMessage(m => localizer["Error.DeliveryRunItemsRequired"]);
    }
}
