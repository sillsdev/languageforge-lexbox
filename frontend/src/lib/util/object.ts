export function pick<T extends object, K extends keyof T>(obj: T, keys: Iterable<K>): Pick<T, K> {
  const filteredObj: Partial<T> = {};
  for (const key of keys) {
    filteredObj[key] = obj[key];
  }
  return filteredObj as Pick<T, K>;
}
