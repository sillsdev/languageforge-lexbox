/* eslint-disable @typescript-eslint/naming-convention */
import {getHubProxyFactory, getReceiverRegister} from '../generated-signalr-client/TypedSignalR.Client';

import {
  type HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  type IHttpConnectionOptions
} from '@microsoft/signalr';
import type {LexboxApiClient} from './lexbox-api';
import {onDestroy} from 'svelte';
import {type Readable, type Writable, writable} from 'svelte/store';
import {AppNotification} from '../notifications/notifications';
import type {
  CloseReason,
} from '../generated-signalr-client/TypedSignalR.Client/Lexbox.ClientServer.Hubs';
import {useEventBus} from './event-bus';
import {type IMiniLcmFeatures, type IEntry, type IMiniLcmJsInvokable} from '$lib/dotnet-types';
import {initProjectContext} from '$lib/project-context.svelte';

// eslint-disable-next-line @typescript-eslint/no-redundant-type-constituents
type ErrorContext = {error: Error|unknown, methodName?: string, origin: 'method'|'connection'};
type ErrorHandler = (errorContext: ErrorContext) => {handled: boolean};
export function SetupSignalR(
  url: string,
  features: IMiniLcmFeatures,
  onError?: ErrorHandler,
  options: IHttpConnectionOptions = {}) : { connected: Readable<boolean>, lexboxApi: LexboxApiClient } {
  const {connection, connected} = setupConnection(url, options, errorContext => {
    if (onError && onError(errorContext).handled) {
      return {handled: true};
    }
    if (errorContext.error instanceof Error) {
      const message = errorContext.error.message;
      AppNotification.display('Connection error: ' + message, 'error', 'long');
    } else {
      AppNotification.display('Unknown Connection error', 'error', 'long');
    }
    return {handled: true};
  });
  const hubFactory = getHubProxyFactory('ILexboxApiHub');
  const hubProxy = hubFactory.createHubProxy(connection);

  const lexboxApiHubProxy = Object.assign(hubProxy, {
    supportedFeatures(): Promise<IMiniLcmFeatures> {
      return Promise.resolve(features);
    }
  } satisfies Partial<IMiniLcmJsInvokable>);
  const changeEventBus = useEventBus();
  getReceiverRegister('ILexboxClient').register(connection, {
    OnProjectClosed(reason: CloseReason): Promise<void> {
      changeEventBus.notifyProjectClosed(reason);
      return Promise.resolve();
    },
    OnEntryUpdated(entry: IEntry): Promise<void> {
      changeEventBus.notifyEntryUpdated(entry);
      return Promise.resolve();
    }
  });
  initProjectContext({api: lexboxApiHubProxy, projectName: 'todo-change-me'});
  return {connected, lexboxApi: lexboxApiHubProxy};
}

function setupConnection(url: string, options: IHttpConnectionOptions, onError: ErrorHandler): {connection: HubConnection, connected: Writable<boolean>} {
  const connected = writable(false)
  let connection = new HubConnectionBuilder()
    .withUrl(url, options)
    .withAutomaticReconnect()
    .build();

  if (import.meta.env.DEV) connection.serverTimeoutInMilliseconds = 60_000 * 10;
  console.debug('SignalR connection timeout', connection.serverTimeoutInMilliseconds);

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
          /* eslint-disable  @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-return, @typescript-eslint/no-unsafe-argument */
          return async (methodName: string, ...args: any[]) => {
            try {
              return await target.invoke(methodName, ...args);
            } catch (e) {
              onError({error: e, methodName, origin: 'method'});
              throw e;
            }
          }
          /* eslint-enable */
        } else {
          return Reflect.get(target, prop, receiver);
        }
      }
    });
  return {connection, connected};
}
