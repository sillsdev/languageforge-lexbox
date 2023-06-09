import { ProjectRole } from '$lib/gql/types';

export function toProjectRoleEnum(index: number): ProjectRole {
  return toEnum(PROJECT_ROLES, index);
}

const PROJECT_ROLES = {
  0: ProjectRole.Editor,
  2: ProjectRole.Manager,
  3: ProjectRole.Editor,
} as const;

function toEnum<T, K extends string | number | symbol>(_enum: Record<K, T>, key: K): T {
  const value = _enum[key];
  if (value === undefined) {
    throw new RangeError(`Enum key out of bounds: ${String(key)} (${Object.keys(_enum).join()}).`);
  }
  return value;
}
