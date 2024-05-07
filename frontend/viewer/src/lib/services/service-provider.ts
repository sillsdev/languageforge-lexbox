import {InMemoryApiService} from '../in-memory-api-service';
import type {LexboxApi} from './lexbox-api';

declare global {

  interface Lexbox {
    ServiceProvider: LexboxServiceProvider;
  }

  interface Window {
    lexbox: Lexbox;
  }
}

export enum LexboxServices {
  LexboxApi = 'LexboxApi',
}

const SERVICE_KEYS = Object.values(LexboxServices);

export class LexboxServiceProvider {
  private services: Record<string, unknown> = {};

  public setService(key: string, service: unknown): void {
    this.validateServiceKey(key);
    this.services[key] = service;
  }

  public getService<T>(key: string): T {
    this.validateServiceKey(key);
    return this.services[key] as T;
  }

  private validateServiceKey(key: string): void {
    if (!SERVICE_KEYS.includes(key as LexboxServices)) {
      throw new Error(`Invalid service key: ${key}. Valid vales are: ${SERVICE_KEYS.join(', ')}`);
    }
  }
}

if (!window.lexbox) {
  window.lexbox = {ServiceProvider: new LexboxServiceProvider()};
} else window.lexbox.ServiceProvider = new LexboxServiceProvider();

export function useLexboxApi() {
  let api = window.lexbox.ServiceProvider.getService<LexboxApi>(LexboxServices.LexboxApi);
  if (!api) {
    throw new Error('LexboxApi service not found');
  }
  return api;
}
