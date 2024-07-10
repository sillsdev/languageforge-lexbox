import {getHubProxyFactory, getReceiverRegister} from '../generated-signalr-client/TypedSignalR.Client';

import type { HubConnection } from '@microsoft/signalr';
import type { LexboxApiFeatures, LexboxApiMetadata } from './lexbox-api';
import {LexboxService} from './service-provider';
import type {ILexboxClient} from '../generated-signalr-client/TypedSignalR.Client/Lexbox.ClientServer.Hubs';


export function SetupSignalR(connection: HubConnection, features: LexboxApiFeatures, client: ILexboxClient | null = null) {
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
}
