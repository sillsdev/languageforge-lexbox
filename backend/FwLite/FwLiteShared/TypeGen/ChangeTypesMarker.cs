// Namespace is LcmCrdt (not FwLiteShared.TypeGen) on purpose: Reinforced.Typings derives the generated
// file's path from the type's namespace, so this keeps the output at generated-types/LcmCrdt/ChangeTypes.ts.
namespace LcmCrdt;

/// <summary>
/// Marker type only — no members. Reinforced.Typings (via <c>ChangeTypesCodeGenerator</c>) emits the
/// <c>ChangeType</c> string-literal union and <c>knownChangeTypes</c> array into ChangeTypes.ts from the
/// registered change types, so the frontend has a generated, exhaustive list of change <c>$type</c> values.
/// </summary>
internal sealed class ChangeTypes;
