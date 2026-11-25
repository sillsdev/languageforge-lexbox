<script lang="ts">
  import {onMount} from 'svelte';
  import {Button} from '$lib/components/ui/button';
  import {DotnetService} from '$lib/dotnet-types';
  import {useProjectsService} from '$lib/services/service-provider';
  import CreateProjectDialog from '../../home/CreateProjectDialog.svelte';

  const {lexboxUserName}: {lexboxUserName?: string} = $props();

  window.lexbox.ServiceProvider.setService(DotnetService.CombinedProjectsService, {
    ...useProjectsService(),
    knownLexboxUserName: () => Promise.resolve(lexboxUserName),
  });

  let dialog = $state<CreateProjectDialog>();
  onMount(() => dialog?.openDialog());
</script>

<Button variant="outline" onclick={() => dialog?.openDialog()}>Open new project dialog</Button>
<CreateProjectDialog bind:this={dialog} refreshProjects={() => Promise.resolve()} />
