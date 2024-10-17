import type {LexboxApiClient} from './lexbox-api';

declare global {

  interface Lexbox {
    // eslint-disable-next-line @typescript-eslint/naming-convention
    ServiceProvider: LexboxServiceProvider;
  }

  interface Window {
    lexbox: Lexbox;
  }
}

export enum LexboxService {
  LexboxApi = 'LexboxApi',
}

type LexboxServiceRegistry = {
  [LexboxService.LexboxApi]: LexboxApiClient,
};

const SERVICE_KEYS = Object.values(LexboxService);

export class LexboxServiceProvider {
  private services: LexboxServiceRegistry = {} as LexboxServiceRegistry;

  public setService<K extends LexboxService>(key: K, service: LexboxServiceRegistry[K]): void {
    this.validateServiceKey(key);
    this.services[key] = service;
  }

  public getService<K extends LexboxService>(key: LexboxService): LexboxServiceRegistry[K] {
    this.validateServiceKey(key);
    const service = this.services[key];
    if (!service) throw new Error(`Lexbox service '${key}' not found`);
    return this.services[key];
  }

  private validateServiceKey(key: LexboxService): void {
    if (!SERVICE_KEYS.includes(key)) {
      throw new Error(`Invalid service key: ${key}. Valid values are: ${SERVICE_KEYS.join(', ')}`);
    }
  }
}

if (!window.lexbox) {
  // eslint-disable-next-line @typescript-eslint/naming-convention
  window.lexbox = {ServiceProvider: new LexboxServiceProvider()};
} else window.lexbox.ServiceProvider = new LexboxServiceProvider();

export function useLexboxApi(): LexboxApiClient {
  return window.lexbox.ServiceProvider.getService(LexboxService.LexboxApi);
}
