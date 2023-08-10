export async function delay<T>(action: () => T, ms?: number): Promise<T> {
  return new Promise<T>(resolve => setTimeout(() => {
    Promise.resolve(action()).then(resolve).catch(console.error);
  }, ms));
}
