using FluentValidation;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Validators.Catalog;

public sealed class EditUnitMeasurementValidator : AbstractValidator<EditUnitMeasurementModel>
{
    public EditUnitMeasurementValidator()
    {
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage("Vui lòng nhập tên đơn vị")
            .MaximumLength(200).WithMessage("Độ dài tên đơn vị phải nhỏ hơn 200 ký tự");
    }
}
