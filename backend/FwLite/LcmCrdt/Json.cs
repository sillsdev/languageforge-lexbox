using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using LcmCrdt.Changes;
using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using SIL.Harmony;

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

            var pathLambda = builder.Arguments[1] switch
            {
                LambdaExpression lambda => lambda,
                UnaryExpression unary => (LambdaExpression)unary.Operand,
                { } exp => throw new InvalidOperationException($"invalid property {exp}.")
            };

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

            if (returnType != typeof(string) && returnType != typeof(RichString))//bypass rich string so it can be used with .GetPlainText()
            {
                valueExpression = PseudoFunctions.MakeTryConvert(new SqlDataType(new DbDataType(returnType)),
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
                            var sql = builder.ConvertExpressionToSql(mce.Arguments[0]);
                            ArgumentNullException.ThrowIfNull(sql);
                            parameters.Insert(0, sql);
                            pathBody = mce.Object;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Invalid property path for expression {mce}.");
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
            var indexerProperty = GetIndexerProperties(declaringType);
            foreach (var propertyInfo in indexerProperty)
            {
                if (propertyInfo.GetMethod == method || propertyInfo.SetMethod == method)
                    return true;
            }
            return false;
        }

        private static IEnumerable<PropertyInfo> GetIndexerProperties(Type type)
        {
            var defaultPropertyAttribute = type.GetCustomAttributes<DefaultMemberAttribute>()
                .FirstOrDefault();
            if (defaultPropertyAttribute is null) return [];
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.Name == defaultPropertyAttribute.MemberName);
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

    [Sql.Extension(typeof(JsonValuePathBuilder),
        Precedence = Precedence.Primary,
        ServerSideOnly = true,
        CanBeNull = true)]
    private static TValue? ValueInternal<TProp, TValue>(TProp prop, Expression<Func<TProp, TValue>> valueAccess)
    {
        return valueAccess.Compile()(prop);
    }

    //indicates that linq2db should rewrite Sense.SemanticDomains.Query()
    //into code in QueryExpression: Sense.SemanticDomains.QueryInternal().Select(v => v.Value)
    [ExpressionMethod(nameof(QueryExpression))]
    public static IQueryable<T> Query<T>(IEnumerable<T> value)
    {
        return value.AsQueryable();
    }

    private static Expression<Func<IEnumerable<T>, IQueryable<T>>> QueryExpression<T>()
    {
        return (values) => values.QueryInternal().Select(v => v.Value);
    }

    [ExpressionMethod(nameof(QueryExpressionMultiString))]
    public static IQueryable<string> QueryValues(MultiString values)
    {
        return values.Values.Values.AsQueryable();
    }

    private static Expression<Func<MultiString, IQueryable<string>>> QueryExpressionMultiString()
    {
        return (values) => values.QueryInternal().Select(v => v.Value);
    }

    //indicates that linq2db should rewrite Sense.SemanticDomains.Query(d => d.Code)
    //into code in QueryExpression: Sense.SemanticDomains.QueryInternal().Select(v => Sql.Value(v.Value, d => d.Code))
    [ExpressionMethod(nameof(QuerySelectExpression))]
    public static IQueryable<T_Value> Query<T, T_Value>(IEnumerable<T> value, Expression<Func<T, T_Value>> select)
    {
        return value.Select(select.Compile()).AsQueryable();
    }

    private static Expression<Func<IEnumerable<T>, Expression<Func<T, T_Value>>, IQueryable<T_Value>>> QuerySelectExpression<T, T_Value>()
    {
        return (values, select) => values.QueryInternal().Select(v => ValueInternal(v.Value, select)!);
    }

    //these 2 methods tell linq2db to treat the given property as a table where each row looks like a JsonEach
    //however we don't really care about any other columns and probably want to just use the value, that's what the QueryExpression does above
    [Sql.TableFunction("json_each", argIndices: [0])]
    private static IQueryable<JsonEach<T>> QueryInternal<T>(this IEnumerable<T> value)
    {
        throw new NotImplementedException("only supported server side");
    }

    [Sql.TableFunction("json_each", argIndices: [0])]
    private static IQueryable<JsonEach<string>> QueryInternal(this MultiString value)
    {
        throw new NotImplementedException("only supported server side");
    }

    [Sql.Expression("(select group_concat(s.value->>'Text', '') from json_each({0}->>'Spans') as s)", PreferServerSide = true)]
    public static string GetPlainText(RichString? richString)
    {
        return richString?.GetPlainText() ?? "";
    }

    //maps to a row from json_each
    private record JsonEach<T>(
        [property: Column("value")] T Value,
        [property: Column("key")] string Key,
        [property: Column("type")] string Type,
        [property: Column("id")] int Id,
        [property: Column("fullkey")] string FullKey,
        [property: Column("path")] string Path);

    public static IJsonTypeInfoResolver MakeLcmCrdtExternalJsonTypeResolver(this CrdtConfig config)
    {
        var resolver = config.MakeJsonTypeResolver();
        resolver = resolver.AddExternalMiniLcmModifiers();
        return resolver;
    }

    internal static void ExampleSentenceTranslationModifier(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type == typeof(ExampleSentence))
        {
            //legacy property
            var propertyInfo = typeInfo.CreateJsonPropertyInfo(typeof(RichMultiString), "Translation");
            propertyInfo.Set = (obj, value) =>
            {
                var exampleSentence = (ExampleSentence)obj;
                if (exampleSentence.Translations.Any()) throw new InvalidOperationException("Cannot set translations when they already exist.");
                var richString = (RichMultiString?)value;
                if (richString.IsNullOrEmpty()) return;
#pragma warning disable CS0618 // Type or member is obsolete
                exampleSentence.Translations = [Translation.FromMultiString(richString)];
#pragma warning restore CS0618 // Type or member is obsolete
            };
            typeInfo.Properties.Add(propertyInfo);
        }
    }
}
