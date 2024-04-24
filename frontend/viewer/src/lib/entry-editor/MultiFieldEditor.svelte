<script lang="ts">
  import FieldTitle from './FieldTitle.svelte';
  import CrdtTextField from './CrdtTextField.svelte';
  import type { Readable } from 'svelte/store';
  import { createEventDispatcher, getContext } from 'svelte';
  import type { MultiString, WritingSystems } from '../mini-lcm';
  import type { FieldConfig } from '../config-types';
  import { pickWritingSystems } from '../utils';

  const dispatch = createEventDispatcher<{
    change: { value: MultiString };
  }>();

  const allWritingSystems = getContext<Readable<WritingSystems>>('writingSystems');

  type T = $$Generic<{}>;
  export let field: FieldConfig;
  export let value: MultiString;

  let unsavedChanges: Record<string, boolean> = {};

  $: writingSystems = pickWritingSystems(field.ws, $allWritingSystems);
  $: empty = !writingSystems.some((ws) => value[ws.id] || unsavedChanges[ws.id]);
  $: collapse = empty && writingSystems.length > 1;
</script>

<div class="multi-field field" class:collapse-field={collapse} class:empty>
  <FieldTitle {field} />
  <div class="fields">
    {#each writingSystems as ws (ws.id)}
      <CrdtTextField
        on:change={() => dispatch('change', { value })}
        bind:value={value[ws.id]}
        bind:unsavedChanges={unsavedChanges[ws.id]}
        label={collapse ? undefined : ws.abbreviation}
        labelPlacement={collapse ? undefined : 'left'}
        placeholder={collapse ? ws.abbreviation : undefined}
      />
    {/each}
  </div>
</div>

<style>
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
