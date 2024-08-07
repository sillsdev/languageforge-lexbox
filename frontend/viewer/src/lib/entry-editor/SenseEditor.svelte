<script lang="ts">
  import EntityEditor from './EntityEditor.svelte';
  import type {ISense} from '../mini-lcm';
  import {getContext} from 'svelte';
  import type {Readable} from 'svelte/store';
  import type {ViewConfig} from '../config-types';
  import MultiFieldEditor from './MultiFieldEditor.svelte';
  import SingleOptionEditor from './SingleOptionEditor.svelte';
  import MultiOptionEditor from './MultiOptionEditor.svelte';

  export let sense: ISense;
  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');
</script>

<MultiFieldEditor on:change bind:value={sense.gloss} field={{ id: 'gloss', type: 'multi', ws: 'analysis', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Gloss_field_Sense.htm' }} />
<MultiFieldEditor on:change bind:value={sense.definition} field={{ id: 'definition', type: 'multi', ws: 'analysis', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/definition_field.htm' }} />
<SingleOptionEditor on:change bind:value={sense.partOfSpeechId} field={{ id: 'partOfSpeechId', type: 'option', optionType: 'part-of-speech', ws: 'first-analysis', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Grammatical_Info_field.htm' }} />
<MultiOptionEditor on:change bind:value={sense.semanticDomains} field={{ id: 'semanticDomains', type: 'multi-option', optionType: 'semantic-domain', ws: 'first-analysis', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/semantic_domains_field.htm' }} />
<EntityEditor
  entity={sense}
  fieldConfigs={[]}
  customFieldConfigs={Object.values($viewConfig.activeView?.customSense ?? [])}
  on:change
/>
