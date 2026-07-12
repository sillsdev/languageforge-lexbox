<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import type {IMultiString, IObjectWithId, IRichMultiString} from '$lib/dotnet-types';
  import {useWritingSystemService} from '$project/data';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {t} from 'svelte-i18n-lingui';
  import DiffMultiString from './DiffMultiString.svelte';
  import DiffRichText from './DiffRichText.svelte';
  import DiffText from './DiffText.svelte';
  import DiffShell from './DiffShell.svelte';
  import DiffMultiSelect from './DiffMultiSelect.svelte';
  import {pt, tvt, type ViewText} from '$lib/views/view-text';
  import {getEntityConfig, type EntityType} from '$lib/views/entity-config';

  // One component for all vocab object types (PartOfSpeech, SemanticDomain, Publication, ComplexFormType,
  // MorphType, WritingSystem, CustomView). They don't share a TS interface beyond IObjectWithId, so fields are
  // read loosely and each row renders only when the field is present on the before or after snapshot.
  // The field-completeness guardrail is `vocab-diff-coverage.test.ts`, which parses the `present('…')`
  // rows below — when a vocab model gains a field, add a row here (or ignore it in vocab-diff-fields.ts).
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
    return isRichMultiString(value) ? value : undefined;
  }

  function asText(value: unknown): string | undefined {
    if (typeof value === 'string') return value;
    if (typeof value === 'boolean') return value ? $t`Yes` : $t`No`;
    if (typeof value === 'number') return String(value);
    return undefined;
  }

  // Readable, app-consistent names for enum fields (CustomView.base, WritingSystem.type). Matched against both
  // the string enum name and its numeric value so it works whichever the wire sends. Unknown values fall
  // through to asText so a new enum member still shows *something* rather than vanishing.
  function viewBaseLabel(key: string): string | undefined {
    if (key === 'FwLite') return $t`FieldWorks Lite`;
    if (key === 'FieldWorks') return $t`FieldWorks Classic`;
    return undefined;
  }
  function wsTypeLabel(key: string): string | undefined {
    if (key === '0' || key === 'Vernacular') return $t`Vernacular`;
    if (key === '1' || key === 'Analysis') return $t`Analysis`;
    return undefined;
  }

  function enumText(value: unknown, label: (key: string) => string | undefined): string | undefined {
    if (value === undefined || value === null) return undefined;
    if (typeof value !== 'string' && typeof value !== 'number') return asText(value);
    return label(String(value)) ?? asText(value);
  }

  // `name` is an IMultiString on most types but a plain string on WritingSystem/CustomView.
  const nameIsMultiString = $derived(isMultiString(b?.name) || isMultiString(a?.name));
  // `abbreviation` is an IMultiString on MorphType but a plain string on WritingSystem.
  const abbreviationIsMultiString = $derived(isMultiString(b?.abbreviation) || isMultiString(a?.abbreviation));

  // Resolves a CustomView field id to the same view-aware label the entry editor shows. Falls back to the
  // raw id for fields not in entityConfig (e.g. a custom field added after this code was last updated).
  function viewFieldLabel(entityType: EntityType, fieldId: string): string {
    const entity = getEntityConfig(entityType);
    const field = fieldId === '$label' ? undefined : (entity[fieldId] as {label: ViewText} | undefined);
    return field ? pt($tvt(field.label), viewService.currentView) : fieldId;
  }
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

{#snippet enumRow(label: string, field: string, valueLabel: (key: string) => string | undefined)}
  <Editor.Field.Root>
    <Editor.Field.Title name={label} />
    <Editor.Field.Body>
      <DiffShell>
        <DiffText before={enumText(b?.[field], valueLabel)} after={enumText(a?.[field], valueLabel)} />
      </DiffShell>
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

<!-- Custom-view list rows: field lists (IViewField[] → fieldId), writing-system lists (IViewWritingSystem[]
  → wsId), and plain string lists (exemplars). Rendered as a DiffMultiSelect so add/remove reads consistently. -->
{#snippet viewFieldsRow(label: string, field: string, entityType: EntityType)}
  <Editor.Field.Root>
    <Editor.Field.Title name={label} />
    <Editor.Field.Body>
      <DiffMultiSelect before={b?.[field] as {fieldId: string}[] | undefined} after={a?.[field] as {fieldId: string}[] | undefined}
                       idSelector={(f) => f.fieldId} labelSelector={(f) => viewFieldLabel(entityType, f.fieldId)}
                       sortKey={(f) => f.fieldId} />
    </Editor.Field.Body>
  </Editor.Field.Root>
{/snippet}

{#snippet wsListRow(label: string, field: string)}
  <Editor.Field.Root>
    <Editor.Field.Title name={label} />
    <Editor.Field.Body>
      <DiffMultiSelect before={b?.[field] as {wsId: string}[] | undefined} after={a?.[field] as {wsId: string}[] | undefined}
                       idSelector={(w) => w.wsId} labelSelector={(w) => w.wsId} />
    </Editor.Field.Body>
  </Editor.Field.Root>
{/snippet}

{#snippet stringListRow(label: string, field: string)}
  <Editor.Field.Root>
    <Editor.Field.Title name={label} />
    <Editor.Field.Body>
      <DiffMultiSelect before={b?.[field] as string[] | undefined} after={a?.[field] as string[] | undefined}
                       idSelector={(s) => s} labelSelector={(s) => s} />
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
    {@render enumRow($t`Type`, 'type', wsTypeLabel)}
  {/if}

  {#if present('isAudio')}
    {@render textRow($t`Audio`, 'isAudio')}
  {/if}

  {#if present('isMain')}
    {@render textRow($t`Main`, 'isMain')}
  {/if}

  {#if present('base')}
    {@render enumRow($t`Base view`, 'base', viewBaseLabel)}
  {/if}

  {#if present('entryFields')}
    {@render viewFieldsRow($t`Entry fields`, 'entryFields', 'entry')}
  {/if}

  {#if present('senseFields')}
    {@render viewFieldsRow($t`Sense fields`, 'senseFields', 'sense')}
  {/if}

  {#if present('exampleFields')}
    {@render viewFieldsRow($t`Example fields`, 'exampleFields', 'example')}
  {/if}

  {#if present('vernacular')}
    {@render wsListRow($t`Vernacular writing systems`, 'vernacular')}
  {/if}

  {#if present('analysis')}
    {@render wsListRow($t`Analysis writing systems`, 'analysis')}
  {/if}

  {#if present('exemplars')}
    {@render stringListRow($t`Exemplars`, 'exemplars')}
  {/if}
</Editor.SubGrid>
