<script lang="ts">

  import {getContext} from 'svelte';
  import type {Readable} from 'svelte/store';
  import type {ViewConfig} from '../../config-types';
  import type {IExampleSentence} from '../../mini-lcm';
  import EntityEditor from './EntityEditor.svelte';
  import MultiFieldEditor from '../field-editors/MultiFieldEditor.svelte';
  import SingleFieldEditor from '../field-editors/SingleFieldEditor.svelte';

  export let example: IExampleSentence;
  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');
</script>
<MultiFieldEditor on:change
                  bind:value={example.sentence}
                  id="sentence"
                  wsType="vernacular" />
<MultiFieldEditor on:change
                  bind:value={example.translation}
                  id="translation"
                  wsType="analysis" />
<SingleFieldEditor on:change
                   bind:value={example.reference}
                   id="reference"
                   wsType="first-analysis"/>
<EntityEditor
  entity={example}
  fieldConfigs={[]}
  customFieldConfigs={Object.values($viewConfig.activeView?.customExample ?? [])}
  on:change
/>
