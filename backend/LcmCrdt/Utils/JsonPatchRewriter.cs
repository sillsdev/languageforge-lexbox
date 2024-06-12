using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Crdt.Changes;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Internal;
using SystemTextJsonPatch.Operations;

namespace LcmCrdt.Utils;

public static class JsonPatchRewriter
{

    public static IEnumerable<IChange> RewriteChanges<T, TProp>(this JsonPatchDocument<T> patchDocument,
        Expression<Func<T, TProp>> expr, Func<TProp?, OperationType, IChange> changeFactory) where T : class
    {
        var path = GetPath(expr, null, patchDocument.Options);
        foreach (var operation in patchDocument.Operations.ToArray())
        {
            if (operation.Path == path)
            {
                patchDocument.Operations.Remove(operation);
                yield return changeFactory((TProp?)operation.Value, operation.OperationType);
            }
        }
    }
    public static IEnumerable<IChange> RewriteChanges<T, TProp>(this JsonPatchDocument<T> patchDocument,
        Expression<Func<T, IList<TProp>>> expr, Func<TProp?, Index, OperationType, IChange> changeFactory) where T : class
    {
        var path = GetPath(expr, null, patchDocument.Options);
        foreach (var operation in patchDocument.Operations.ToArray())
        {
            if (operation.Path is null || !operation.Path.StartsWith(path)) continue;
            Index index;
            if (operation.Path == path)
            {
                index = default;
            }
            else
            {
                var parsedPath = new ParsedPath(operation.Path);
                if (parsedPath.LastSegment is "-")
                {
                    index = Index.FromEnd(1);
                }
                else if (int.TryParse(parsedPath.LastSegment, out var i))
                {
                    index = Index.FromStart(i);
                }
                else
                {
                    continue;
                }
            }
            patchDocument.Operations.Remove(operation);
            yield return changeFactory((TProp?)operation.Value, index, operation.OperationType);
        }
    }

    //won't work until dotnet 9 per https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.unsafeaccessorattribute?view=net-8.0#remarks
    //this is due to generics, for now we will use the version copied below
    private static class JsonPatchAccessors<T> where T : class
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetPath")]
        public static extern string ExpressionToJsonPointer<TProp>(
            JsonPatchDocument<T> patchDocument,
            Expression<Func<T, TProp>> expr,
            string? position);
    }


    public static string GetPath<TProp, TModel>(Expression<Func<TModel, TProp>> expr, string? position, JsonSerializerOptions options)
	{
		var segments = GetPathSegments(expr.Body, options);
		var path = string.Join("/", segments);
		if (position != null)
		{
			path += "/" + position;
			if (segments.Count == 0)
			{
				return path;
			}
		}

		return "/" + path;
	}

	private static List<string> GetPathSegments(Expression? expr, JsonSerializerOptions options)
    {
        if (expr == null || expr.NodeType == ExpressionType.Parameter) return [];
		var listOfSegments = new List<string>();
		switch (expr.NodeType)
		{
			case ExpressionType.ArrayIndex:
				var binaryExpression = (BinaryExpression)expr;
				listOfSegments.AddRange(GetPathSegments(binaryExpression.Left, options));
				listOfSegments.Add(binaryExpression.Right.ToString());
				return listOfSegments;

			case ExpressionType.Call:
				var methodCallExpression = (MethodCallExpression)expr;
				listOfSegments.AddRange(GetPathSegments(methodCallExpression.Object, options));
				listOfSegments.Add(EvaluateExpression(methodCallExpression.Arguments[0]));
				return listOfSegments;

			case ExpressionType.Convert:
				listOfSegments.AddRange(GetPathSegments(((UnaryExpression)expr).Operand, options));
				return listOfSegments;

			case ExpressionType.MemberAccess:
				var memberExpression = (MemberExpression)expr;
				listOfSegments.AddRange(GetPathSegments(memberExpression.Expression, options));
				// Get property name, respecting JsonProperty attribute
				listOfSegments.Add(GetPropertyNameFromMemberExpression(memberExpression, options));
				return listOfSegments;

			default:
                throw new InvalidOperationException($"type of expression not supported {expr}");
		}
	}

	private static string GetPropertyNameFromMemberExpression(MemberExpression memberExpression, JsonSerializerOptions options)
	{
		var jsonPropertyNameAttr = memberExpression.Member.GetCustomAttribute<JsonPropertyNameAttribute>();

		if (jsonPropertyNameAttr != null && !string.IsNullOrEmpty(jsonPropertyNameAttr.Name))
		{
			return jsonPropertyNameAttr.Name;
		}

		var memberName = memberExpression.Member.Name;

		if (options.PropertyNamingPolicy != null)
		{
			return options.PropertyNamingPolicy.ConvertName(memberName);
		}

		return memberName;
	}


	// Evaluates the value of the key or index which may be an int or a string,
	// or some other expression type.
	// The expression is converted to a delegate and the result of executing the delegate is returned as a string.
	private static string EvaluateExpression(Expression expression)
	{
		var converted = Expression.Convert(expression, typeof(object));
		var fakeParameter = Expression.Parameter(typeof(object), null);
		var lambda = Expression.Lambda<Func<object?, object>>(converted, fakeParameter);
		var func = lambda.Compile();

		return Convert.ToString(func(null), CultureInfo.InvariantCulture) ?? "";
	}
}
