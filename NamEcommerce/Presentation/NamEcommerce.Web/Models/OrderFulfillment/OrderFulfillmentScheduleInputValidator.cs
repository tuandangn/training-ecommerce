using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Models.OrderFulfillment;

public sealed class OrderFulfillmentScheduleInputValidator : AbstractValidator<OrderFulfillmentScheduleInputModel>
{
    private const int AsSoonAsPossible = 10;
    private const int NotBeforeDate = 20;
    private const int WhenStockAvailable = 30;

    public OrderFulfillmentScheduleInputValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(model => model.OrderId)
            .NotEmpty().WithMessage(model => localizer["Error.Invalid", localizer["Label.Code"]]);
        RuleFor(model => model.Mode)
            .Must(mode => mode is AsSoonAsPossible or NotBeforeDate or WhenStockAvailable)
            .WithMessage(model => localizer["Error.Invalid", localizer["Label.Status"]]);
        RuleFor(model => model.ScheduledFromUtc)
            .NotEmpty()
            .When(model => model.Mode == NotBeforeDate)
            .WithMessage(model => localizer["Error.Required", localizer["Label.Date"]]);
        RuleFor(model => model.ScheduledToUtc)
            .GreaterThanOrEqualTo(model => model.ScheduledFromUtc)
            .When(model => model.ScheduledFromUtc.HasValue && model.ScheduledToUtc.HasValue)
            .WithMessage(model => localizer["Error.Invalid", localizer["Label.Date"]]);
        RuleFor(model => model.Items)
            .NotEmpty().WithMessage(model => localizer["Error.Required", localizer["Label.Product"]]);
        RuleForEach(model => model.Items).ChildRules(item =>
        {
            item.RuleFor(model => model.OrderItemId).NotEmpty();
            item.RuleFor(model => model.ProductId).NotEmpty();
            item.RuleFor(model => model.Quantity).GreaterThan(0);
        });
    }
}
