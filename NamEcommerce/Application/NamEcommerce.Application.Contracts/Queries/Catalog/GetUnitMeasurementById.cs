using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;

namespace NamEcommerce.Application.Contracts.Queries.Catalog;

[Serializable]
public record GetUnitMeasurementById(Guid Id) : IRequest<UnitMeasurementAppDto?>;