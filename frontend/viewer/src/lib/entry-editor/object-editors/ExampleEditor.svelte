<script lang="ts">

  import {getContext} from 'svelte';
  import type {Readable} from 'svelte/store';
  import type {ViewConfig} from '../../config-types';
  import type {IExampleSentence} from '../../mini-lcm';
  import EntityEditor from './EntityEditor.svelte';
  import MultiFieldEditor from '../field-editors/MultiFieldEditor.svelte';
  import SingleFieldEditor from '../field-editors/SingleFieldEditor.svelte';

  export let example: IExampleSentence;
  export let readonly: boolean;
  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');
</script>
<MultiFieldEditor on:change
                  bind:value={example.sentence}
                  {readonly}
                  id="sentence"
                  wsType="vernacular" />
<MultiFieldEditor on:change
                  bind:value={example.translation}
                  {readonly}
                  id="translation"
                  wsType="analysis" />
<SingleFieldEditor on:change
                   bind:value={example.reference}
                   {readonly}
                   id="reference"
                   wsType="first-analysis"/>
<EntityEditor
  entity={example}
  {readonly}
  customFieldConfigs={Object.values($viewConfig.activeView?.customExample ?? [])}
  on:change
/>
