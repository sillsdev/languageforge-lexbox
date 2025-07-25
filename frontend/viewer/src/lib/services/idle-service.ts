import {IsIdle} from 'runed';
import {getContext, hasContext, setContext} from 'svelte';

const symbol = Symbol.for('fw-lite-idle-service');
const DEFAULT_IDLE_TIMEOUT_MS = 5 * 60 * 1000;

export function useIdleService(): IdleService {
  if (hasContext(symbol)) return getContext(symbol);
  const idleService = new IdleService();
  setContext(symbol, idleService);
  return idleService;
}

export class IdleService {
  constructor(timeoutMs?: number) {
    timeoutMs ??= DEFAULT_IDLE_TIMEOUT_MS;
    this.#isIdle = new IsIdle({ timeout: timeoutMs });
  }

  #isIdle: IsIdle;

  get isIdle() { return this.#isIdle.current; }
}
