using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.DeliveryNotes;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class CreateDeliveryNoteValidator : AbstractValidator<CreateDeliveryNoteModel>
{
    public CreateDeliveryNoteValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage(x => localizer["Error.Invalid", localizer["Label.Code"]]);

        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage(x => localizer["Error.Required", localizer["Label.Warehouse"]]);

        RuleFor(x => x.ShippingAddress)
            .NotEmpty().WithMessage(x => localizer["Error.Required", localizer["Label.Address"]])
            .MaximumLength(500).WithMessage(x => localizer["Error.MaxLength", localizer["Label.Address"], 500]);

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage(x => localizer["Error.MaxLength", localizer["Label.Note"], 1000]);

        RuleFor(x => x.Surcharge)
            .GreaterThanOrEqualTo(0).WithMessage(x => localizer["Error.Invalid", localizer["Label.Surcharge"]]);

        RuleFor(x => x.AmountToCollect)
            .GreaterThanOrEqualTo(0).WithMessage(x => localizer["Error.Invalid", localizer["Label.Amount"]]);

        RuleForEach(x => x.Items)
            .SetValidator(new CreateDeliveryNoteItemValidator(localizer));
    }
}

public sealed class CreateDeliveryNoteItemValidator : AbstractValidator<CreateDeliveryNoteItemModel>
{
    public CreateDeliveryNoteItemValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).When(x => x.Selected)
            .WithMessage(x => localizer["Error.Invalid", localizer["Label.Quantity"]]);
    }
}
