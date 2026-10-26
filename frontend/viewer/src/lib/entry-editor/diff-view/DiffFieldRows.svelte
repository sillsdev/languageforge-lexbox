<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import {WritingSystemType, type IMultiString, type IRichMultiString, type IViewField, type IViewWritingSystem} from '$lib/dotnet-types';
  import {useWritingSystemService} from '$project/data';
  import {useMorphTypesService} from '$project/data/morph-types.svelte';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {FW_CLASSIC_VIEW, FW_LITE_VIEW} from '$lib/views/view-data';
  import {getEntityConfig, type EntityType} from '$lib/views/entity-config';
  import {pt, tvt, type ViewText} from '$lib/views/view-text';
  import {t} from 'svelte-i18n-lingui';
  import DiffMultiString from './DiffMultiString.svelte';
  import DiffRichText from './DiffRichText.svelte';
  import DiffText from './DiffText.svelte';
  import DiffShell from './DiffShell.svelte';
  import DiffSelect from './DiffSelect.svelte';
  import DiffMultiSelect from './DiffMultiSelect.svelte';
  import type {DiffRow} from './diff-fields';

  let {rows, before, after, changedOnly = false}: {
    rows: Readonly<Record<string, DiffRow | null>>;
    before?: object;
    after?: object;
    /** For fields the editor doesn't show, which would only add noise when unchanged. */
    changedOnly?: boolean;
  } = $props();

  const writingSystemService = useWritingSystemService();
  const morphTypes = useMorphTypesService();
  const viewService = useViewService();
  const analysisWss = $derived(writingSystemService.viewAnalysis(viewService.currentView));

  const b = $derived(before as Record<string, unknown> | undefined);
  const a = $derived(after as Record<string, unknown> | undefined);
  const shown = $derived(Object.entries(rows).filter((entry): entry is [string, DiffRow] => {
    const [field, row] = entry;
    if (!row) return false;
    if (changedOnly) return JSON.stringify(b?.[field]) !== JSON.stringify(a?.[field]);
    return isSet(b?.[field]) || isSet(a?.[field]);
  }));

  function isSet(value: unknown): boolean {
    return value !== undefined && value !== null;
  }

  function text(row: DiffRow, value: unknown): string | undefined {
    if (!isSet(value)) return undefined;
    switch (row.kind) {
      case 'boolean':
        return value ? $t`Yes` : $t`No`;
      case 'wsType':
        return value === WritingSystemType.Analysis || value === 'Analysis' ? $t`Analysis` : $t`Vernacular`;
      case 'viewBase':
        return value === FW_CLASSIC_VIEW.base ? FW_CLASSIC_VIEW.name : value === FW_LITE_VIEW.base ? FW_LITE_VIEW.name : String(value);
      case 'morphType': {
        const morphType = morphTypes.current.find((m) => m.kind === value);
        return (morphType && writingSystemService.pickBestAlternative(morphType.name, 'analysis')) || String(value);
      }
      default:
        return String(value);
    }
  }

  function wsList(value: unknown): IViewWritingSystem[] {
    return (value as IViewWritingSystem[] | undefined) ?? [{wsId: ''}];
  }

  // The label the entry editor shows for a custom view's field, or the raw id for a field entityConfig doesn't know.
  function viewFieldLabel(entity: EntityType, fieldId: string): string {
    const field = fieldId === '$label' ? undefined : (getEntityConfig(entity)[fieldId] as {label: ViewText} | undefined);
    return field ? pt($tvt(field.label), viewService.currentView) : fieldId;
  }
</script>

{#if shown.length}
  <Editor.SubGrid class="gap-2">
    {#each shown as [field, row] (field)}
      {@const old = b?.[field]}
      {@const now = a?.[field]}
      <Editor.Field.Root>
        <Editor.Field.Title name={$t(row.label)} />
        <Editor.Field.Body subGrid={row.kind === 'multiString' || row.kind === 'richMultiString'}>
          {#if row.kind === 'richMultiString'}
            <DiffRichText before={old as IRichMultiString} after={now as IRichMultiString} writingSystems={analysisWss} />
          {:else if row.kind === 'multiString'}
            <DiffMultiString before={old as IMultiString} after={now as IMultiString} writingSystems={analysisWss} />
          {:else if row.kind === 'viewFields'}
            <DiffMultiSelect before={old as IViewField[]} after={now as IViewField[]} sortValuesBy="selectionOrder"
                             idSelector={(f) => f.fieldId} labelSelector={(f) => viewFieldLabel(row.entity, f.fieldId)} />
          {:else if row.kind === 'wsList'}
            <!-- No list means every writing system, which an empty list would otherwise look like -->
            <DiffMultiSelect before={wsList(old)} after={wsList(now)} sortValuesBy="selectionOrder"
                             idSelector={(ws) => ws.wsId} labelSelector={(ws) => ws.wsId || $t`All`} />
          {:else if row.kind === 'stringList'}
            <DiffMultiSelect before={old as string[]} after={now as string[]} sortValuesBy="selectionOrder"
                             idSelector={(s) => s} labelSelector={(s) => s} />
          {:else if row.kind === 'text'}
            <DiffShell>
              <DiffText before={text(row, old)} after={text(row, now)} />
            </DiffShell>
          {:else}
            <!-- A character diff of "0" to "1" would read as "01" -->
            <DiffSelect before={text(row, old)} after={text(row, now)} idSelector={(s) => s} labelSelector={(s) => s} />
          {/if}
        </Editor.Field.Body>
      </Editor.Field.Root>
    {/each}
  </Editor.SubGrid>
{/if}
