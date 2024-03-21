import './app.postcss';

import App from './App.svelte'
import { HubConnectionBuilder } from '@microsoft/signalr';
import { SetupSignalR } from './lib/services/service-provider-signalr';

// const connection = new HubConnectionBuilder()
//     .withUrl("/api/hub/project")
//     .withAutomaticReconnect()
//     .build();
// await connection.start();
// SetupSignalR(connection);

export {
  App
};
