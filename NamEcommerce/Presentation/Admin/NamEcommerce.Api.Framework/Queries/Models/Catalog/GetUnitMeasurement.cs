using MediatR;
using NamEcommerce.Admin.Contracts.Models.Catalog;
using NamEcommerce.Admin.Contracts.Models.Common;

namespace NamEcommerce.Admin.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed record GetUnitMeasurement(Guid Id) : IRequest<ResponseModel<UnitMeasurementModel?>>;
