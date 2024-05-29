using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using MiniLcm;
using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

namespace LcmCrdt;

public static class Json
{
    sealed class JsonValuePathBuilder : Sql.IExtensionCallBuilder
    {
        public void Build(Sql.ISqExtensionBuilder builder)
        {
            var propExpression = builder.GetExpression(0);

            if (propExpression == null)
                throw new InvalidOperationException("Invalid property.");

            var pathLambda = (LambdaExpression)builder.Arguments[1];

            var pathBody = pathLambda.Body;

            var parameters = new List<ISqlExpression>();

            BuildParameterPath(pathBody, parameters, builder);

            if (parameters.Count == 0)
                throw new InvalidOperationException("invalid property path.");

            var expressionStr = "{0}->>" +
                                string.Join("->>", Enumerable.Range(1, parameters.Count).Select(x => "{" + x + "}"));

            parameters.Insert(0, propExpression);

            var valueExpression = (ISqlExpression)new SqlExpression(typeof(string),
                expressionStr,
                Precedence.Primary,
                parameters.ToArray());

            var returnType = ((MethodInfo)builder.Member).ReturnType;

            if (returnType != typeof(string))
            {
                valueExpression = PseudoFunctions.MakeConvert(new SqlDataType(new DbDataType(returnType)),
                    new SqlDataType(new DbDataType(typeof(string), DataType.Text)),
                    valueExpression);
            }

            builder.ResultExpression = valueExpression;
        }

        private static void BuildParameterPath(Expression? pathBody,
            List<ISqlExpression> parameters,
            Sql.ISqExtensionBuilder builder)
        {
            while (pathBody is MemberExpression or MethodCallExpression)
            {
                switch (pathBody)
                {
                    case MemberExpression me:
                        parameters.Insert(0, new SqlValue(me.Member.Name));
                        pathBody = me.Expression;
                        break;
                    case MethodCallExpression mce:
                        if (IsIndexerPropertyMethod(mce.Method))
                        {
                            parameters.Insert(0, builder.ConvertExpressionToSql(mce.Arguments[0]));
                            pathBody = mce.Object;
                        }
                        else
                        {
                            throw new InvalidOperationException("Invalid property path.");
                        }

                        break;
                    default:
                        throw new InvalidOperationException("Invalid property path.");
                }
            }
        }

        public static bool IsIndexerPropertyMethod(MethodInfo method)
        {
            var declaringType = method.DeclaringType;
            if (declaringType is null) return false;
            var indexerProperty = GetIndexerProperty(declaringType);
            if (indexerProperty is null) return false;
            return method == indexerProperty.GetMethod || method == indexerProperty.SetMethod;
        }

        private static PropertyInfo? GetIndexerProperty(Type type)
        {
            var defaultPropertyAttribute = type.GetCustomAttributes<DefaultMemberAttribute>()
                .FirstOrDefault();
            if (defaultPropertyAttribute is null) return null;
            return type.GetProperty(defaultPropertyAttribute.MemberName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }

    [Sql.Extension(typeof(JsonValuePathBuilder),
        Precedence = Precedence.Primary,
        ServerSideOnly = true,
        CanBeNull = true)]
    public static TValue? Value<TProp, TValue>(TProp prop, Func<TProp, TValue> valueAccess)
    {
        return valueAccess(prop);
    }
}
