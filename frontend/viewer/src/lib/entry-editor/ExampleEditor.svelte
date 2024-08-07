<script lang="ts">

  import {getContext} from 'svelte';
  import type {Readable} from 'svelte/store';
  import type {ViewConfig} from '../config-types';
  import type {IExampleSentence} from '../mini-lcm';
  import EntityEditor from './EntityEditor.svelte';
  import MultiFieldEditor from './MultiFieldEditor.svelte';
  import SingleFieldEditor from './SingleFieldEditor.svelte';

  export let example: IExampleSentence;
  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');
</script>
<MultiFieldEditor on:change bind:value={example.sentence} field={{ id: 'sentence', type: 'multi', ws: 'vernacular', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/example_field.htm' }} />
<MultiFieldEditor on:change bind:value={example.translation} field={{ id: 'translation', type: 'multi', ws: 'analysis', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Translation_field.htm' }} />
<SingleFieldEditor on:change bind:value={example.reference} field={{ id: 'reference', type: 'single', ws: 'first-analysis', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Reference_field.htm' }} />
<EntityEditor
  entity={example}
  fieldConfigs={[]}
  customFieldConfigs={Object.values($viewConfig.activeView?.customExample ?? [])}
  on:change
/>
