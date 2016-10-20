using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GeekseatBus
{
    public static class ExpressionExtensions
    {
        public static Expression<T> Compose<T>(this Expression<T> firstExpression, Expression<T> secondExpression,
            Func<Expression, Expression, Expression> merge)
        {
            var map = firstExpression.Parameters.Select((first, index) => new
            {
                key = secondExpression.Parameters[index],
                value = first
            }).ToDictionary(pair => pair.key, pair => pair.value);
            var secondBody = ParameterRebinder.ReplaceParameters(map, secondExpression.Body);

            return Expression.Lambda<T>(merge(firstExpression.Body, secondBody), firstExpression.Parameters);
        }

        public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> firsExpression,
            Expression<Func<T, bool>> secondExpression)
        {
            return firsExpression.Compose(secondExpression, Expression.AndAlso);
        }

        public static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>> firsExpression,
            Expression<Func<T, bool>> secondExpression)
        {
            return firsExpression.Compose(secondExpression, Expression.OrElse);
        }
    }

    internal class ParameterRebinder : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> _map;

        public ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
        {
            _map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
        }

        public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map,
            Expression expression)
        {
            return new ParameterRebinder(map).Visit(expression);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            ParameterExpression replacemant;
            if (_map.TryGetValue(node, out replacemant))
                node = replacemant;

            return base.VisitParameter(node);
        }
    }
}