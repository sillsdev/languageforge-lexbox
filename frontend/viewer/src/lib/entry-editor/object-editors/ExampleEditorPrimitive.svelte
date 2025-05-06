<script lang="ts">
  import type {IExampleSentence} from '$lib/dotnet-types';
  import MultiFieldEditor from '../field-editors/MultiFieldEditor.svelte';
  import SingleFieldEditor from '../field-editors/SingleFieldEditor.svelte';
  import {objectTemplateAreas, useCurrentView} from '$lib/views/view-service';
  import {EditorSubGrid} from '$lib/components/editor';

  export let example: IExampleSentence;
  export let readonly: boolean;
  const currentView = useCurrentView();
</script>

<EditorSubGrid style="grid-template-areas: {objectTemplateAreas($currentView, example)}">
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
</EditorSubGrid>
