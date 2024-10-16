<script lang="ts">
  import EntityEditor from './EntityEditor.svelte';
  import type {ISense, SemanticDomain} from '../../mini-lcm';
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
  const partsOfSpeech = usePartsOfSpeech();
  const semanticDomains = useSemanticDomains();
  const writingSystems = useWritingSystems();
  function setSemanticDomains(ids: string[]): void {
    sense.semanticDomains = ids.map(id => $semanticDomains.find(sd => sd.id === id)).filter((sd): sd is SemanticDomain => !!sd);
  }
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
                      options={$partsOfSpeech.map(pos => ({value: pos.id, label: pickBestAlternative(pos.name, 'analysis', $writingSystems)}))}
                      {readonly}
                      id="partOfSpeechId"
                      wsType="first-analysis" />
  <MultiOptionEditor
                    on:change={(e) => setSemanticDomains(e.detail.value)}
                    on:change
                    value={sense.semanticDomains.map(sd => sd.id)}
                    options={$semanticDomains.map(sd => ({value: sd.id, label: `${sd.code} ${pickBestAlternative(sd.name, 'analysis', $writingSystems)}`}))}
                    {readonly}
                    id="semanticDomains"
                    wsType="first-analysis" />
  <EntityEditor
    entity={sense}
    {readonly}
    customFieldConfigs={[]}
    on:change
  />
</div>
