import type { Get, IfNever } from 'type-fest';

/**
 * Removes properties with value `never` from an object type
 */
type OmitNever<T> = { [K in keyof T as IfNever<T[K], never, K>]: T[K] };

/**
 * Creates a union of all possible deep paths / nested keys (e.g. `obj.nestedObj.prop`) of an object
 */
export type DeepPaths<ObjectType extends object> =
  {
    [Key in keyof ObjectType & (string)]: ObjectType[Key] extends object
    ? `${Key}` | `${Key}.${DeepPaths<ObjectType[Key]>}`
    : `${Key}`
  }[keyof ObjectType & (string)];

/**
 * Create a union of all possible deep paths of an object who's value fulfill `Condition`
 */
export type DeepPathsToType<Base, Path extends string, Type> = keyof OmitNever<{
  [Property in Path]: Get<Base, Property> extends Type ? Property : never;
}>;

/**
 * Create a union of all possible deep paths of an object who's value type is `string`
 */
export type DeepPathsToString<Shape extends object> = DeepPathsToType<Shape, DeepPaths<Shape>, string>;
