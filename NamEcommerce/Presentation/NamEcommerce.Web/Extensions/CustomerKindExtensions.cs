using NamEcommerce.Domain.Shared.Enums.Customers;

namespace NamEcommerce.Web.Extensions;

public static class CustomerKindExtensions
{
    extension(CustomerKind)
    {
        public static IEnumerable<(int value, string text)> GetOptions()
            => Enum.GetValues<CustomerKind>().Select(kind => ((int)kind, kind.GetDisplayText()));
    }

    extension(CustomerKind kind)
    {
        public string GetDisplayText() => kind switch
        {
            CustomerKind.RetailWalkIn => "Khách lẻ",
            CustomerKind.Standard => "Khách quen",
            _ => throw new InvalidDataException(nameof(kind)),
        };

        public string GetDisplayColor() => kind switch
        {
            CustomerKind.RetailWalkIn => "bg-warning text-light",
            CustomerKind.Standard => "bg-primary text-light",
            _ => throw new InvalidDataException(nameof(kind)),
        };
    }
}
