import {getHubProxyFactory, getReceiverRegister} from '../generated-signalr-client/TypedSignalR.Client';

import {type HubConnection, HubConnectionBuilder, HubConnectionState} from '@microsoft/signalr';
import type { LexboxApiFeatures, LexboxApiMetadata } from './lexbox-api';
import {LexboxService} from './service-provider';
import type {ILexboxClient} from '../generated-signalr-client/TypedSignalR.Client/Lexbox.ClientServer.Hubs';
import {onDestroy} from 'svelte';
import {type Writable, writable} from 'svelte/store';
import {AppNotification} from '../notifications/notifications';

type ErrorContext = {error: Error|unknown, methodName?: string, origin: 'method'|'connection'};
type ErrorHandler = (errorContext: ErrorContext) => {handled: boolean};
export function SetupSignalR(url: string,
                             features: LexboxApiFeatures,
                             client: ILexboxClient | null = null,
                             onError?: ErrorHandler) {
  let {connection, connected} = setupConnection(url, errorContext => {
    if (onError && onError(errorContext).handled) {
      return {handled: true};
    }
    if (errorContext.error instanceof Error) {
      let message = errorContext.error.message;
      AppNotification.display('Connection error: ' + message, 'error', 'long');
    } else {
      AppNotification.display('Unknown Connection error', 'error', 'long');
    }
    return {handled: true};
  });
  const hubFactory = getHubProxyFactory('ILexboxApiHub');
  const hubProxy = hubFactory.createHubProxy(connection);

  const lexboxApiHubProxy = Object.assign(hubProxy, {
    SupportedFeatures(): LexboxApiFeatures {
      return features;
    }
  } satisfies LexboxApiMetadata);
  if (client) {
    getReceiverRegister('ILexboxClient').register(connection, client);
  }
  window.lexbox.ServiceProvider.setService(LexboxService.LexboxApi, lexboxApiHubProxy);
  return {connected, lexboxApi: lexboxApiHubProxy};
}

function setupConnection(url: string, onError: ErrorHandler): {connection: HubConnection, connected: Writable<boolean>} {
  const connected = writable(false)
  let connection = new HubConnectionBuilder()
    .withUrl(url)
    .withAutomaticReconnect()
    .build();
  onDestroy(() => connection.stop());
  connection.onclose((error) => {
    connected.set(false);
    if (!error) return;
    console.error('Connection closed', error);
    onError({error, origin: 'connection'});
  });
  void connection.start()
    .then(() => connected.set(connection.state == HubConnectionState.Connected))
    .catch(err => {
      onError({error: err, origin: 'connection'});
      console.error('error connecting to signalr', err);
    });
    connection = new Proxy(connection, {
      get(target, prop: keyof HubConnection, receiver) {
        if (prop === 'invoke') {
          return async (methodName: string, ...args: any[]) => {
            try {
              return await target.invoke(methodName, ...args);
            } catch (e) {
              onError({error: e, methodName, origin: 'method'});
              throw e;
            }
          }
        } else {
          return Reflect.get(target, prop, receiver);
        }
      }
    }) as HubConnection;
  return {connection, connected};
}
