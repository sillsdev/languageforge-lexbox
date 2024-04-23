<script lang="ts">
  import CrdtOptionField from './CrdtOptionField.svelte';

  import FieldTitle from './FieldTitle.svelte';
  import type { WritingSystems } from '../mini-lcm';
  import type { Readable } from 'svelte/store';
  import { getContext } from 'svelte';
  import { pickWritingSystems } from '../utils';
  import type { FieldConfig } from '../types';

  type T = $$Generic<{}>;
  export let field: FieldConfig;
  export let value: string;

  const allWritingSystems =
    getContext<Readable<WritingSystems>>('writingSystems');

  $: [ws] = pickWritingSystems(field.ws, $allWritingSystems);
  $: empty = !value;
</script>

<div class="single-field field" class:empty class:extra={'extra' in field && field.extra}>
  <FieldTitle {field} />
  <div class="fields">
    <CrdtOptionField on:change bind:value placeholder={ws.abbreviation} />
  </div>
</div>
