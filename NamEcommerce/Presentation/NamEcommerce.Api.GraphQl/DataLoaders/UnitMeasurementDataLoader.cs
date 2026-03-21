using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Queries.Catalog;

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

    public Task<IEnumerable<UnitMeasurementAppDto>> GetAllUnitMeasurementsAsync(CancellationToken cancellationToken)
        => _mediator.Send(new GetAllUnitMeasurements(), cancellationToken);

    public async Task<UnitMeasurementAppDto?> GetUnitMeasurementByIdAsync(CancellationToken cancellationToken, Guid? id)
    {
        if (!id.HasValue)
            return null;
        return await _mediator.Send(new GetUnitMeasurementById(id.Value), cancellationToken);
    }

    public async Task<IDictionary<Guid, UnitMeasurementAppDto>> GetUnitMeasurementsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var categories = await _mediator.Send(new GetUnitMeasurementsByIds(ids), cancellationToken);

        return categories.ToDictionary(category => category.Id);
    }
}
