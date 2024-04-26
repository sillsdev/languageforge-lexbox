<script lang="ts">
  import CrdtMultiOptionField from './CrdtMultiOptionField.svelte';

  import FieldTitle from './FieldTitle.svelte';
  import type { WritingSystems } from '../mini-lcm';
  import type { Readable } from 'svelte/store';
  import { getContext } from 'svelte';
  import { pickWritingSystems } from '../utils';
  import type { FieldConfig, ViewConfig } from '../config-types';

  type T = $$Generic<{}>;
  export let field: FieldConfig;
  export let value: string[];

  const allWritingSystems = getContext<Readable<WritingSystems>>('writingSystems');
  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');

  $: [ws] = pickWritingSystems(field.ws, $allWritingSystems);
  $: empty = !value?.length;
</script>

<div class="single-field field" class:empty class:extra={'extra' in field && field.extra}>
  <FieldTitle {field} />
  <div class="fields">
    <CrdtMultiOptionField on:change bind:value placeholder={ws.abbreviation} readonly={field.readonly || $viewConfig.readonly} />
  </div>
</div>
