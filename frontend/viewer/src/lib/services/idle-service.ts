import {IsIdle} from 'runed';
import {getContext, hasContext, setContext} from 'svelte';

const symbol = Symbol.for('fw-lite-idle-service');
const DEFAULT_IDLE_TIMEOUT_MS = 5 * 60 * 1000;

export function useIdleService(timeout?: number): IdleService {
  if (hasContext(symbol)) return getContext(symbol);
  // Note that only the first call to useIdleService() can set the timeout.
  const service = new IdleService(timeout);
  setContext(symbol, service);
  return service;
}

export class IdleService {
  constructor(timeoutMs?: number) {
    this.#isIdle = new IsIdle({ timeout: timeoutMs ?? DEFAULT_IDLE_TIMEOUT_MS });
  }

  #isIdle: IsIdle;

  get isIdle() { return this.#isIdle.current; }
}
