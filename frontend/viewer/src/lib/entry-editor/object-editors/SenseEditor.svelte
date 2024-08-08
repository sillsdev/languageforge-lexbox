<script lang="ts">
  import EntityEditor from './EntityEditor.svelte';
  import type {ISense} from '../../mini-lcm';
  import {getContext} from 'svelte';
  import type {Readable} from 'svelte/store';
  import type {ViewConfig} from '../../config-types';
  import MultiFieldEditor from '../field-editors/MultiFieldEditor.svelte';
  import SingleOptionEditor from '../field-editors/SingleOptionEditor.svelte';
  import MultiOptionEditor from '../field-editors/MultiOptionEditor.svelte';
  import type {OptionProvider} from '../../services/option-provider';

  export let sense: ISense;
  export let readonly: boolean = false;
  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');
  const optionProvider = getContext<OptionProvider>('optionProvider');
  const partsOfSpeech = optionProvider.partsOfSpeech;
  const semanticDomains = optionProvider.semanticDomains;
</script>

<MultiFieldEditor on:change
                  bind:value={sense.gloss}
                  {readonly}
                  id="gloss"
                  wsType="analysis" />
<MultiFieldEditor on:change
                  bind:value={sense.definition}
                  {readonly}
                  id="definition"
                  wsType="analysis" />
<SingleOptionEditor on:change
                    bind:value={sense.partOfSpeechId}
                    {readonly}
                    id="partOfSpeechId"
                    wsType="first-analysis"
                    options={$partsOfSpeech}/>
<MultiOptionEditor on:change
                   bind:value={sense.semanticDomains}
                   {readonly}
                   id="semanticDomains"
                   wsType="first-analysis"
                   options={$semanticDomains}/>
<EntityEditor
  entity={sense}
  {readonly}
  customFieldConfigs={Object.values($viewConfig.activeView?.customSense ?? [])}
  on:change
/>
