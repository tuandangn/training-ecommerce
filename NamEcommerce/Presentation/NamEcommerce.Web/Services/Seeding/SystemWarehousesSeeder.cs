using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Web.Services.Seeding;

public sealed class SystemWarehousesSeeder(IWarehouseAppService warehouseAppService) : IDataSeeder
{
    private static readonly (string Code, string Name, WarehouseType Type, int Order)[] SystemWarehouses =
    [
        ("KHC", "Kho chính",      WarehouseType.Physical,      1),
        ("KHGT", "Kho giao thẳng", WarehouseType.DirectTransit, 99),
        ("KHHH", "Kho hàng hỏng",  WarehouseType.Damaged,      98),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await warehouseAppService
            .GetWarehousesAsync(0, 200, includeDirectTransit: true, includeDamaged: true)
            .ConfigureAwait(false);

        var existingCodes = existing.Items
            .Select(w => w.Code.Trim().ToUpperInvariant())
            .ToHashSet();

        foreach (var (code, name, type, order) in SystemWarehouses)
        {
            if (existingCodes.Contains(code.ToUpperInvariant()))
                continue;

            await warehouseAppService.CreateWarehouseAsync(new CreateWarehouseAppDto
            {
                Code = code,
                Name = name,
                WarehouseType = (int)type,
                IsActive = true,
                DisplayOrder = order
            }).ConfigureAwait(false);
        }
    }
}
