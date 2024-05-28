<script lang="ts">

  import {getContext} from 'svelte';
  import type {Readable} from 'svelte/store';
  import type {ViewConfig} from '../config-types';
  import type {IExampleSentence} from '../mini-lcm';
  import EntityEditor from './EntityEditor.svelte';

  export let example: IExampleSentence;
  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');
</script>
<EntityEditor
  entity={example}
  fieldConfigs={Object.values($viewConfig.activeView?.example ?? [])}
  customFieldConfigs={Object.values($viewConfig.activeView?.customExample ?? [])}
  on:change
/>
