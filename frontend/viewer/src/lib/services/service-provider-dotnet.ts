/* eslint-disable */
import  { type DotNet } from '@microsoft/dotnet-js-interop';
import {LexboxService, type LexboxServiceRegistry, SERVICE_KEYS, type ServiceKey} from './service-provider';

declare global {
  interface Lexbox {
    DotNetServiceProvider?: DotNetServiceProvider;
    FwLiteProvider?: LexboxServiceRegistry;
  }
}
export class DotNetServiceProvider {
  private services: LexboxServiceRegistry;

  constructor() {
    this.services = globalThis.window.lexbox.FwLiteProvider ?? ({} as LexboxServiceRegistry);
  }

  public async setOverrideServices(fwLiteProvider: LexboxServiceRegistry) {
    this.services = fwLiteProvider;
  }

  public getService<K extends ServiceKey>(key: K): LexboxServiceRegistry[K] {
    this.validateAllServices();
    let service = this.services[key] as unknown as DotNet.DotNetObject;
    if (!service) throw new Error(`Service ${key} is null`);
    return wrapInProxy(service) as LexboxServiceRegistry[K];
  }
  private validateAllServices() {
    const validServiceKeys = SERVICE_KEYS;
    for (const [key, value] of Object.entries(this.services)) {
      if (!value) throw new Error(`Service ${key} is null`);
      if (!validServiceKeys.includes(key as ServiceKey)) {
        throw new Error(`Invalid service key: ${key}. Valid values are: ${validServiceKeys.join(', ')}`);
      }
    }
  }
}

function wrapInProxy(dotnetObject: DotNet.DotNetObject): any {
  return new Proxy(dotnetObject, {
    get(target: DotNet.DotNetObject, prop: string) {
      let dotnetMethodName = uppercaseFirstLetter(prop);
      return function (...args: any[]) {
        return target.invokeMethodAsync(dotnetMethodName, ...args);
      };
    },
  });
}

function uppercaseFirstLetter(str: string): string {
  return str.charAt(0).toUpperCase() + str.slice(1);
}

export function setupDotnetServiceProvider() {
  const lexbox = {DotNetServiceProvider: new DotNetServiceProvider()};
  if (globalThis.window.lexbox) {
    globalThis.window.lexbox = {...globalThis.window.lexbox, ...lexbox};
  } else {
    // @ts-ignore
    globalThis.window.lexbox = lexbox;
  }
}
