using NamEcommerce.Domain.Shared.Events;

namespace NamEcommerce.Domain.Shared.Events.Finance;

public sealed record FixedAssetCreated(Guid AssetId, string Name) : DomainEvent;

public sealed record FixedAssetDisposed(Guid AssetId, decimal RemainingBookValue) : DomainEvent, IReliableDomainEvent;
