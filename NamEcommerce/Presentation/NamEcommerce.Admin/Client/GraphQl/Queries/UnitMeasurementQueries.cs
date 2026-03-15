namespace NamEcommerce.Admin.Client.GraphQl.Queries;

public static class UnitMeasurementQueries
{
    public const string UnitMeasurementListQuery =
    @"
    query UnitMeasurementListQuery {
        catalog {
            unitMeasurements: allUnitMeasurements {
                id name
            }
        }
    }
    ";

    public const string UnitMeasurementQuery =
    @"
    query UnitMeasurementQuery($id: ID!) {
        catalog {
            unitMeasurement(id: $id) {
                id name
            }
        }
    }
    ";
}
