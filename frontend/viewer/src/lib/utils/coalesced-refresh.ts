/**
 * Runs `refresh` at most once at a time. A call while one is in flight joins it, so an older, slower
 * response can never land after a newer one.
 * `forceFresh` is for callers that know the data changed after the in-flight call started (e.g. a sync
 * just finished): it queues one follow-up run and resolves once that fresh run is done.
 */
export class CoalescedRefresh {
  #pending: Promise<void> | undefined;
  #queued: Promise<void> | undefined;

  constructor(private readonly refresh: () => Promise<void>) {}

  run(opts?: {forceFresh?: boolean}): Promise<void> {
    if (!this.#pending) return this.#start();
    if (!opts?.forceFresh) return this.#queued ?? this.#pending;
    const startQueued = () => {
      this.#queued = undefined;
      return this.#start();
    };
    // The in-flight call's own awaiters still see its error; the follow-up runs either way
    return this.#queued ??= this.#pending.then(startQueued, startQueued);
  }

  #start(): Promise<void> {
    const pending = this.refresh().finally(() => {
      if (this.#pending === pending) this.#pending = undefined;
    });
    this.#pending = pending;
    return pending;
  }
}
