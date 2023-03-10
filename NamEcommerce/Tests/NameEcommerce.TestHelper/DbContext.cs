using Moq;
using NamEcommerce.Data.Contracts;
using System.Linq.Expressions;

namespace NamEcommerce.TestHelper;

public static class DbContext
{
    public static Mock<IDbContext> Create() => new Mock<IDbContext>();

    public static Mock<IDbContext> WhenCall(this Mock<IDbContext> mock,
        Expression<Action<IDbContext>> expr)
    {
        mock.Setup(expr).Verifiable();
        return mock;
    }
    public static Mock<IDbContext> WhenCall<TResult>(this Mock<IDbContext> mock,
        Expression<Func<IDbContext, TResult>> expr, TResult @return)
    {
        mock.Setup(expr).Returns(@return).Verifiable();
        return mock;
    }
    public static Mock<IDbContext> WhenCall<TResult>(this Mock<IDbContext> mock,
        Expression<Func<IDbContext, Task<TResult>>> expr, TResult @return)
    {
        mock.Setup(expr).ReturnsAsync(@return).Verifiable();
        return mock;
    }
    public static Mock<IDbContext> WhenCall<TResult>(this Mock<IDbContext> mock,
        Expression<Func<IDbContext, ValueTask<TResult>>> expr, TResult @return)
    {
        mock.Setup(expr).ReturnsAsync(@return).Verifiable();
        return mock;
    }
    public static Mock<IDbContext> WhenCall<TResult>(this Mock<IDbContext> mock,
        Expression<Func<IDbContext, Task<IEnumerable<TResult>>>> expr, params TResult[] @return)
    {
        mock.Setup(expr).ReturnsAsync(@return).Verifiable();
        return mock;
    }
}
