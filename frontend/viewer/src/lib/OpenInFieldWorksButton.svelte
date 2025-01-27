<script lang="ts">
  import flexLogo from '$lib/assets/flex-logo.png';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useAppLauncher} from '$lib/services/service-provider';
  import {Button} from 'svelte-ux';
  import {useProjectViewState} from './services/project-view-state-service';

  const projectViewState = useProjectViewState();
  const appLauncher = useAppLauncher();

  export let show = false;
  export let entryId: string;
  export let projectName: string;

  async function openInFlex(e: Event) {
    if (appLauncher) {
      e.preventDefault();//don't follow the link
      await appLauncher.openInFieldWorks(entryId, projectName);
    }
    AppNotification.displayAction('The project is open in FieldWorks. Please close it to reopen.', 'warning', {
      label: 'Open',
      callback: () => window.location.reload()
    });
  }
</script>
{#if show}
<!--button must be a link otherwise it won't follow the redirect to a protocol handler-->
  <Button
    href={`/api/fw/${projectName}/open/entry/${entryId}`}
    on:click={openInFlex}
    variant="fill-light"
    color="info"
    size="sm">
    <img src={flexLogo} alt="FieldWorks logo" class="h-6 max-w-fit"/>
    <div class="sm-form:hidden" class:hidden={$projectViewState.rightToolbarCollapsed}>
      Open in FieldWorks
    </div>
  </Button>
{/if}
