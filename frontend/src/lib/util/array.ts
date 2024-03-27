/**
 * @param values any number of arrays, values or undefined's
 * @returns a single array containing any/all the defined values from the inputs
 */
export function concatAll<T>(...values: (T | T[] | undefined)[]): T[] {
  const mergedResult = [];
  for (const value of values) {
    if (value === undefined) continue;
    mergedResult.push(...(Array.isArray(value) ? value : [value]));
  }
  return mergedResult;
}

// https://stackoverflow.com/a/14438954/2301416
export function distinct(value: unknown, index: number, array: unknown[]): boolean {
  return array.indexOf(value) === index;
}
