namespace NamEcommerce.Admin.Client.GraphQL;

public static class CategoryQueries
{
    public const string CategoryListQuery = 
    @"
    query CategoryListQuery {
        catalog {
            category {
                all {
                    id name
                    parent { id name }
                }
            }
        }
    }
    ";
}
