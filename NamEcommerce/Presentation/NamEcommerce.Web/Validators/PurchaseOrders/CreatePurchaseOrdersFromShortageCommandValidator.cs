using FluentValidation;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class CreatePurchaseOrdersFromShortageCommandValidator : AbstractValidator<CreatePurchaseOrdersFromShortageCommand>
{
    public CreatePurchaseOrdersFromShortageCommandValidator()
    {
        RuleFor(command => command.Groups).NotEmpty();
        RuleForEach(command => command.Groups).ChildRules(group =>
        {
            group.RuleFor(item => item.VendorId).NotEmpty();
            group.RuleFor(item => item.Items).NotEmpty();
            group.RuleForEach(item => item.Items).ChildRules(line =>
            {
                line.RuleFor(item => item.OrderItemId).NotEmpty();
                line.RuleFor(item => item.ProductId).NotEmpty();
                line.RuleFor(item => item.Quantity).GreaterThan(0);
                line.RuleFor(item => item.UnitCost).GreaterThanOrEqualTo(0);
                line.RuleForEach(item => item.Actions).ChildRules(action =>
                {
                    action.RuleFor(item => item.ActionType).NotEmpty();
                    action.RuleFor(item => item.Quantity).GreaterThan(0);
                    action.RuleFor(item => item.PurchaseOrderId)
                        .NotEmpty()
                        .When(item => item.ActionType.Equals("AllocateFromExisting", StringComparison.OrdinalIgnoreCase)
                                      || item.ActionType.Equals("MergeIntoDraft", StringComparison.OrdinalIgnoreCase));
                    action.RuleFor(item => item.PurchaseOrderItemId)
                        .NotEmpty()
                        .When(item => item.ActionType.Equals("AllocateFromExisting", StringComparison.OrdinalIgnoreCase));
                });
            });
        });
    }
}
