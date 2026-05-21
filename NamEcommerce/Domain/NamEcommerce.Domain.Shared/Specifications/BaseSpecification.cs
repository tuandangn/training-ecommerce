using System.Linq.Expressions;

namespace NamEcommerce.Domain.Shared.Specifications;

public abstract class BaseSpecification<T> : ISpecification<T>
{
    protected BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    public Expression<Func<T, bool>> Criteria { get; }
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    public bool IsSatisfiedBy(T entity) => Criteria.Compile()(entity);

    protected void AddInclude(Expression<Func<T, object>> include)
        => Includes.Add(include);

    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy)
        => OrderBy = orderBy;

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDesc)
        => OrderByDescending = orderByDesc;
}
