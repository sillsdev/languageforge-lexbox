namespace LcmCrdt;

/// <summary>
/// Marker type only — no members. Reinforced.Typings (via <c>ChangeTypesCodeGenerator</c>) emits the
/// <c>ChangeType</c> string-literal union and <c>knownChangeTypes</c> array into ChangeTypes.ts from the
/// registered change types, so the frontend has a generated, exhaustive list of change <c>$type</c> values.
/// </summary>
public sealed class ChangeTypes;
