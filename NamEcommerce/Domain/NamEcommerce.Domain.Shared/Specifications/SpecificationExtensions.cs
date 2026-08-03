using System.Linq.Expressions;

namespace NamEcommerce.Domain.Shared.Specifications;

public static class SpecificationExtensions
{
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        if (left == null) return right;
        if (right == null) return left;

        // Dùng parameter của biểu thức 'left' làm parameter chung
        var parameter = left.Parameters[0];

        // Thay thế parameter của 'right' bằng parameter của 'left'
        var visitor = new ReplaceParameterVisitor(right.Parameters[0], parameter);
        var rightBody = visitor.Visit(right.Body);

        // Kết hợp 2 body bằng AndAlso
        var body = Expression.AndAlso(left.Body, rightBody);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        if (left == null) return right;
        if (right == null) return left;

        // 1. Dùng parameter của 'left' làm parameter chung
        var parameter = left.Parameters[0];

        // 2. Đổi parameter của 'right' sang parameter của 'left'
        var visitor = new ReplaceParameterVisitor(right.Parameters[0], parameter);
        var rewrittenRightBody = visitor.Visit(right.Body);

        // 3. Kết hợp bằng OrElse
        var combinedBody = Expression.OrElse(left.Body, rewrittenRightBody); 
        
        return Expression.Lambda<Func<T, bool>>(combinedBody, parameter);
    }

    public static Expression<Func<T, bool>> AndNot<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        if (left == null)
        {
            if (right == null) 
                throw new ArgumentNullException(nameof(right), "Both left and right expressions cannot be null.");
            // Nếu left null, kết quả chỉ là NOT(right)
            var notBody = Expression.Not(right.Body);
            return Expression.Lambda<Func<T, bool>>(notBody, right.Parameters[0]);
        }
        if (right == null) return left;

        // 1. Dùng parameter của 'left' làm parameter chung
        var parameter = left.Parameters[0];

        // 2. Đổi parameter của 'right' sang parameter của 'left'
        var visitor = new ReplaceParameterVisitor(right.Parameters[0], parameter);
        var rewrittenRightBody = visitor.Visit(right.Body);

        // 3. Phủ định rightBody bằng Expression.Not: !(right)
        var notRightBody = Expression.Not(rewrittenRightBody);

        // 4. Kết hợp lại: left && !(right)
        var combinedBody = Expression.AndAlso(left.Body, notRightBody);

        return Expression.Lambda<Func<T, bool>>(combinedBody, parameter);
    }

    private class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}
