import {IsIdle} from 'runed';
import {getContext, hasContext, setContext} from 'svelte';

const symbol = Symbol.for('fw-lite-idle-service');
const IDLE_TIMEOUT_MS = 5 * 60 * 1000;

export function useIdleService(): IdleService {
  if (hasContext(symbol)) return getContext(symbol);
  const service = new IdleService();
  setContext(symbol, service);
  return service;
}

export class IdleService {
  constructor() {
    this.#isIdle = new IsIdle({ timeout: IDLE_TIMEOUT_MS });
  }

  #isIdle: IsIdle;

  get isIdle() { return this.#isIdle.current; }
}
