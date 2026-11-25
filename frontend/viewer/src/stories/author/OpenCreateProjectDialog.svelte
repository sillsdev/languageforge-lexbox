<script lang="ts">
  import {onMount} from 'svelte';
  import {Button} from '$lib/components/ui/button';
  import {DotnetService, LoginResult, type IAuthService} from '$lib/dotnet-types';
  import CreateProjectDialog from '../../home/CreateProjectDialog.svelte';

  const {loggedInAs}: {loggedInAs?: string} = $props();

  const authService: IAuthService = {
    servers: () => Promise.resolve(loggedInAs ? [{
      displayName: 'Lexbox', loggedIn: true, loggedInAs,
      server: {id: 'lexbox', displayName: 'Lexbox', authority: 'https://lexbox.example'},
    }] : []),
    signInWebView: () => Promise.resolve(LoginResult.Success),
    useSystemWebView: () => Promise.resolve(true),
    logout: () => Promise.resolve(),
  };
  window.lexbox.ServiceProvider.setService(DotnetService.AuthService, authService);

  let dialog = $state<CreateProjectDialog>();
  onMount(() => dialog?.openDialog());
</script>

<Button variant="outline" onclick={() => dialog?.openDialog()}>Open new project dialog</Button>
<CreateProjectDialog bind:this={dialog} refreshProjects={() => Promise.resolve()} />
