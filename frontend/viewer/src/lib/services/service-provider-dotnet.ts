/* eslint-disable @typescript-eslint/naming-convention */
import './service-declaration';
import  { DotNet } from '@microsoft/dotnet-js-interop';
import {type LexboxServiceRegistry, SERVICE_KEYS, type ServiceKey} from './service-provider';
export class DotNetServiceProvider {
  private services: LexboxServiceRegistry;

  constructor() {
    this.services = globalThis.window.lexbox.FwLiteProvider ?? ({} as LexboxServiceRegistry);
  }

  public async setOverrideServices(fwLiteProvider: LexboxServiceRegistry) {
    this.services = fwLiteProvider;
  }

  public hasService(key: ServiceKey): boolean {
    return !!this.services[key];
  }

  public getService<K extends ServiceKey>(key: K): LexboxServiceRegistry[K] | undefined {
    this.validateAllServices();
    const service = this.services[key] as LexboxServiceRegistry[K] | DotNet.DotNetObject | undefined;
    //todo maybe don't return undefined
    if (!service) return undefined;
    if (this.isDotnetObject(service)) return wrapInProxy(service) as LexboxServiceRegistry[K];
    return service;
  }

  private isDotnetObject(service: object): service is DotNet.DotNetObject {
    return service instanceof DotNet.DotNetObject || 'invokeMethodAsync' in service;
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

function wrapInProxy(dotnetObject: DotNet.DotNetObject): unknown {
  return new Proxy(dotnetObject, {
    get(target: DotNet.DotNetObject, prop: string) {
      const dotnetMethodName = uppercaseFirstLetter(prop);
      return function (...args: unknown[]) {
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
    // @ts-expect-error this is only partially what's required here
    globalThis.window.lexbox = lexbox;
  }
}
