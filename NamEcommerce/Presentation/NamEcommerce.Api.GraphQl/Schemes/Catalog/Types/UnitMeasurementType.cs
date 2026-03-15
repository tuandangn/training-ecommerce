using GraphQL.DataLoader;
using GraphQL.Types;
using NamEcommerce.Api.GraphQl.Models.Catalog;

namespace NamEcommerce.Api.GraphQl.Schemes.Catalog.Types;

public sealed class UnitMeasurementType : ObjectGraphType<UnitMeasurementModel>
{
    public UnitMeasurementType()
    {
        Name = "UnitMeasurementType";
        Description = "Describes unit measurement type";

        Field(c => c.Id).Description("Unit measurement ID");
        Field(c => c.Name).Description("Unit measurement name");
    }
}