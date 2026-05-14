using FluentValidation;

namespace NamEcommerce.Web.Models.Inventory;

public sealed class SetStockLevelsModel
{
    public Guid Id { get; set; }
    public string? ProductName { get; set; }
    public string? WarehouseName { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal MaxStockLevel { get; set; }
}

public sealed class SetStockLevelsModelValidator : AbstractValidator<SetStockLevelsModel>
{
    public SetStockLevelsModelValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Stock không hợp lệ.");

        RuleFor(x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Mức cảnh báo không được âm.");

        RuleFor(x => x.MaxStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Mức tồn tối đa không được âm.");

        RuleFor(x => x)
            .Must(x => x.MaxStockLevel == 0 || x.MaxStockLevel >= x.ReorderLevel)
            .WithMessage("Mức tồn tối đa phải lớn hơn hoặc bằng mức cảnh báo.");
    }
}
