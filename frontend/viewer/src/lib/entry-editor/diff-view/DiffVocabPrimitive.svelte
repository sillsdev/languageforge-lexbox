<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import type {IMultiString, IObjectWithId} from '$lib/dotnet-types';
  import {Label} from '$lib/components/ui/label';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {useWritingSystemService} from '$project/data';
  import {t} from 'svelte-i18n-lingui';
  import DiffMultiString from './DiffMultiString.svelte';
  import DiffText from './DiffText.svelte';
  import DiffShell from './DiffShell.svelte';

  // One component for all vocab object types (PartOfSpeech, SemanticDomain, Publication, ComplexFormType,
  // MorphType, WritingSystem, CustomView). They don't share a TS interface beyond IObjectWithId, so fields are
  // read loosely and each row renders only when the field is present on the before or after snapshot.
  let {before, after}: {before?: IObjectWithId; after?: IObjectWithId} = $props();

  const writingSystemService = useWritingSystemService();
  const viewService = useViewService();
  const analysisWss = $derived(writingSystemService.viewAnalysis(viewService.currentView));

  type Loose = Record<string, unknown>;
  const b = $derived(before as Loose | undefined);
  const a = $derived(after as Loose | undefined);

  function present(field: string): boolean {
    return b?.[field] !== undefined || a?.[field] !== undefined;
  }

  function isMultiString(value: unknown): value is IMultiString {
    return typeof value === 'object' && value !== null;
  }

  function multiString(value: unknown): IMultiString | undefined {
    return isMultiString(value) ? value : undefined;
  }

  function asText(value: unknown): string | undefined {
    return typeof value === 'string' ? value : undefined;
  }

  // `name` is an IMultiString on most types but a plain string on WritingSystem/CustomView.
  const nameIsMultiString = $derived(isMultiString(b?.name) || isMultiString(a?.name));
</script>

{#snippet textRow(label: string, field: string)}
  <div class="grid gap-y-1 @lg/editor:grid-cols-subgrid col-span-full items-baseline">
    <Label>{label}</Label>
    <DiffShell>
      <DiffText before={asText(b?.[field])} after={asText(a?.[field])} />
    </DiffShell>
  </div>
{/snippet}

{#snippet multiStringRow(label: string, field: string)}
  <div class="grid gap-y-1 @lg/editor:grid-cols-subgrid col-span-full items-baseline">
    <Label>{label}</Label>
    <DiffMultiString before={multiString(b?.[field])} after={multiString(a?.[field])} writingSystems={analysisWss} />
  </div>
{/snippet}

<Editor.SubGrid class="gap-2">
  {#if present('name')}
    {#if nameIsMultiString}
      {@render multiStringRow($t`Name`, 'name')}
    {:else}
      {@render textRow($t`Name`, 'name')}
    {/if}
  {/if}

  {#if present('abbreviation')}
    {#if isMultiString(b?.abbreviation) || isMultiString(a?.abbreviation)}
      {@render multiStringRow($t`Abbreviation`, 'abbreviation')}
    {:else}
      {@render textRow($t`Abbreviation`, 'abbreviation')}
    {/if}
  {/if}

  {#if present('code')}
    {@render textRow($t`Code`, 'code')}
  {/if}

  {#if present('prefix')}
    {@render textRow($t`Prefix`, 'prefix')}
  {/if}

  {#if present('postfix')}
    {@render textRow($t`Postfix`, 'postfix')}
  {/if}

  {#if present('wsId')}
    {@render textRow($t`Code`, 'wsId')}
  {/if}

  {#if present('font')}
    {@render textRow($t`Font`, 'font')}
  {/if}
</Editor.SubGrid>
