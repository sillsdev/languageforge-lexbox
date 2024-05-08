<script lang="ts">
  import EntityEditor from './EntityEditor.svelte';
  import type {ISense} from '../mini-lcm';
  import {getContext} from 'svelte';
  import type {Readable} from 'svelte/store';
  import type {ViewConfig} from '../config-types';

  export let sense: ISense;
  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');
</script>

<EntityEditor
  entity={sense}
  fieldConfigs={Object.values($viewConfig.activeView?.sense ?? [])}
  customFieldConfigs={Object.values($viewConfig.activeView?.customSense ?? [])}
  on:change
/>
