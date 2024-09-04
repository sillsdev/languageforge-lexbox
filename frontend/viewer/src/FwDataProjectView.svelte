<script lang="ts">
  import {HubConnectionBuilder, HubConnectionState, type IHttpConnectionOptions} from '@microsoft/signalr';
  import {onDestroy, setContext} from 'svelte';
  import {SetupSignalR} from './lib/services/service-provider-signalr';
  import ProjectView from './ProjectView.svelte';
  import {onDestroy} from 'svelte';
  import {navigate} from 'svelte-routing';
  import {AppNotification} from './lib/notifications/notifications';
  import {CloseReason} from './lib/generated-signalr-client/TypedSignalR.Client/Lexbox.ClientServer.Hubs';
  import {useEventBus} from './lib/services/event-bus';

  export let projectName: string;
  export let baseUrl: string = '';
  export let signalrConnectionOptions: IHttpConnectionOptions = {};;
  const {connected, lexboxApi} = SetupSignalR(`/api/hub/${projectName}/fwdata`, {
      history: false,
      write: true,
      openWithFlex: true,
      feedback: true
    },
    (errorContext) => {
      if (errorContext.error instanceof Error) {
        let message = errorContext.error.message;
        if (message.includes('The project is locked')) return {handled: true}; //handled via the project closed callback
      }
      return {handled: false};
    }
  );
  onDestroy(useEventBus().onProjectClosed(reason => {
    switch (reason) {
      case CloseReason.User:
        navigate('/');
        AppNotification.display('Project closed on another tab', 'warning', 'long');
        break;
      case CloseReason.Locked:
        AppNotification.displayAction('The project is open in FieldWorks. Please close it and try again.', 'warning', {
          label: 'Retry',
          callback: () => $connected = true
        });
        break;
    }
  }));
</script>
<ProjectView {projectName} isConnected={$connected}></ProjectView>
