using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.DeliveryNotes;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.DeliveryNotes;

public sealed class MarkDeliveryNoteAsDeliveredValidator : AbstractValidator<MarkDeliveryNoteAsDeliveredModel>
{
    public MarkDeliveryNoteAsDeliveredValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(p => p.DeliveryNoteId)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Id"]]);

        RuleFor(p => p.CashCollectedAmount)
            .GreaterThanOrEqualTo(0).WithMessage(p => localizer["Error.CashCollectedAmountCannotBeNegative"]);

        RuleFor(p => p.AgreedCustomerCharge)
            .GreaterThanOrEqualTo(0).WithMessage(p => localizer["Error.AgreedCustomerChargeMustBePositive"]);
    }
}
