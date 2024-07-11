<script lang="ts">

  import {HubConnectionBuilder, HubConnectionState} from '@microsoft/signalr';
  import {onDestroy, setContext} from 'svelte';
  import {SetupSignalR} from './lib/services/service-provider-signalr';
  import ProjectView from './ProjectView.svelte';
  import {navigate} from 'svelte-routing';
  import {AppNotification} from './lib/notifications/notifications';

  export let projectName: string;
  const connection = new HubConnectionBuilder()
    .withUrl(`/api/hub/${projectName}/fwdata`)
    .withAutomaticReconnect()
    .build();
  void connection.start()
    .then(() => connected = (connection.state == HubConnectionState.Connected))
    .catch(err => console.error(err));
  onDestroy(() => connection.stop());
  setContext('project-name', projectName);
  SetupSignalR(connection, {
    history: false,
    write: true,
  },
  async () => {
    navigate('/');
    AppNotification.display('Project closed on another tab', 'warning', 'long');
  });
  let connected = false;
</script>
<ProjectView {projectName} isConnected={connected}></ProjectView>
