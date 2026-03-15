using MediatR;
using NamEcommerce.Application.Shared.Dtos.Catalog;

namespace NamEcommerce.Application.Shared.Queries.Models.Catalog;

[Serializable]
public record GetUnitMeasurementById(Guid Id) : IRequest<UnitMeasurementDto?>;