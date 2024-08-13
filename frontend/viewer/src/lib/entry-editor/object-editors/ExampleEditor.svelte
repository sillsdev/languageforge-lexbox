<script lang="ts">
  import type {IExampleSentence} from '../../mini-lcm';
  import EntityEditor from './EntityEditor.svelte';
  import MultiFieldEditor from '../field-editors/MultiFieldEditor.svelte';
  import SingleFieldEditor from '../field-editors/SingleFieldEditor.svelte';

  export let example: IExampleSentence;
  export let readonly: boolean;
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
  customFieldConfigs={[]}
  on:change
/>
