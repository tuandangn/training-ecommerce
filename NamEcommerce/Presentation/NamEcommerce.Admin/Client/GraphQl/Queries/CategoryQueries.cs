namespace NamEcommerce.Admin.Client.GraphQl.Queries;

public static class CategoryQueries
{
    public const string CategoryListQuery =
    @"
    query CategoryListQuery {
        catalog {
            categories: allCategories {
                id name
                parent { id name }
            }
        }
    }
    ";

    public const string CategoryQuery =
    @"
    query CategoryQuery($id: ID!) {
        catalog {
            category(id: $id) {
                id name
                parent { id name }
            }
        }
    }
    ";
}
