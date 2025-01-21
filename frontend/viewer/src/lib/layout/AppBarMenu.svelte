<script lang="ts">
  import AboutDialog from '$lib/about/AboutDialog.svelte';
  import ActivityView from '$lib/activity/ActivityView.svelte';
  import {useFeatures} from '$lib/services/feature-service';
  import {mdiDotsVertical, mdiEyeSettingsOutline, mdiHistory, mdiInformationVariantCircle} from '@mdi/js';
  import {createEventDispatcher} from 'svelte';
  import { Button, Menu, MenuItem, Toggle } from 'svelte-ux';

  const dispatch = createEventDispatcher<{
    showOptionsDialog: void;
  }>();

  export let projectName: string;
  export let about: string | undefined = undefined;

  const features = useFeatures();

  $: console.log(1, $features);

  let activityViewOpen = false;
  let aboutDialogOpen = false;
</script>

<Toggle let:on={open} let:toggle let:toggleOff>
  <Button on:click={toggle} icon={mdiDotsVertical} iconOnly>
    <Menu {open} on:close={toggleOff}>
      {#if $features.history}
        <MenuItem icon={mdiHistory} on:click={() => activityViewOpen = true} class="justify-start">Activity</MenuItem>
      {/if}
      <MenuItem icon={mdiEyeSettingsOutline} on:click={() => dispatch('showOptionsDialog')} class="justify-start">Configure</MenuItem>
      {#if about}
        <MenuItem icon={mdiInformationVariantCircle} on:click={() => aboutDialogOpen = true} class="justify-start">About</MenuItem>
      {/if}
    </Menu>
  </Button>
</Toggle>

{#if $features.history}
  <ActivityView bind:open={activityViewOpen} {projectName} />
{/if}

{#if about}
  <AboutDialog bind:open={aboutDialogOpen} text={about} />
{/if}
