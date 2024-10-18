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
  import {useCurrentView, objectTemplateAreas} from '../../services/view-service';

  export let sense: ISense;
  export let readonly: boolean = false;
  const writingSystems = useWritingSystems();
  const partsOfSpeech = usePartsOfSpeech(writingSystems);
  const semanticDomains = useSemanticDomains();
  const currentView = useCurrentView();
</script>

<div class="grid-layer" style:grid-template-areas={objectTemplateAreas($currentView, sense)}>
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
                      idValue
                      options={$partsOfSpeech}
                      getOptionLabel={(pos) => pos.label}
                      {readonly}
                      id="partOfSpeechId"
                      wsType="first-analysis" />
  <MultiOptionEditor
                    on:change
                    bind:value={sense.semanticDomains}
                    options={$semanticDomains}
                    getOptionLabel={(sd) => `${sd.code} ${pickBestAlternative(sd.name, 'analysis', $writingSystems)}`}
                    {readonly}
                    id="semanticDomains"
                    wsType="first-analysis" />
  <EntityEditor
    {readonly}
    customFieldConfigs={[]}
    on:change
  />
</div>
