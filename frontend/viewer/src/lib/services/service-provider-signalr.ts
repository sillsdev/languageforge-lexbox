import type { HubConnection } from '@microsoft/signalr';
import {getHubProxyFactory, getReceiverRegister} from '../generated-signalr-client/TypedSignalR.Client';
import type { LexboxApi } from './lexbox-api';
import {LexboxServices} from './service-provider';
import type { Entry } from '../mini-lcm';

export function SetupSignalR(connection: HubConnection) {
    const hubFactory = getHubProxyFactory('ILexboxApiHub');
    const hubProxy = hubFactory.createHubProxy(connection);
    getReceiverRegister('ILexboxClient').register(connection, {
        OnEntryUpdated: async (entry: Entry) => {
            console.log('OnEntryUpdated', entry);
        }
    });
    window.lexbox.ServiceProvider.setService(LexboxServices.LexboxApi, hubProxy satisfies LexboxApi);
}
