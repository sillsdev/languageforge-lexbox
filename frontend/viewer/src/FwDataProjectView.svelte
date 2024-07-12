<script lang="ts">

  import {HubConnectionBuilder, HubConnectionState} from '@microsoft/signalr';
  import {onDestroy, setContext} from 'svelte';
  import {SetupSignalR} from './lib/services/service-provider-signalr';
  import ProjectView from './ProjectView.svelte';
  import {navigate} from 'svelte-routing';
  import {AppNotification} from './lib/notifications/notifications';
  import {CloseReason} from './lib/generated-signalr-client/TypedSignalR.Client/Lexbox.ClientServer.Hubs';
  import {Entry} from './lib/mini-lcm';

  export let projectName: string;
  setContext('project-name', projectName);
  const connection = new HubConnectionBuilder()
    .withUrl(`/api/hub/${projectName}/fwdata`)
    .withAutomaticReconnect()
    .build();

  function connect() {
    void connection.start()
      .then(() => connected = (connection.state == HubConnectionState.Connected))
      .catch(err => {
        console.error('Failed to start the connection:', err);
      });
  }
  connect();
  onDestroy(() => connection.stop());
  connection.onclose(error => {
    connected = false;
    if (!error) return;
    console.error('Connection closed:', error);
  });
  SetupSignalR(connection, {
      history: false,
      write: true,
    },
    {
      OnEntryUpdated: async (entry: Entry) => {
        console.log('OnEntryUpdated', entry);
      },
      async OnProjectClosed(reason: CloseReason): Promise<void> {
        connected = false;
        switch (reason) {
          case CloseReason.User:
            navigate('/');
            AppNotification.display('Project closed on another tab', 'warning', 'long');
            break;
          case CloseReason.Locked:
            AppNotification.displayAction('The project is open in FieldWorks. Please close it and try again.', 'warning', {
              label: 'Retry',
              callback: () => connected = true
            });
            break;
        }
      }
    },
    (errorContext) => {
      connected = false;
      if (errorContext.error instanceof Error) {
        let message = errorContext.error.message;
        if (message.includes('The project is locked')) return; //handled via the project closed callback
        AppNotification.display('Connection error: ' + message, 'error', 'long');
      } else {
        AppNotification.display('Unknown Connection error', 'error', 'long');
      }
    }
  );
  let connected = false;
</script>
<ProjectView {projectName} isConnected={connected}></ProjectView>
