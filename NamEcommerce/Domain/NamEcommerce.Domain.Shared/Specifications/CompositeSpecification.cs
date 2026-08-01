using System.Linq.Expressions;

namespace NamEcommerce.Domain.Shared.Specifications;

[Serializable]
public sealed class CompositeSpecification<T> : ISpecification<T>
{
    private static readonly Expression<Func<T, bool>> _emptyCriteria = t => true;

    private Expression<Func<T, bool>> currentCriteria;

    public CompositeSpecification()
    {
        currentCriteria = _emptyCriteria;
    }
    public CompositeSpecification(ISpecification<T> initialCriteria)
    {
        currentCriteria = initialCriteria.Criteria;
    }

    public Expression<Func<T, bool>> Criteria => currentCriteria;
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    public void AddInclude(Expression<Func<T, object>> include)
        => Includes.Add(include);
    public void ApplyOrderBy(Expression<Func<T, object>> orderBy)
        => OrderBy = orderBy;
    public void ApplyOrderByDescending(Expression<Func<T, object>> orderByDesc)
        => OrderByDescending = orderByDesc;

    public bool IsSatisfiedBy(T entity) => Criteria.Compile()(entity);

    public CompositeSpecification<T> And(ISpecification<T> right)
    {
        if (currentCriteria == _emptyCriteria)
            currentCriteria = right.Criteria;
        else
            currentCriteria = currentCriteria.And(right.Criteria);

        return this;
    }
    public CompositeSpecification<T> AndNot(ISpecification<T> right)
    {
        if (currentCriteria == _emptyCriteria)
            currentCriteria = right.Criteria;
        else
            currentCriteria = currentCriteria.AndNot(right.Criteria);

        return this;
    }
    public CompositeSpecification<T> Or(ISpecification<T> right)
    {
        if (currentCriteria == _emptyCriteria)
            currentCriteria = right.Criteria;
        else
            currentCriteria = currentCriteria.Or(right.Criteria);

        return this;
    }
}
