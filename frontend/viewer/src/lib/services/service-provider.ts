declare global {

  interface Lexbox {
    ServiceProvider: typeof LexboxServiceProvider;
  }

  interface Window {
    lexbox: Lexbox;
  }
}

export enum LexboxServices {
  LexboxApi = 'LexboxApi',
}

var SERVICE_KEYS = Object.values(LexboxServices);

export class LexboxServiceProvider {
  static services: Record<string, unknown> = {};

  static setService(key: string, service: unknown) {
    console.log('set-service', key, service);
    this.validateServiceKey(key);
    this.services[key] = service;
  }

  static getService<T>(key: string): T {
    console.log('get-service', key, this.services[key]);
    this.validateServiceKey(key);
    return this.services[key] as T;
  }

  private static validateServiceKey(key: string) {
    if (!SERVICE_KEYS.includes(key as LexboxServices)) {
      throw new Error(`Invalid service key: ${key}. Valid vales are: ${SERVICE_KEYS.join(', ')}`);
    }
  }
}

window.lexbox = {
  ServiceProvider: LexboxServiceProvider,
};
