import {getHubProxyFactory, getReceiverRegister} from '../generated-signalr-client/TypedSignalR.Client';

import type { Entry } from '../mini-lcm';
import type { HubConnection } from '@microsoft/signalr';
import type { LexboxApiFeatures, LexboxApiMetadata } from './lexbox-api';
import {LexboxService} from './service-provider';

const noop = () => Promise.resolve();
export function SetupSignalR(connection: HubConnection, features: LexboxApiFeatures, onProjectClosed: () => Promise<void> = noop) {
  const hubFactory = getHubProxyFactory('ILexboxApiHub');
  const hubProxy = hubFactory.createHubProxy(connection);

  const lexboxApiHubProxy = Object.assign(hubProxy, {
    SupportedFeatures(): LexboxApiFeatures {
      return features;
    }
  } satisfies LexboxApiMetadata);
  getReceiverRegister('ILexboxClient').register(connection, {
      OnEntryUpdated: async (entry: Entry) => {
          console.log('OnEntryUpdated', entry);
      },
      OnProjectClosed: onProjectClosed
  });
  window.lexbox.ServiceProvider.setService(LexboxService.LexboxApi, lexboxApiHubProxy);
}
