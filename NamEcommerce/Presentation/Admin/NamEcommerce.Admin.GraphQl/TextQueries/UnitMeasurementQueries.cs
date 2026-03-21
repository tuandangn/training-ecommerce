namespace NamEcommerce.Admin.GraphQl.TextQueries;

public static class UnitMeasurementQueries
{
    public const string UnitMeasurementListQuery =
    @$"
    query {nameof(UnitMeasurementListQuery)} {{
        catalog {{
            unitMeasurements: allUnitMeasurements {{
                id name
            }}
        }}
    }}
    ";

    public const string UnitMeasurementQuery =
    @$"
    query {nameof(UnitMeasurementQuery)}($id: ID!) {{
        catalog {{
            unitMeasurement(id: $id) {{
                id name
            }}
        }}
    }}
    ";
}
