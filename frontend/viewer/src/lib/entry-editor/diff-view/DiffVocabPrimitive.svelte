<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import type {IMultiString, IObjectWithId, IRichMultiString} from '$lib/dotnet-types';
  import {asString, useWritingSystemService} from '$project/data';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {t} from 'svelte-i18n-lingui';
  import DiffMultiString from './DiffMultiString.svelte';
  import DiffRichText from './DiffRichText.svelte';
  import DiffText from './DiffText.svelte';
  import DiffShell from './DiffShell.svelte';

  // One component for all vocab object types (PartOfSpeech, SemanticDomain, Publication, ComplexFormType,
  // MorphType, WritingSystem, CustomView). They don't share a TS interface beyond IObjectWithId, so fields are
  // read loosely and each row renders only when the field is present on the before or after snapshot.
  // Fields covered (any type that HAS them): name, abbreviation, code, prefix, postfix, wsId, font, kind, type,
  // description, isMain, secondaryOrder. See `vocab-diff-coverage.test.ts` for the field-completeness guardrail.
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

  function isRichMultiString(value: unknown): value is IRichMultiString {
    // RichMultiString values are objects whose members are {spans: [...]}. We accept anything object-shaped and
    // let DiffRichText's asString(...) render fallback handle malformed input.
    return typeof value === 'object' && value !== null;
  }

  function multiString(value: unknown): IMultiString | undefined {
    return isMultiString(value) ? value : undefined;
  }

  function richMultiString(value: unknown): IRichMultiString | undefined {
    return isRichMultiString(value) ? (value as IRichMultiString) : undefined;
  }

  function asText(value: unknown): string | undefined {
    if (typeof value === 'string') return value;
    if (typeof value === 'boolean') return value ? $t`Yes` : $t`No`;
    if (typeof value === 'number') return String(value);
    return undefined;
  }

  // `name` is an IMultiString on most types but a plain string on WritingSystem/CustomView.
  const nameIsMultiString = $derived(isMultiString(b?.name) || isMultiString(a?.name));
  // `abbreviation` is an IMultiString on MorphType but a plain string on WritingSystem.
  const abbreviationIsMultiString = $derived(isMultiString(b?.abbreviation) || isMultiString(a?.abbreviation));
</script>

{#snippet textRow(label: string, field: string)}
  <Editor.Field.Root>
    <Editor.Field.Title name={label} />
    <Editor.Field.Body>
      <DiffShell>
        <DiffText before={asText(b?.[field])} after={asText(a?.[field])} />
      </DiffShell>
    </Editor.Field.Body>
  </Editor.Field.Root>
{/snippet}

{#snippet multiStringRow(label: string, field: string)}
  <Editor.Field.Root>
    <Editor.Field.Title name={label} />
    <Editor.Field.Body subGrid>
      <DiffMultiString before={multiString(b?.[field])} after={multiString(a?.[field])} writingSystems={analysisWss} />
    </Editor.Field.Body>
  </Editor.Field.Root>
{/snippet}

{#snippet richMultiStringRow(label: string, field: string)}
  <Editor.Field.Root>
    <Editor.Field.Title name={label} />
    <Editor.Field.Body subGrid>
      <DiffRichText before={richMultiString(b?.[field])} after={richMultiString(a?.[field])} writingSystems={analysisWss} />
    </Editor.Field.Body>
  </Editor.Field.Root>
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
    {#if abbreviationIsMultiString}
      {@render multiStringRow($t`Abbreviation`, 'abbreviation')}
    {:else}
      {@render textRow($t`Abbreviation`, 'abbreviation')}
    {/if}
  {/if}

  {#if present('code')}
    {@render textRow($t`Code`, 'code')}
  {/if}

  {#if present('kind')}
    {@render textRow($t`Kind`, 'kind')}
  {/if}

  {#if present('description')}
    {@render richMultiStringRow($t`Description`, 'description')}
  {/if}

  {#if present('prefix')}
    {@render textRow($t`Prefix`, 'prefix')}
  {/if}

  {#if present('postfix')}
    {@render textRow($t`Postfix`, 'postfix')}
  {/if}

  {#if present('wsId')}
    {@render textRow($t`Writing system code`, 'wsId')}
  {/if}

  {#if present('font')}
    {@render textRow($t`Font`, 'font')}
  {/if}

  {#if present('type')}
    {@render textRow($t`Type`, 'type')}
  {/if}

  {#if present('isMain')}
    {@render textRow($t`Main`, 'isMain')}
  {/if}
</Editor.SubGrid>
