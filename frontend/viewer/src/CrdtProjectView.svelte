<script lang="ts">

  import {HubConnectionBuilder, HubConnectionState} from '@microsoft/signalr';
  import {onDestroy, setContext} from 'svelte';
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
  setContext('project-name', projectName);
  SetupSignalR(connection, {
    history: true,
    write: true,
    feedback: true
  });
  let connected = false;
</script>
<ProjectView {projectName} isConnected={connected}></ProjectView>
