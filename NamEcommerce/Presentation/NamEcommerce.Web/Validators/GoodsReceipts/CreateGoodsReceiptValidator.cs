using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.GoodsReceipts;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.GoodsReceipts;

public sealed class CreateGoodsReceiptValidator : AbstractValidator<CreateGoodsReceiptModel>
{
    public CreateGoodsReceiptValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.CreatedOn)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.CreatedOn"]])
            .LessThanOrEqualTo(DateTime.Now).WithMessage(m => localizer["Error.GoodsReceipt.CreationDateInvalid"]);

        RuleFor(m => m.PictureIds)
            .NotEmpty().WithMessage(m => localizer["Error.GoodsReceipt.ProofPictureRequired"]);

        RuleFor(m => m.Items)
            .NotEmpty().WithMessage(m => localizer["Error.GoodsReceipt.ItemsRequired"]);

        RuleForEach(m => m.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage(i => localizer["Error.Required", localizer["Label.Product"]]);

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage(i => localizer["Error.GoodsReceipt.Item.QuantityMustBePositive"]);

            item.RuleFor(i => i.UnitCost)
                .GreaterThanOrEqualTo(0)
                .When(i => i.UnitCost.HasValue)
                .WithMessage(i => localizer["Error.GoodsReceipt.Item.UnitCostCannotBeNegative"]);
        });
    }
}
