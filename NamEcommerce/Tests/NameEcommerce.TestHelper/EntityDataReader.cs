using Moq;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Services;
using System.Linq.Expressions;

namespace NamEcommerce.TestHelper;

public static class EntityDataReader
{
    public static Mock<IEntityDataReader<TEntity>> Create<TEntity>() where TEntity : AppAggregateEntity 
        => new Mock<IEntityDataReader<TEntity>>();

    public static Mock<IEntityDataReader<TEntity>> WithData<TEntity>(this Mock<IEntityDataReader<TEntity>> mock, params TEntity[] entities)
        where TEntity : AppAggregateEntity
    {
        mock.Setup(t => t.DataSource).Returns(entities.AsQueryable()).Verifiable();
        return mock;
    }

    public static Mock<IEntityDataReader<TEntity>> WhenCall<TEntity, TResult>(this Mock<IEntityDataReader<TEntity>> mock,
        Expression<Func<IEntityDataReader<TEntity>, TResult>> expr, TResult @return)
        where TEntity : AppAggregateEntity
    {
        mock.Setup(expr).Returns(@return).Verifiable();
        return mock;
    }
    public static Mock<IEntityDataReader<TEntity>> WhenCall<TEntity, TResult>(this Mock<IEntityDataReader<TEntity>> mock,
        Expression<Func<IEntityDataReader<TEntity>, IEnumerable<TResult>>> expr, params TResult[] @return)
        where TEntity : AppAggregateEntity
    {
        mock.Setup(expr).Returns(@return).Verifiable();
        return mock;
    }

    public static Mock<IEntityDataReader<TEntity>> WhenCall<TEntity, TResult>(this Mock<IEntityDataReader<TEntity>> mock,
        Expression<Func<IEntityDataReader<TEntity>, Task<TResult>>> expr, TResult @return)
        where TEntity : AppAggregateEntity
    {
        mock.Setup(expr).ReturnsAsync(@return).Verifiable();
        return mock;
    }
    public static Mock<IEntityDataReader<TEntity>> WhenCall<TEntity, TResult>(this Mock<IEntityDataReader<TEntity>> mock,
        Expression<Func<IEntityDataReader<TEntity>, Task<IEnumerable<TResult>>>> expr, params TResult[] @return)
        where TEntity : AppAggregateEntity
    {
        mock.Setup(expr).ReturnsAsync(@return).Verifiable();
        return mock;
    }
}
