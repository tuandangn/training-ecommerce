using MediatR;
using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;

namespace NamEcommerce.Api.GraphQl.DataLoaders;

public sealed class UnitMeasurementDataLoader : IUnitMeasurementDataLoader
{
    public const string GET_ALL = "UnitMeasurementDataLoader.GetAll";
    public const string GET_BY_ID = "UnitMeasurementDataLoader.GetById";

    private readonly IMediator _mediator;

    public UnitMeasurementDataLoader(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<IEnumerable<UnitMeasurementDto>> GetAllUnitMeasurementsAsync(CancellationToken cancellationToken)
        => _mediator.Send(new GetAllUnitMeasurements(), cancellationToken);

    public async Task<UnitMeasurementDto?> GetUnitMeasurementByIdAsync(CancellationToken cancellationToken, Guid? id)
    {
        if (!id.HasValue)
            return null;
        return await _mediator.Send(new GetUnitMeasurementById(id.Value), cancellationToken);
    }

    public async Task<IDictionary<Guid, UnitMeasurementDto>> GetUnitMeasurementsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var categories = await _mediator.Send(new GetUnitMeasurementsByIds(ids), cancellationToken);

        return categories.ToDictionary(category => category.Id);
    }
}
