<script lang="ts">
  import AboutDialog from '$lib/about/AboutDialog.svelte';
  import ActivityView from '$lib/activity/ActivityView.svelte';
  import {useFeatures} from '$lib/services/feature-service';
  import {
    mdiDotsVertical,
    mdiEyeSettingsOutline,
    mdiFaceAgent,
    mdiHistory,
    mdiInformationVariantCircle,
    mdiNoteEdit, mdiOpenInNew
  } from '@mdi/js';
  import {createEventDispatcher} from 'svelte';
  import {Button, MenuItem, ResponsiveMenu, Toggle} from 'svelte-ux';
  import {asScottyPortal} from './Scotty.svelte';
  import {useProjectViewState} from '$lib/views/project-view-state-service';
  import WritingSystemDialog from '$lib/writing-system/WritingSystemDialog.svelte';
  import DevContent from '$lib/layout/DevContent.svelte';
  import TroubleshootDialog from '$lib/troubleshoot/TroubleshootDialog.svelte';
  import {useMultiWindowService, useTroubleshootingService} from '$lib/services/service-provider';

  const dispatch = createEventDispatcher<{
    showOptionsDialog: void;
  }>();

  export let projectName: string;
  export let about: string | undefined = undefined;

  const features = useFeatures();
  const projectViewState = useProjectViewState();
  const supportsTroubleshooting = useTroubleshootingService();
  const multiWindowService = useMultiWindowService();

  let activityViewOpen = false;
  let aboutDialogOpen = false;
  let wsEditDialogOpen = false;
  let troubleshootDialogOpen = false;
</script>

<!-- #key prevents rendering ugly delayed state updates -->
{#key $projectViewState.userPickedEntry}
<Toggle let:on={open} let:toggle let:toggleOff>
  <Button on:click={toggle} icon={mdiDotsVertical} iconOnly>
    <!-- the menu transition doesn't play well with our portal, so it's just turned off -->
    <ResponsiveMenu {open} on:close={toggleOff} class="app-bar-menu whitespace-nowrap md:-translate-x-1" transitionParams={{ duration: 0 }}>
      <button class="w-full" on:click={toggleOff}>
        <div class="contents" use:asScottyPortal={'app-bar-menu'}></div>
        <div class="contents" class:sm-view:hidden={$projectViewState.userPickedEntry}>
          {#if $features.history}
            <MenuItem icon={mdiHistory} on:click={() => activityViewOpen = true}>Activity</MenuItem>
          {/if}
        </div>
        {#if multiWindowService}
          <MenuItem icon={mdiOpenInNew}
                    on:click={() => multiWindowService.openNewWindow(location.pathname + location.search + location.hash)}>
            Open new Window
          </MenuItem>
        {/if}
        <MenuItem icon={mdiEyeSettingsOutline} on:click={() => dispatch('showOptionsDialog')}>Configure</MenuItem>
        {#if about}
          <div class="contents" class:sm-view:hidden={$projectViewState.userPickedEntry}>
            <MenuItem icon={mdiInformationVariantCircle} on:click={() => aboutDialogOpen = true}>About</MenuItem>
          </div>
        {/if}
        <DevContent>
          <MenuItem icon={mdiNoteEdit} on:click={() => wsEditDialogOpen = true}>
            Edit WS
          </MenuItem>
        </DevContent>
        {#if supportsTroubleshooting}
          <MenuItem icon={mdiFaceAgent} on:click={() => troubleshootDialogOpen = true}>Troubleshoot</MenuItem>
        {/if}
      </button>
    </ResponsiveMenu>
  </Button>
</Toggle>
{/key}

{#if $features.history}
  <ActivityView bind:open={activityViewOpen} {projectName} />
{/if}
{#if about}
  <AboutDialog bind:open={aboutDialogOpen} text={about} />
{/if}
{#if supportsTroubleshooting}
  <TroubleshootDialog bind:open={troubleshootDialogOpen}/>
{/if}
<WritingSystemDialog bind:open={wsEditDialogOpen}/>

<style lang="postcss" global>
  .app-bar-menu .MenuItem {
    @apply justify-start;
  }
</style>
