<script module lang="ts">
  import {Button} from '$lib/components/ui/button';
  import type {IEntry} from '$lib/dotnet-types';
  import EntryOrSensePicker, {type EntrySenseSelection} from '$lib/entry-editor/EntryOrSensePicker.svelte';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {defineMeta} from '@storybook/addon-svelte-csf';

  // More on how to set up stories at: https://storybook.js.org/docs/writing-stories
  const { Story } = defineMeta({
    title: 'editor/misc/entry-picker',
    component: EntryOrSensePicker,
    args: {
      pick(selection) {
        selectedEntryHistory = [...selectedEntryHistory, selection];
      },
    }
  });

  let selectedEntryHistory: EntrySenseSelection[] = $state([]);
  let openPicker = $state(false);
  function disableEntry(entry: IEntry): false | { reason: string, disableSenses?: true } {
    const selected = selectedEntryHistory.some(e => e.entry.id === entry.id);
    if (!selected) return false;
    return {
      reason: 'You cannot select an entry that you have already selected',
      disableSenses: true
    };
  }
</script>

<div class="space-y-4">
  <div class="flex gap-2">
    <Button onclick={() => openPicker = true}>Open picker</Button>
    <Button onclick={() => selectedEntryHistory = []}>Clear history</Button>
  </div>

  <div>
    {#each selectedEntryHistory as selected}
      <p>
        Entry: {useWritingSystemService().headword(selected.entry)}
        {#if selected.sense}
          Sense: {useWritingSystemService().firstGloss(selected.sense)}
        {/if}
      </p>
    {/each}
  </div>
</div>

<Story name="Entry or sense">
  {#snippet template(args)}
    <EntryOrSensePicker {...args} title="Pick entry or sense" bind:open={openPicker} />
  {/snippet}
</Story>

<Story name="Entry only">
  {#snippet template(args)}
    <EntryOrSensePicker {...args} title="Pick entry" bind:open={openPicker} mode="only-entries" />
  {/snippet}
</Story>

<Story name="Disable selected entries">
  {#snippet template(args)}
    <EntryOrSensePicker {...args} title="Pick entry or sense" bind:open={openPicker} {disableEntry} />
  {/snippet}
</Story>
