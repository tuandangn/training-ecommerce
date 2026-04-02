using FluentValidation;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Validators;

public sealed class CreatePurchaseOrderValidator : AbstractValidator<CreatePurchaseOrderModel>
{
    public CreatePurchaseOrderValidator()
    {
        RuleFor(p => p.VendorId).NotEmpty().WithMessage("Vui lòng chọn nhà cung cấp.");
        RuleFor(p => p.ExpectedDeliveryDate).GreaterThanOrEqualTo(DateTime.Now).WithMessage("Ngày dự kiến phải lớn hơn hiện tại.");
    }
}
