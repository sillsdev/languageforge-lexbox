using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LexData.EntityIds;

public class LfIdValueConverterSelector: ValueConverterSelector
{
    public LfIdValueConverterSelector(ValueConverterSelectorDependencies dependencies) : base(dependencies)
    {
    }

    public override IEnumerable<ValueConverterInfo> Select(Type modelClrType, Type? providerClrType = null)
    {
        foreach (var converter in base.Select(modelClrType, providerClrType))
        {
            yield return converter;
        }

        if (LfId.IsTypeLfId(modelClrType) && (providerClrType == typeof(Guid) || providerClrType is null))
        {
            var converter = MakeLfIdConverter(modelClrType);
            yield return new ValueConverterInfo(
                modelClrType,
                typeof(Guid),
                info => converter
            );
        }
    }

    private static readonly MethodInfo MakeLfIdConverterMethodInfo =
        new Func<ValueConverter>(MakeLfIdConverter<object>).Method.GetGenericMethodDefinition();

    private static ValueConverter MakeLfIdConverter(Type idType)
    {
        var entityType = LfId.GetEntityType(idType);
        var converter = MakeLfIdConverterMethodInfo.MakeGenericMethod(entityType).Invoke(null, null);
        return converter as ValueConverter ?? throw new Exception("Unable to make lf id converter for id type " + idType);
    }

    private static ValueConverter MakeLfIdConverter<TEntity>()
    {
        return new LfIdValueConverter<TEntity>();
    }
}