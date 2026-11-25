<script lang="ts">
  import {onMount} from 'svelte';
  import {Button} from '$lib/components/ui/button';
  import {DotnetService, LoginResult} from '$lib/dotnet-types';
  import {useProjectsService} from '$lib/services/service-provider';
  import CreateProjectDialog from '../../home/CreateProjectDialog.svelte';

  const {loggedInAs = [], lexboxUserName}: {loggedInAs?: string[], lexboxUserName?: string} = $props();

  window.lexbox.ServiceProvider.setService(DotnetService.AuthService, {
    servers: () => Promise.resolve(loggedInAs.map((userName, i) => {
      const displayName = i ? `Lexbox ${i + 1}` : 'Lexbox';
      return {displayName, loggedIn: true, loggedInAs: userName, server: {id: `lexbox-${i}`, displayName, authority: `https://lexbox-${i}.example`}};
    })),
    signInWebView: () => Promise.resolve(LoginResult.Success),
    useSystemWebView: () => Promise.resolve(true),
    logout: () => Promise.resolve(),
  });
  window.lexbox.ServiceProvider.setService(DotnetService.CombinedProjectsService, {
    ...useProjectsService(),
    knownLexboxUserName: () => Promise.resolve(lexboxUserName),
  });

  let dialog = $state<CreateProjectDialog>();
  onMount(() => dialog?.openDialog());
</script>

<Button variant="outline" onclick={() => dialog?.openDialog()}>Open new project dialog</Button>
<CreateProjectDialog bind:this={dialog} refreshProjects={() => Promise.resolve()} />
