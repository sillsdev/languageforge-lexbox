<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import {MultiWsInput, RichMultiWsInput} from '$lib/components/field-editors';
  import {getEntityConfig} from '$lib/views/entity-config';
  import {tvt, type ViewText} from '$lib/views/view-text';
  import type {IMultiString, IRichMultiString} from '$lib/dotnet-types';
  import type {ContextField} from './tasks-service';

  const {fields}: {fields: ContextField[]} = $props();

  function label(field: ContextField) {
    return (getEntityConfig(field.entity) as Record<string, {label: ViewText}>)[field.fieldId].label;
  }
</script>

{#each fields as field (field.fieldId)}
  <!-- no fieldId: it sets a grid-area this grid doesn't define -->
  <Editor.Field.Root>
    <Editor.Field.Title name={$tvt(label(field))} />
    <Editor.Field.Body subGrid>
      {#if field.rich}
        <RichMultiWsInput bind:value={field.owner[field.fieldId] as IRichMultiString} writingSystems={field.writingSystems} readonly />
      {:else}
        <MultiWsInput bind:value={field.owner[field.fieldId] as IMultiString} writingSystems={field.writingSystems} readonly />
      {/if}
    </Editor.Field.Body>
  </Editor.Field.Root>
{/each}
