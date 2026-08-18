using MediatR;
using NamEcommerce.Domain.Shared.Events.Catalog;

namespace NamEcommerce.Application.Services.Events.Catalog;

public sealed class ProductCreatedEventHandler : INotificationHandler<ProductCreated>
{
    public Task Handle(ProductCreated notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
