using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

namespace NamEcommerce.Web.Extensions;

public static class DeliveryRunStatusExtensions
{
    extension(DeliveryRunStatus status)
    {
        public string GetDisplayColor() => status switch
        {
            DeliveryRunStatus.Planning => "bg-secondary text-white",
            DeliveryRunStatus.ReadyForHandover => "bg-info text-white",
            DeliveryRunStatus.HandedToDriver => "bg-warning text-white",
            DeliveryRunStatus.Closed => "bg-success text-white",
            _ => "bg-secondary text-white"
        };
    }
}
