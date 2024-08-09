<script lang="ts">
  import EntityEditor from './EntityEditor.svelte';
  import type {ISense} from '../../mini-lcm';
  import MultiFieldEditor from '../field-editors/MultiFieldEditor.svelte';
  import SingleOptionEditor from '../field-editors/SingleOptionEditor.svelte';
  import MultiOptionEditor from '../field-editors/MultiOptionEditor.svelte';
  import {useSemanticDomains} from '../../semantic-domains';
  import {useWritingSystems} from '../../writing-systems';
  import {pickBestAlternative} from '../../utils';
  import {usePartsOfSpeech} from '../../parts-of-speech';

  export let sense: ISense;
  export let readonly: boolean = false;
  const partsOfSpeech = usePartsOfSpeech();
  const semanticDomains = useSemanticDomains();
  const writingSystems = useWritingSystems();
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
                    options={$partsOfSpeech.map(pos => ({label: pickBestAlternative(pos.name, 'analysis', $writingSystems), value: pos.id}))}/>
<MultiOptionEditor on:change
                   bind:value={sense.semanticDomains}
                   {readonly}
                   id="semanticDomains"
                   wsType="first-analysis"
                   options={$semanticDomains.map(sd => ({label: pickBestAlternative(sd.name, 'analysis', $writingSystems), value: sd.id}))}/>
<EntityEditor
  entity={sense}
  {readonly}
  customFieldConfigs={[]}
  on:change
/>
