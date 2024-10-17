/* eslint-disable */
import type { DotNet } from '@microsoft/dotnet-js-interop';
import type { LexboxApiClient, LexboxApiMetadata } from './lexbox-api';
import { LexboxService } from './service-provider';

declare global {
  interface Lexbox {
    DotNetServiceProvider?: typeof DotNetServiceProvider;
  }
}

export class DotNetServiceProvider {
  static setLexboxApi(service: DotNet.DotNetObject) {
    const dotNetProxy = new Proxy(service, {
      get(target: DotNet.DotNetObject, prop: string) {
        if (prop === 'SupportedFeatures' satisfies keyof LexboxApiMetadata) {
          return () => {
            return {
              history: true,
              write: true,
            };
          }
        }
        return function (...args: any[]) {
          return target.invokeMethodAsync(prop, ...args);
        };
      },
    }) as unknown as LexboxApiClient;
    globalThis.window.lexbox.ServiceProvider.setService(LexboxService.LexboxApi, dotNetProxy);
  }
}

globalThis.window.lexbox.DotNetServiceProvider = DotNetServiceProvider;
