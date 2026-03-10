import type {ResourceReturn} from 'runed';

export class LazyResource<T, RefetchInfo = unknown> implements ResourceReturn<T, RefetchInfo, true> {
  #active = false;

  constructor(private res: ResourceReturn<T, RefetchInfo, true>, private onActivate: () => void) {
  }

  get isActive(): boolean {
    return this.#active;
  }

  #activate() {
    if (this.#active) return;
    this.#active = true;
    this.onActivate();
  }

  get current(): T {
    this.#activate();
    return this.res.current;
  }

  get loading(): boolean {
    return this.res.loading;
  }

  get error(): Error | undefined {
    return this.res.error;
  }

  mutate(value: T) {
    this.res.mutate(value);
  }

  refetch(info?: RefetchInfo): Promise<T | undefined> {
    this.#activate();
    return this.res.refetch(info);
  }
}
