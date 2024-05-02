<script lang="ts">

  import {HubConnectionBuilder, HubConnectionState} from '@microsoft/signalr';
  import {onDestroy} from 'svelte';
  import {SetupSignalR} from './lib/services/service-provider-signalr';
  import ProjectView from './ProjectView.svelte';

  export let projectName: string;
  const connection = new HubConnectionBuilder()
    .withUrl(`/api/hub/${projectName}/lexbox`)
    .withAutomaticReconnect()
    .build();
  void connection.start()
    .then(() => connected = (connection.state == HubConnectionState.Connected))
    .catch(err => console.error(err));
  onDestroy(() => connection.stop());
  SetupSignalR(connection);
  let connected = false;
</script>
<ProjectView isConnected={connected}></ProjectView>
