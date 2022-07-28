﻿using Moq;
using NamEcommerce.Data.Contracts;
using System.Linq.Expressions;

namespace NamEcommerce.TestHelper;

public static class Repository
{
    public static Mock<IRepository<TEntity>> Create<TEntity>() where TEntity : class
    {
        return new Mock<IRepository<TEntity>>();
    }

    public static Mock<IRepository<TEntity>> WithData<TEntity>(this Mock<IRepository<TEntity>> mock, params TEntity[] entities)
        where TEntity : class
    {
        mock.Setup(t => t.GetAllAsync()).ReturnsAsync(entities).Verifiable();
        return mock;
    }

    public static Mock<IRepository<TEntity>> WhenCall<TEntity, TResult>(this Mock<IRepository<TEntity>> mock,
        Expression<Func<IRepository<TEntity>, TResult>> expr, TResult @return)
        where TEntity : class
    {
        mock.Setup(expr).Returns(@return).Verifiable();
        return mock;
    }
    public static Mock<IRepository<TEntity>> WhenCall<TEntity, TResult>(this Mock<IRepository<TEntity>> mock,
        Expression<Func<IRepository<TEntity>, IEnumerable<TResult>>> expr, params TResult[] @return)
        where TEntity : class
    {
        mock.Setup(expr).Returns(@return).Verifiable();
        return mock;
    }

    public static Mock<IRepository<TEntity>> WhenCall<TEntity, TResult>(this Mock<IRepository<TEntity>> mock,
        Expression<Func<IRepository<TEntity>, Task<TResult>>> expr, TResult @return)
        where TEntity : class
    {
        mock.Setup(expr).ReturnsAsync(@return).Verifiable();
        return mock;
    }
    public static Mock<IRepository<TEntity>> WhenCall<TEntity, TResult>(this Mock<IRepository<TEntity>> mock,
        Expression<Func<IRepository<TEntity>, Task<IEnumerable<TResult>>>> expr, params TResult[] @return)
        where TEntity : class
    {
        mock.Setup(expr).ReturnsAsync(@return).Verifiable();
        return mock;
    }
}