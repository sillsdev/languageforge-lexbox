import type { HubConnection } from "@microsoft/signalr";
import {getHubProxyFactory, getReceiverRegister} from '../generated-signalr-client/TypedSignalR.Client';
import type { LexboxApi } from "./lexbox-api";
import {LexboxServiceProvider, LexboxServices} from "./service-provider";
import type { Entry } from "../generated-signalr-client/lexboxClientContracts";

export function SetupSignalR(connection: HubConnection) {
    const hubFactory = getHubProxyFactory("ILexboxApiHub");
    const hubProxy = hubFactory.createHubProxy(connection);
    getReceiverRegister("ILexboxClient").register(connection, {
        OnEntryUpdated: async (entry: Entry) => {
            console.log('OnEntryUpdated', entry);
        }
    });
    LexboxServiceProvider.setService(LexboxServices.LexboxApi, hubProxy satisfies LexboxApi);
}