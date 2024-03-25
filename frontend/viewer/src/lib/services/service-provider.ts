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
    console.log('set-service', key, service);
    this.validateServiceKey(key);
    this.services[key] = service;
  }

  public getService<T>(key: string): T {
    console.log('get-service', key, this.services[key]);
    this.validateServiceKey(key);
    return this.services[key] as T;
  }

  private validateServiceKey(key: string): void {
    if (!SERVICE_KEYS.includes(key as LexboxServices)) {
      throw new Error(`Invalid service key: ${key}. Valid vales are: ${SERVICE_KEYS.join(', ')}`);
    }
  }
}
if (!window.lexbox) { window.lexbox = { ServiceProvider: new LexboxServiceProvider()}; }
else window.lexbox.ServiceProvider = new LexboxServiceProvider();
