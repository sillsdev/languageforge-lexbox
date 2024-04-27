//*// v1 Run with normal Svelte App (advantages: tailwind classes stay up to date)

import './app.postcss';
import {HubConnectionBuilder} from '@microsoft/signalr';
import {SetupSignalR} from './lib/services/service-provider-signalr';
import App from './App.svelte'

async function run() {

  const connection = new HubConnectionBuilder()
    .withUrl("/api/hub/project")
    .withAutomaticReconnect()
    .build();
  await connection.start();
  SetupSignalR(connection);

  new App({
    target: document.getElementById('app')!,
  });
}

run();
/*/// v2 Run with web-component in shadow dom

import './web-component';

//*/


