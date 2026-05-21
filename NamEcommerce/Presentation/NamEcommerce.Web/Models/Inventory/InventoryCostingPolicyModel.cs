using FluentValidation;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Models.Inventory;

public sealed class InventoryCostingPolicyModel
{
    public Guid Id { get; set; }
    public InventoryCostingMethod CostingMethod { get; set; }
    public InventoryValuationScope ValuationScope { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Note { get; set; }

    public static InventoryCostingPolicyModel FromSettings(InventoryCostingPolicySettingsModel settings)
        => new()
        {
            Id = settings.Id,
            CostingMethod = (InventoryCostingMethod)settings.CostingMethod,
            ValuationScope = (InventoryValuationScope)settings.ValuationScope,
            EffectiveFrom = settings.EffectiveFrom,
            CreatedAt = settings.CreatedAt,
            Note = settings.Note
        };
}

public sealed class InventoryCostingPolicyModelValidator : AbstractValidator<InventoryCostingPolicyModel>
{
    public InventoryCostingPolicyModelValidator()
    {
        RuleFor(x => x.CostingMethod)
            .Equal(InventoryCostingMethod.WeightedAverage)
            .WithMessage("Hiện tại chỉ hỗ trợ bình quân gia quyền.");

        RuleFor(x => x.ValuationScope)
            .Equal(InventoryValuationScope.Product)
            .WithMessage("Hiện tại giá vốn chỉ được tính theo sản phẩm.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Vui lòng chọn thời điểm hiệu lực.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Ghi chú không được vượt quá 1000 ký tự.");
    }
}
