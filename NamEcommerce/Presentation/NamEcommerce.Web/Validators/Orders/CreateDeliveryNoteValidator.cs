using FluentValidation;
using NamEcommerce.Web.Models.DeliveryNotes;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class CreateDeliveryNoteValidator : AbstractValidator<CreateDeliveryNoteModel>
{
    public CreateDeliveryNoteValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId là bắt buộc.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("Vui lòng chọn kho xuất hàng.");

        RuleFor(x => x.ShippingAddress)
            .NotEmpty().WithMessage("Địa chỉ giao hàng là bắt buộc.")
            .MaximumLength(500).WithMessage("Địa chỉ giao hàng không được vượt quá 500 ký tự.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Ghi chú không được vượt quá 1000 ký tự.");

        RuleFor(x => x.Surcharge)
            .GreaterThanOrEqualTo(0).WithMessage("Phụ phí không được âm.");

        RuleFor(x => x.AmountToCollect)
            .GreaterThanOrEqualTo(0).WithMessage("Số tiền phải thu không được âm.");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateDeliveryNoteItemValidator());
    }
}

public sealed class CreateDeliveryNoteItemValidator : AbstractValidator<CreateDeliveryNoteItemModel>
{
    public CreateDeliveryNoteItemValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).When(x => x.Selected)
            .WithMessage("Số lượng xuất phải lớn hơn 0.");
    }
}
