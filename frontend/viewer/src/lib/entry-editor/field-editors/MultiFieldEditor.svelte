<script lang="ts">
  import FieldTitle from '../FieldTitle.svelte';
  import CrdtTextField from '../inputs/CrdtTextField.svelte';
  import type { Readable } from 'svelte/store';
  import { createEventDispatcher, getContext } from 'svelte';
  import type { MultiString, WritingSystems } from '../../mini-lcm';
  import type {WritingSystemSelection} from '../../config-types';
  import { pickWritingSystems } from '../../utils';
  import {useCurrentView} from '../../services/view-service';
  import {useWritingSystems} from '../../writing-systems';

  const dispatch = createEventDispatcher<{
    change: { value: MultiString };
  }>();

  const allWritingSystems = useWritingSystems();

  type T = $$Generic<{}>;
  export let id: string;
  export let name: string | undefined = undefined;
  export let wsType: WritingSystemSelection;
  export let value: MultiString;
  export let readonly: boolean = false;

  let unsavedChanges: Record<string, boolean> = {};
  let currentView = useCurrentView();

  $: writingSystems = pickWritingSystems(wsType, $allWritingSystems);
  $: empty = !writingSystems.some((ws) => value[ws.id] || unsavedChanges[ws.id]);
  $: collapse = empty && writingSystems.length > 1;
</script>

<div class="multi-field field" class:collapse-field={collapse} class:empty class:hidden={!$currentView.fields[id].show} style:grid-area={id}>
  <FieldTitle {id} {name} />
  <div class="fields">
    {#each writingSystems as ws (ws.id)}
      <CrdtTextField
        on:change={() => dispatch('change', { value })}
        bind:value={value[ws.id]}
        bind:unsavedChanges={unsavedChanges[ws.id]}
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
