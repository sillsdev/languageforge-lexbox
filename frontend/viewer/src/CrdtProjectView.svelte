<script lang="ts">
  import {SetupSignalR} from './lib/services/service-provider-signalr';
  import ProjectView from './ProjectView.svelte';

  export let projectName: string;
  const {connected} = SetupSignalR(`/api/hub/${projectName}/lexbox`, {
    history: true,
    write: true,
    feedback: true,
    sync: true
  });
</script>
<ProjectView {projectName} isConnected={$connected}></ProjectView>
