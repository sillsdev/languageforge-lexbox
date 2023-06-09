/**
 * creates a union of all possible nested keys of an object
 */
export type NestedKeyOf<ObjectType extends object> =
  {
    [Key in keyof ObjectType & (string)]: ObjectType[Key] extends object
    ? `${Key}` | `${Key}.${NestedKeyOf<ObjectType[Key]>}`
    : `${Key}`
  }[keyof ObjectType & (string)];
