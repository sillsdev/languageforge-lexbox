<script lang="ts">
  import {EditorSubGrid} from '$lib/components/editor';
  import type {IEntry} from '$lib/dotnet-types';
  import {objectTemplateAreas, useCurrentView} from '$lib/views/view-service';
  import ComplexFormComponents from '../field-editors/ComplexFormComponents.svelte';
  import ComplexForms from '../field-editors/ComplexForms.svelte';
  import ComplexFormTypes from '../field-editors/ComplexFormTypes.svelte';
  import MultiFieldEditor from '../field-editors/MultiFieldEditor.svelte';

  type Props = {
    entry: IEntry;
    readonly?: boolean;
    modalMode?: boolean;
    onchange?: (entry: IEntry) => void; // Added onchange prop
  }

  const {
    entry,
    onchange,
    readonly = false,
    modalMode = false,
  }: Props = $props();

  const currentView = useCurrentView();

  function onFieldChanged(): void {
    onchange?.(entry);
  }
</script>

<EditorSubGrid style="grid-template-areas: {objectTemplateAreas($currentView, entry)}">
  <MultiFieldEditor on:change={onFieldChanged}
                    bind:value={entry.lexemeForm}
                    {readonly}
                    autofocus={modalMode}
                    id="lexemeForm"
                    wsType="vernacular"/>

  <MultiFieldEditor on:change={onFieldChanged}
                    bind:value={entry.citationForm}
                    {readonly}
                    id="citationForm"
                    wsType="vernacular"/>

  {#if !modalMode}
    <ComplexForms on:change={onFieldChanged}
                  bind:value={entry.complexForms}
                  {readonly}
                  {entry}
                  id="complexForms" />

    <ComplexFormTypes on:change={onFieldChanged}
                  bind:value={entry.complexFormTypes}
                  {readonly}
                  id="complexFormTypes" />

    <ComplexFormComponents  on:change={onFieldChanged}
                            bind:value={entry.components}
                            {readonly}
                            {entry}
                            id="components" />
  {/if}

  <MultiFieldEditor on:change={onFieldChanged}
                    bind:value={entry.literalMeaning}
                    {readonly}
                    id="literalMeaning"
                    wsType="vernacular"/>
  <MultiFieldEditor on:change={onFieldChanged}
                    bind:value={entry.note}
                    {readonly}
                    id="note"
                    wsType="analysis"/>
</EditorSubGrid>
