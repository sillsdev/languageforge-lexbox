import {getHubProxyFactory, getReceiverRegister} from '../generated-signalr-client/TypedSignalR.Client';

import type { HubConnection } from '@microsoft/signalr';
import type { LexboxApiFeatures, LexboxApiMetadata } from './lexbox-api';
import {LexboxService} from './service-provider';
import {
  CloseReason,
  type ILexboxClient
} from '../generated-signalr-client/TypedSignalR.Client/Lexbox.ClientServer.Hubs';
import {useEventBus} from './event-bus';
import {Entry} from '../mini-lcm';

type ErrorContext = {error: Error|unknown, methodName: string};
export function SetupSignalR(connection: HubConnection, features: LexboxApiFeatures, onError?: (context: ErrorContext) => void) {
  const hubFactory = getHubProxyFactory('ILexboxApiHub');
  if (onError) {
    connection = new Proxy(connection, {
      get(target, prop: keyof HubConnection, receiver) {
        if (prop === 'invoke') {
          return async (methodName: string, ...args: any[]) => {
            try {
              return await target.invoke(methodName, ...args);
            } catch (e) {
              onError({error: e, methodName});
              throw e;
            }
          }
        } else {
          return Reflect.get(target, prop, receiver);
        }
      }
    }) as HubConnection;
  }
  const hubProxy = hubFactory.createHubProxy(connection);

  const lexboxApiHubProxy = Object.assign(hubProxy, {
    SupportedFeatures(): LexboxApiFeatures {
      return features;
    }
  } satisfies LexboxApiMetadata);

  const changeEventBus = useEventBus();
  getReceiverRegister('ILexboxClient').register(connection, {
    OnProjectClosed(reason: CloseReason): Promise<void> {
      changeEventBus.notifyProjectClosed(reason);
      return Promise.resolve();
    },
    OnEntryUpdated(entry: Entry): Promise<void> {
      changeEventBus.notifyEntryUpdated(entry);
      return Promise.resolve();
    }
  });
  window.lexbox.ServiceProvider.setService(LexboxService.LexboxApi, lexboxApiHubProxy);
}
