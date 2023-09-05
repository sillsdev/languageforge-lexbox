export const enum Duration {
  Default = 5000,
  Medium = 10000,
  Long = 15000,
}

export async function delay<T>(ms = Duration.Default): Promise<T> {
  return new Promise<T>(resolve => setTimeout(resolve, ms));
}
