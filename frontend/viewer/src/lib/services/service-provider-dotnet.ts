import type { DotNet } from '@microsoft/dotnet-js-interop';
import { LexboxServiceProvider } from './service-provider';

declare global {
  interface Lexbox {
    DotNetServiceProvider?: typeof DotNetServiceProvider;
  }
}

export class DotNetServiceProvider {
  static services: Record<string, DotNet.DotNetObject> = {};

  static setService(key: string, service: DotNet.DotNetObject) {
    const dotNetProxy = new Proxy(service, {
      get(target: DotNet.DotNetObject, prop: string,) {
        return function (...args: any[]) {
          return target.invokeMethodAsync(prop, ...args);
        };
      },
    });
    LexboxServiceProvider.setService(key, dotNetProxy);
  }
}

globalThis.window.lexbox.DotNetServiceProvider = DotNetServiceProvider;
