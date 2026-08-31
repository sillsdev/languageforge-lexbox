// jsdom under Vitest 4 + Node's experimental Web Storage leaves `localStorage`
// undefined on the global (Node exposes `sessionStorage` but not `localStorage`
// without --localstorage-file, and jsdom doesn't install its own). Provide the
// same in-memory Storage jsdom is meant to supply so unit tests that touch
// `localStorage` behave like the browser.
class InMemoryStorage implements Storage {
  #store = new Map<string, string>();

  get length(): number {
    return this.#store.size;
  }

  clear(): void {
    this.#store.clear();
  }

  getItem(key: string): string | null {
    return this.#store.has(key) ? this.#store.get(key)! : null;
  }

  key(index: number): string | null {
    return [...this.#store.keys()][index] ?? null;
  }

  removeItem(key: string): void {
    this.#store.delete(key);
  }

  setItem(key: string, value: string): void {
    this.#store.set(key, String(value));
  }
}

if (typeof globalThis.localStorage === 'undefined') {
  Object.defineProperty(globalThis, 'localStorage', {
    configurable: true,
    value: new InMemoryStorage(),
  });
}
