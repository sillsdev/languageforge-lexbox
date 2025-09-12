/* eslint-disable @typescript-eslint/naming-convention */
import './service-declaration';
//do not import as a value, we need to use the object defined on window
import type {DotNet} from '@microsoft/dotnet-js-interop';
import {type LexboxServiceRegistry, SERVICE_KEYS, type ServiceKey} from './service-provider';

declare global {
  interface Window {
    DotNet: typeof DotNet;
  }
}

export class DotNetServiceProvider {
  private services: LexboxServiceRegistry;

  constructor() {
    this.services = globalThis.window.lexbox.FwLiteProvider ?? ({} as LexboxServiceRegistry);
  }

  public setOverrideServices(fwLiteProvider: LexboxServiceRegistry) {
    this.services = fwLiteProvider;
  }

  public hasService(key: ServiceKey): boolean {
    return !!this.services[key];
  }

  public removeService(key: ServiceKey): void {
    delete this.services[key];
  }

  public getService<K extends ServiceKey>(key: K): LexboxServiceRegistry[K] | undefined {
    this.validateAllServices();
    const service = this.services[key] as LexboxServiceRegistry[K] | DotNet.DotNetObject | undefined;
    //todo maybe don't return undefined
    if (!service) return undefined;
    if (this.isDotnetObject(service)) return wrapInProxy(service, key);
    return service;
  }

  private isDotnetObject(service: object): service is DotNet.DotNetObject {
    return service instanceof window.DotNet.DotNetObject || 'invokeMethodAsync' in service;
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

export function wrapInProxy<K extends ServiceKey>(dotnetObject: DotNet.DotNetObject, serviceName: K): LexboxServiceRegistry[K] {
  return new Proxy(dotnetObject, {
    get(target: DotNet.DotNetObject, prop: unknown) {
      if (typeof prop !== 'string') return undefined;
      //runed resource calls stringify on values to check equality, so we don't want to pass the toJSON call through to the backend
      if (prop === 'toJSON') return undefined;
      //called to check if it's a promise or not
      if (prop === 'then') return undefined;
      const dotnetMethodName = uppercaseFirstLetter(prop);
      return async function proxyHandler(...args: unknown[]) {
        console.debug(`[Dotnet Proxy] Calling ${serviceName} method ${dotnetMethodName}`, args);
        args = transformArgs(args);
        const result = await target.invokeMethodAsync(dotnetMethodName, ...args);
        console.debug(`[Dotnet Proxy] ${serviceName} method ${dotnetMethodName} returned`, result);
        return result;
      };
    },
  }) as unknown as LexboxServiceRegistry[K];
}

function uppercaseFirstLetter(str: string): string {
  return str.charAt(0).toUpperCase() + str.slice(1);
}

function transformArgs(args: unknown[]): unknown[] {
  return args.map(arg => {
    return transformBlob(arg);
  });
}

function transformBlob(result: unknown): unknown {
  if (result instanceof Blob) return window.DotNet.createJSStreamReference(result);
  if (result instanceof ArrayBuffer) return window.DotNet.createJSStreamReference(result);
  return result;
}

export function setupDotnetServiceProvider() {
  if (globalThis.window.lexbox?.DotNetServiceProvider) return;
  const lexbox = {DotNetServiceProvider: new DotNetServiceProvider()};
  if (globalThis.window.lexbox) {
    globalThis.window.lexbox = {...globalThis.window.lexbox, ...lexbox};
  } else {
    // @ts-expect-error this is only partially what's required here
    globalThis.window.lexbox = lexbox;
  }
}
