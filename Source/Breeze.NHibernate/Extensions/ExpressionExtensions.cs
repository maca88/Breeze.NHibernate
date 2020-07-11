using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Breeze.NHibernate.Extensions
{
    internal static class ExpressionExtensions
    {
        internal static MemberInfo GetMemberInfo<TSource, TResult>(this Expression<Func<TSource, TResult>> lambda)
        {
            if (lambda == null)
            {
                throw new ArgumentNullException(nameof(lambda));
            }

            return ((MemberExpression) lambda.Body).Member;
        }
    }
}
