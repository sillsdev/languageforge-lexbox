// Namespace is LcmCrdt (not FwLiteShared.TypeGen) on purpose: Reinforced.Typings derives the generated
// file's folder from the type's namespace and the file name from the type name, so this places the output
// at generated-types/LcmCrdt/ChangeType.ts. Named to match the emitted union so properties can reference it.
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace LcmCrdt;
#pragma warning restore IDE0130 // Namespace does not match folder structure


/// <summary>
/// Marker type only — no members. Reinforced.Typings (via <c>ChangeTypesCodeGenerator</c>) replaces it with a
/// <c>ChangeType</c> string-literal union plus a <c>knownChangeTypes</c> array built from the registered change
/// types, so the frontend has a generated, exhaustive list of change <c>$type</c> values.
/// </summary>
internal sealed class ChangeType;
