/* eslint-disable */
import  { type DotNet } from '@microsoft/dotnet-js-interop';
import {LexboxService, type LexboxServiceRegistry} from './service-provider';

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

  public getService<K extends LexboxService>(key: K): LexboxServiceRegistry[K] {
    this.validateAllServices();
    return wrapInProxy(this.services[key] as unknown as DotNet.DotNetObject) as LexboxServiceRegistry[K];
  }
  private validateAllServices() {
    const serviceKeys = Object.keys(this.services);
    const validServiceKeys = Object.values(LexboxService);
    for (const key of serviceKeys) {
      if (!validServiceKeys.includes(key as LexboxService)) {
        throw new Error(`Invalid service key: ${key}. Valid values are: ${validServiceKeys.join(', ')}`);
      }
    }
  }
}

function wrapInProxy(dotnetObject: DotNet.DotNetObject): any {
  return new Proxy(dotnetObject, {
    get(target: DotNet.DotNetObject, prop: string) {
      return function (...args: any[]) {
        return target.invokeMethodAsync(prop, ...args);
      };
    },
  });
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
