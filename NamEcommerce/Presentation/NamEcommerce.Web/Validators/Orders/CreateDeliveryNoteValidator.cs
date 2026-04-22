using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.DeliveryNotes;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class CreateDeliveryNoteValidator : AbstractValidator<CreateDeliveryNoteModel>
{
    public CreateDeliveryNoteValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage(x => localizer["DeliveryNote.OrderId.Required"]);

        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage(x => localizer["DeliveryNote.WarehouseId.Required"]);

        RuleFor(x => x.ShippingAddress)
            .NotEmpty().WithMessage(x => localizer["DeliveryNote.ShippingAddress.Required"])
            .MaximumLength(500).WithMessage(x => localizer["DeliveryNote.ShippingAddress.MaxLength"]);

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage(x => localizer["DeliveryNote.Note.MaxLength"]);

        RuleFor(x => x.Surcharge)
            .GreaterThanOrEqualTo(0).WithMessage(x => localizer["DeliveryNote.Surcharge.Invalid"]);

        RuleFor(x => x.AmountToCollect)
            .GreaterThanOrEqualTo(0).WithMessage(x => localizer["DeliveryNote.AmountToCollect.Invalid"]);

        RuleForEach(x => x.Items)
            .SetValidator(new CreateDeliveryNoteItemValidator(localizer));
    }
}

public sealed class CreateDeliveryNoteItemValidator : AbstractValidator<CreateDeliveryNoteItemModel>
{
    public CreateDeliveryNoteItemValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).When(x => x.Selected)
            .WithMessage(x => localizer["DeliveryNote.Item.Quantity.Invalid"]);
    }
}
