using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Returns;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Returns;

public sealed class CreateCustomerReturnValidator : AbstractValidator<CreateCustomerReturnModel>
{
    public CreateCustomerReturnValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.CustomerId).NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Customer"]]);
    }
}
public sealed class CreateCustomerReturnItemValidator : AbstractValidator<CreateCustomerReturnItemModel>
{
    public CreateCustomerReturnItemValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.ProductId).NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Product"]]);
    }
}
