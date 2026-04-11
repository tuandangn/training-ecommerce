using FluentValidation;
using NamEcommerce.Web.Models.Inventory;

namespace NamEcommerce.Web.Validators;

public sealed class EditWarehouseValidator : AbstractValidator<EditWarehouseModel>
{
    public EditWarehouseValidator()
    {
        RuleFor(m => m.Code)
            .NotEmpty().WithMessage("Vui lòng nhập mã kho hàng")
            .MaximumLength(50).WithMessage("Độ dài mã kho hàng phải nhỏ hơn 50 ký tự");
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage("Vui lòng nhập tên kho hàng")
            .MaximumLength(200).WithMessage("Độ dài tên kho hàng phải nhỏ hơn 200 ký tự");

        RuleFor(m => m.PhoneNumber)
            .Matches(@"0\d{9,10}").WithMessage("Số điện thoại không đúng");

        RuleFor(m => m.Address)
            .MaximumLength(800).WithMessage("Độ dài địa chỉ phải nhỏ hơn 800 ký tự");
    }
}
