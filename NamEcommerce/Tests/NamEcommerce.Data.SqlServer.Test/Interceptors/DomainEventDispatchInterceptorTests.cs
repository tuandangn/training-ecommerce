using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using NamEcommerce.Data.SqlServer.Interceptors;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Events;

namespace NamEcommerce.Data.SqlServer.Test.Interceptors;

public sealed class DomainEventDispatchInterceptorTests
{
    #region Test Fixtures

    /// <summary>
    /// Concrete domain event để test dispatch.
    /// </summary>
    private sealed record FakeDomainEvent(string Payload) : DomainEvent;

    /// <summary>
    /// Concrete aggregate kế thừa <see cref="AppAggregateEntity"/> + expose <c>RaiseDomainEvent</c>
    /// dưới dạng public để test dễ raise event.
    /// </summary>
    private sealed record FakeAggregate : AppAggregateEntity
    {
        public FakeAggregate(Guid id, string name) : base(id)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    /// <summary>
    /// In-memory DbContext chứa <see cref="FakeAggregate"/> để test với EF tracking thật.
    /// </summary>
    private sealed class FakeDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public FakeDbContext(DbContextOptions<FakeDbContext> options) : base(options) { }
        public DbSet<FakeAggregate> Aggregates => Set<FakeAggregate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FakeAggregate>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name);
                b.Ignore(x => x.DomainEvents);
            });
            base.OnModelCreating(modelBuilder);
        }
    }

    private static FakeDbContext CreateInMemoryDbContext(DomainEventDispatchInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<FakeDbContext>()
            .UseInMemoryDatabase(databaseName: $"DomainEventTest_{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;
        return new FakeDbContext(options);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_PublisherIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DomainEventDispatchInterceptor(null!));
    }

    #endregion

    #region SavedChangesAsync

    [Fact]
    public async Task SavedChangesAsync_AggregateRaisedEvent_PublishesEventThenClears()
    {
        var publisherMock = new Mock<IPublisher>();
        publisherMock
            .Setup(p => p.Publish(It.IsAny<FakeDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var interceptor = new DomainEventDispatchInterceptor(publisherMock.Object);
        await using var context = CreateInMemoryDbContext(interceptor);

        var aggregate = new FakeAggregate(Guid.NewGuid(), "fake");
        var domainEvent = new FakeDomainEvent("payload-1");
        aggregate.Raise(domainEvent);

        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        publisherMock.Verify(p => p.Publish(
            It.Is<FakeDomainEvent>(e => e.Payload == "payload-1" && e.EventId == domainEvent.EventId),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public async Task SavedChangesAsync_AggregateRaisedMultipleEvents_PublishesAllInOrder()
    {
        var publishedEvents = new List<FakeDomainEvent>();
        var publisherMock = new Mock<IPublisher>();
        publisherMock
            .Setup(p => p.Publish(It.IsAny<FakeDomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((evt, _) => publishedEvents.Add((FakeDomainEvent)evt))
            .Returns(Task.CompletedTask);

        var interceptor = new DomainEventDispatchInterceptor(publisherMock.Object);
        await using var context = CreateInMemoryDbContext(interceptor);

        var aggregate = new FakeAggregate(Guid.NewGuid(), "fake");
        aggregate.Raise(new FakeDomainEvent("e1"));
        aggregate.Raise(new FakeDomainEvent("e2"));
        aggregate.Raise(new FakeDomainEvent("e3"));

        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        Assert.Equal(3, publishedEvents.Count);
        Assert.Equal("e1", publishedEvents[0].Payload);
        Assert.Equal("e2", publishedEvents[1].Payload);
        Assert.Equal("e3", publishedEvents[2].Payload);
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public async Task SavedChangesAsync_AggregateHasNoDomainEvents_DoesNotPublishAnything()
    {
        var publisherMock = new Mock<IPublisher>(MockBehavior.Strict);

        var interceptor = new DomainEventDispatchInterceptor(publisherMock.Object);
        await using var context = CreateInMemoryDbContext(interceptor);

        context.Aggregates.Add(new FakeAggregate(Guid.NewGuid(), "no-events"));
        await context.SaveChangesAsync();

        publisherMock.Verify(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SavedChangesAsync_NoAggregateTracked_DoesNotPublishAnything()
    {
        var publisherMock = new Mock<IPublisher>(MockBehavior.Strict);

        var interceptor = new DomainEventDispatchInterceptor(publisherMock.Object);
        await using var context = CreateInMemoryDbContext(interceptor);

        // Không Add gì cả
        await context.SaveChangesAsync();

        publisherMock.Verify(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SavedChangesAsync_MultipleAggregatesEachWithEvents_PublishesAllEvents()
    {
        var publishedCount = 0;
        var publisherMock = new Mock<IPublisher>();
        publisherMock
            .Setup(p => p.Publish(It.IsAny<FakeDomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback(() => publishedCount++)
            .Returns(Task.CompletedTask);

        var interceptor = new DomainEventDispatchInterceptor(publisherMock.Object);
        await using var context = CreateInMemoryDbContext(interceptor);

        var aggA = new FakeAggregate(Guid.NewGuid(), "A");
        aggA.Raise(new FakeDomainEvent("a1"));
        aggA.Raise(new FakeDomainEvent("a2"));

        var aggB = new FakeAggregate(Guid.NewGuid(), "B");
        aggB.Raise(new FakeDomainEvent("b1"));

        context.Aggregates.AddRange(aggA, aggB);
        await context.SaveChangesAsync();

        Assert.Equal(3, publishedCount);
        Assert.Empty(aggA.DomainEvents);
        Assert.Empty(aggB.DomainEvents);
    }

    [Fact]
    public async Task SavedChangesAsync_AfterDispatch_EventsAreClearedSoSecondSaveDoesNotRePublish()
    {
        var publishedCount = 0;
        var publisherMock = new Mock<IPublisher>();
        publisherMock
            .Setup(p => p.Publish(It.IsAny<FakeDomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback(() => publishedCount++)
            .Returns(Task.CompletedTask);

        var interceptor = new DomainEventDispatchInterceptor(publisherMock.Object);
        await using var context = CreateInMemoryDbContext(interceptor);

        var aggregate = new FakeAggregate(Guid.NewGuid(), "agg");
        aggregate.Raise(new FakeDomainEvent("once"));

        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();
        Assert.Equal(1, publishedCount);

        // SaveChanges lần 2 không có event mới
        await context.SaveChangesAsync();
        Assert.Equal(1, publishedCount);
    }

    #endregion
}
