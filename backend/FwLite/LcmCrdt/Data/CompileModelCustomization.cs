
using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Db;
using SIL.Harmony.Db.EntityConfig;

namespace LcmCrdt.CompiledModels;

partial class ChangeEntityEntityType
{
    static partial void Customize(RuntimeEntityType runtimeEntityType)
    {
        var changeProperty = runtimeEntityType.FindProperty(nameof(ChangeEntity<IChange>.Change));
        if (changeProperty == null)
        {
            throw new InvalidOperationException($"Change property not found on {nameof(ChangeEntity<IChange>)}");
        }

        var config = LcmCrdtKernel.MakeConfig();
        var jsonSerializerOptions = config.JsonSerializerOptions;
        jsonSerializerOptions.TypeInfoResolver = JsonSourceGenerationContext.Default.WithAddedModifier(config.MakeJsonTypeModifier());
        changeProperty.TypeMapping = changeProperty.TypeMapping.Clone(
            converter: new ValueConverter<IChange, string>(
                (IChange change) => ChangeEntityConfig.SerializeChange(change, jsonSerializerOptions),
                (string json) => ChangeEntityConfig.DeserializeChange(json,
                    jsonSerializerOptions)),
            jsonValueReaderWriter: new JsonConvertedValueReaderWriter<IChange, string>(
                JsonStringReaderWriter.Instance,
                new ValueConverter<IChange, string>(
                    (IChange change) => ChangeEntityConfig.SerializeChange(change,
                        jsonSerializerOptions),
                    (string json) => ChangeEntityConfig.DeserializeChange(json,
                        jsonSerializerOptions)))
        );
    }
}
partial class ObjectSnapshotEntityType
{
    static partial void Customize(RuntimeEntityType runtimeEntityType)
    {
        var entityProperty = runtimeEntityType.FindProperty(nameof(ObjectSnapshot.Entity));
        if (entityProperty == null)
        {
            throw new InvalidOperationException($"Entity property not found on {nameof(ObjectSnapshot)}");
        }

        var config = LcmCrdtKernel.MakeConfig();
        var jsonSerializerOptions = config.JsonSerializerOptions;
        jsonSerializerOptions.TypeInfoResolver =
            JsonSourceGenerationContext.Default.WithAddedModifier(config.MakeJsonTypeModifier());
        entityProperty.TypeMapping = entityProperty.TypeMapping.Clone(
            converter: new ValueConverter<IObjectBase, string>(
                (IObjectBase obj) => SnapshotEntityConfig.Serialize(obj, jsonSerializerOptions),
                (string json) => SnapshotEntityConfig.DeserializeObject(json,
                    jsonSerializerOptions)),
            jsonValueReaderWriter: new JsonConvertedValueReaderWriter<IObjectBase, string>(
                JsonStringReaderWriter.Instance,
                new ValueConverter<IObjectBase, string>(
                    (IObjectBase obj) => SnapshotEntityConfig.Serialize(obj,
                        jsonSerializerOptions),
                    (string json) => SnapshotEntityConfig.DeserializeObject(json,
                        jsonSerializerOptions)))
        );

        var referenceProperty = runtimeEntityType.FindProperty(nameof(ObjectSnapshot.References));
        if (referenceProperty == null)
        {
            throw new InvalidOperationException($"References property not found on {nameof(ObjectSnapshot)}");
        }

        var clrType = referenceProperty.TypeMapping.ElementTypeMapping?.ClrType;
        if (clrType == null)
        {
            throw new InvalidOperationException($"Element type mapping not found on {nameof(ObjectSnapshot.References)}");
        }
        referenceProperty.SetElementType(clrType, typeMapping: referenceProperty.TypeMapping.ElementTypeMapping, primitiveCollection: true);
    }
}
