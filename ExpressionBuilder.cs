using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace mBase.App.Shared.Utils.SearchEngin
{
    public static class ExpressionBuilder
    {
        private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod("Contains");
        private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod("StartsWith", new Type[] { typeof(string) });
        private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod("EndsWith", new Type[] { typeof(string) });
        private static readonly MethodInfo ToLowerMethod = typeof(string).GetMethod("ToLower", new Type[] { });

        public static Op GetOp(string opName)
        {

            switch (opName)
            {
                case "Equals":
                    return Op.Equals;

                case "NotEquals":
                    return Op.NotEquals;

                case "GreaterThan":
                    return Op.GreaterThan;

                case "GreaterThanOrEqual":
                    return Op.GreaterThanOrEqual;

                case "LessThan":
                    return Op.LessThan;

                case "LessThanOrEqual":
                    return Op.LessThanOrEqual;

                case "Contains":
                    return Op.Contains;

                case "StartsWith":
                    return Op.StartsWith;

                case "EndsWith":
                    return Op.EndsWith;
                default:
                    return Op.Equals;
            }

            return Op.Equals;
        }

        public static Expression<Func<T, bool>> GetExpression<T>(IList<FilterEngine> filters)
        {
            if (filters.Count == 0)
                return null;

            ParameterExpression param = Expression.Parameter(typeof(T), "t");
            Expression exp = null;

            if (filters.Count == 1)
                exp = GetExpression<T>(param, filters[0]);
            else if (filters.Count == 2)
                exp = GetExpression<T>(param, filters[0], filters[1]);
            else
            {
                while (filters.Count > 0)
                {
                    var f1 = filters[0];
                    var f2 = filters[1];

                    if (exp == null)
                        exp = GetExpression<T>(param, filters[0], filters[1]);
                    else
                        exp = Expression.AndAlso(exp, GetExpression<T>(param, filters[0], filters[1]));

                    filters.Remove(f1);
                    filters.Remove(f2);

                    if (filters.Count == 1)
                    {
                        exp = Expression.AndAlso(exp, GetExpression<T>(param, filters[0]));
                        filters.RemoveAt(0);
                    }
                }
            }
            return Expression.Lambda<Func<T, bool>>(exp, param);
        }

        private static Expression GetExpression<T>(ParameterExpression param, FilterEngine filter)
        {
            var propertyName = filter.PropertyName.ToLower();
            MemberExpression member = Expression.Property(param, propertyName);
            string filterValue = filter.Value.ToString().ToLower();

            UnaryExpression constant;
            if (member.Type == typeof(Int32?) || member.Type == typeof(Int32))
            {
                constant = Expression.Convert(Expression.Constant(Int32.Parse(filterValue)), member.Type);
            }
            else if (member.Type == typeof(DateTime?) || member.Type == typeof(DateTime))
            {
                constant = Expression.Convert(Expression.Constant(DateTime.Parse(filterValue)), member.Type);
            }
            else if (member.Type == typeof(Guid?) || member.Type == typeof(Guid))
            {
                constant = Expression.Convert(Expression.Constant(Guid.Parse(filterValue)), member.Type);
            }
            else if (member.Type == typeof(Boolean?) || member.Type == typeof(Boolean))
            {
                constant = Expression.Convert(Expression.Constant(Boolean.Parse(filterValue)), member.Type);
            }
            else
            {
                constant = Expression.Convert(Expression.Constant(filterValue), member.Type);
            }


            switch (filter.Operation)
            {
                case Op.Equals:
                    if (member.Type.Name == "String")
                    {
                        var expToLower = Expression.Call(member, ToLowerMethod);
                        return Expression.Equal(expToLower, constant);
                    }
                    return Expression.Equal(member, constant);

                case Op.GreaterThan:
                    return Expression.GreaterThan(member, constant);

                case Op.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(member, constant);

                case Op.LessThan:
                    return Expression.LessThan(member, constant);

                case Op.LessThanOrEqual:
                    return Expression.LessThanOrEqual(member, constant);

                case Op.Contains:

                    var expToLowerContains = Expression.Call(member, ToLowerMethod);
                    return Expression.Call(expToLowerContains, ContainsMethod, constant); // call StartsWith() on the exp, which is property.ToLower()

                case Op.StartsWith:

                    var expToLowerStartsWith = Expression.Call(member, ToLowerMethod);
                    return Expression.Call(expToLowerStartsWith, StartsWithMethod, constant);

                case Op.EndsWith:

                    var expToLowerEndsWith = Expression.Call(member, ToLowerMethod);
                    return Expression.Call(expToLowerEndsWith, EndsWithMethod, constant);

                case Op.NotEquals:
                    if (member.Type.Name == "String")
                    {
                        var expToLowerNotEqual = Expression.Call(member, ToLowerMethod);
                        return Expression.NotEqual(expToLowerNotEqual, constant);
                    }
                    else
                    {
                        return Expression.NotEqual(member, constant);
                    }

            }

            return null;
        }

        private static BinaryExpression GetExpression<T>
            (ParameterExpression param, FilterEngine filter1, FilterEngine filter2)
        {
            Expression bin1 = GetExpression<T>(param, filter1);
            Expression bin2 = GetExpression<T>(param, filter2);

            return Expression.AndAlso(bin1, bin2);
        }
    }
}