<script lang="ts">
  import FieldTitle from '../FieldTitle.svelte';
  import CrdtTextField from '../inputs/CrdtTextField.svelte';
  import { createEventDispatcher } from 'svelte';
  import type { IMultiString } from '$lib/dotnet-types';
  import type {WritingSystemSelection} from '../../config-types';
  import {useCurrentView} from '$lib/views/view-service';
  import {useWritingSystemService} from '../../writing-system-service.svelte';
  import {makeHasHadValueTracker} from '$lib/utils';

  const dispatch = createEventDispatcher<{
    change: { value: IMultiString };
  }>();

  const writingSystemService = useWritingSystemService();

  export let id: string;
  export let name: string | undefined = undefined;
  export let wsType: WritingSystemSelection;
  export let value: IMultiString;
  export let readonly: boolean = false;
  export let autofocus: boolean = false;

  let unsavedChanges: Record<string, boolean> = {};
  let currentView = useCurrentView();

  $: writingSystems = writingSystemService.pickWritingSystems(wsType);

  let hasHadValueTracker = makeHasHadValueTracker();
  let hasHadValue = hasHadValueTracker.store;
  $: hasHadValueTracker.pushAndGet(writingSystems.some((ws) => value[ws.wsId] || unsavedChanges[ws.wsId]));
  $: collapse = !$hasHadValue && writingSystems.length > 1;
  $: hide = !$currentView.fields[id].show;
</script>

<div class="multi-field field" class:collapse-field={collapse} class:unused={!$hasHadValue} class:hidden={hide} style:grid-area={id}>
  <FieldTitle {id} {name} />
  <div class="fields">
    {#each writingSystems as ws, idx (ws.wsId)}
      <CrdtTextField
        on:change={() => dispatch('change', { value })}
        bind:value={value[ws.wsId]}
        bind:unsavedChanges={unsavedChanges[ws.wsId]}
        autofocus={autofocus && (idx == 0)}
        label={collapse ? undefined : ws.abbreviation}
        labelPlacement={collapse ? undefined : 'left'}
        placeholder={collapse ? ws.abbreviation : undefined}
        {readonly}
      />
    {/each}
  </div>
</div>

<style lang="postcss">
  .collapse-field .fields {
    grid-column: 3;
    display: flex;

    :global(.ws-field-wrapper) {
      display: flex;
      flex-grow: 1;
    }

    :global(.ws-field) {
      flex-grow: 1;
    }

    :global(.input) {
      @apply my-2 py-0;
    }
  }
</style>
