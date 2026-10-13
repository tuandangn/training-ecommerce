using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class CreateOrderExpenseValidator : AbstractValidator<CreateOrderExpenseModel>
{
    public CreateOrderExpenseValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage(m => localizer["Error.OrderIdIsRequired"]);

        RuleFor(m => m.Title)
            .NotEmpty().WithMessage(m => localizer["Error.ExpenseTitleRequired"])
            .MaximumLength(255).WithMessage(m => localizer["Error.ExpenseTitleTooLong"]);

        RuleFor(m => m.IncurredDate)
            .LessThanOrEqualTo(DateTime.Today).WithMessage(m => localizer["Error.ExpenseIncurredDateCannotBeInFuture"]);

        RuleFor(m => m.AmountWithoutTax)
            .GreaterThan(0).WithMessage(m => localizer["Error.ExpenseAmountMustBePositive"]);

        RuleFor(m => m.TaxRate)
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["Error.ExpenseTaxRateInvalid"]);

        RuleFor(m => m.Description)
            .MaximumLength(500).WithMessage(m => localizer["Error.ExpenseDescriptionTooLong"]);
    }
}
