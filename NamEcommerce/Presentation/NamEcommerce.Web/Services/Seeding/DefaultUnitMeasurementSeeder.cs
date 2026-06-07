using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;

namespace NamEcommerce.Web.Services.Seeding;

public sealed class DefaultUnitMeasurementSeeder(IUnitMeasurementAppService unitMeasurementAppService) : IDataSeeder
{
    private static readonly (string Name, int DecimalPlaces, int Order)[] DefaultUnits =
    [
        ("bao",  0, 1),
        ("bộ",  0, 1),
        ("cái",  0, 1),
        ("cây",  0, 1),
        ("chiếc",  0, 1),
        ("viên",  0, 1),
        ("kg",  1, 1),
        ("m2",   2, 1),
        ("m3",   1, 1),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await unitMeasurementAppService
            .GetUnitMeasurementsAsync(pageSize: 200)
            .ConfigureAwait(false);

        if (existing.Pagination.TotalCount > 0)
            return;

        foreach (var (name, decimalPlaces, order) in DefaultUnits)
        {
            await unitMeasurementAppService.CreateUnitMeasurementAsync(new CreateUnitMeasurementAppDto
            {
                Name = name,
                DecimalPlaces = decimalPlaces,
                DisplayOrder = order
            }).ConfigureAwait(false);
        }
    }
}
