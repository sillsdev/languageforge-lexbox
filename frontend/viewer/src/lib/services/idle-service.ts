import {IsIdle} from 'runed';
import {createContext} from 'svelte';

const [getIdleService, setIdleService] = createContext<IdleService>();
const IDLE_TIMEOUT_MS = 5 * 60 * 1000;

export function useIdleService(): IdleService {
  const existingService = getIdleService();
  if (existingService) return existingService;
  const service = new IdleService();
  setIdleService(service);
  return service;
}

export class IdleService {
  constructor() {
    this.#isIdle = new IsIdle({ timeout: IDLE_TIMEOUT_MS });
  }

  #isIdle: IsIdle;

  get isIdle() { return this.#isIdle.current; }
}
