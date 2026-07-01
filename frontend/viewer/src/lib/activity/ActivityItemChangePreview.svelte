<script lang="ts">
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import * as Editor from '$lib/components/editor';
  import {type IChangeContext, type IComplexFormComponent, type IEntry, type IExampleSentence, type ISense} from '$lib/dotnet-types';
  import {t} from 'svelte-i18n-lingui';
  import {formatJsonForUi} from './utils';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import {Button} from '$lib/components/ui/button';
  import {pt, tvt} from '$lib/views/view-text';
  import {entryBrowseParams} from '$lib/utils/search-params';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {entityConfig} from '$lib/views/entity-config';
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils';
  import {Link} from 'svelte-routing';
  import Headwords from '$lib/components/dictionary/Headwords.svelte';
  import DiffEntryPrimitive from '$lib/entry-editor/diff-view/DiffEntryPrimitive.svelte';
  import DiffSensePrimitive from '$lib/entry-editor/diff-view/DiffSensePrimitive.svelte';
  import DiffExamplePrimitive from '$lib/entry-editor/diff-view/DiffExamplePrimitive.svelte';
  import DiffVocabPrimitive from '$lib/entry-editor/diff-view/DiffVocabPrimitive.svelte';
  import DiffEntryLinkList from '$lib/entry-editor/diff-view/DiffEntryLinkList.svelte';
  import PreviewViewScope from './PreviewViewScope.svelte';

  type Props = {
    context: IChangeContext,
  }

  const { context }: Props = $props();

  const multiWindowService = useMultiWindowService();
  const viewService = useViewService();

  // `.order` isn't in the generated TS type (Harmony's orderable-entity concept) but is present at runtime.
  function orderOf(item: unknown): number {
    if (!item || typeof item !== 'object') return 0;
    const r = item as Record<string, unknown>;
    const v = r.order ?? r.Order;
    return typeof v === 'number' ? v : 0;
  }

  // Reads a ComplexFormComponent-shaped object from an IObjectWithId; the CFC snapshot has NO `id`
  // (the C# `Id` property is `[MiniLcmInternal]` and stripped by MiniLcmJson.IgnoreInternalMiniLcmProperties),
  // and `.components[i]` on an entry also arrives without `id` for the same reason. Identity is the
  // composite (complexFormEntryId, componentEntryId, componentSenseId). Returns undefined if the
  // snapshot represents a deleted CFC (Harmony still emits a snapshot with `DeletedAt` set for a
  // DeleteChange, but semantically the CFC is "gone" and belongs on the removal side of the diff).
  function cfcAny(obj: unknown): IComplexFormComponent | undefined {
    if (!obj || typeof obj !== 'object') return undefined;
    const r = obj as Record<string, unknown>;
    const s = (camel: string, pascal: string): string | undefined => {
      const v = r[camel] ?? r[pascal];
      return typeof v === 'string' ? v : undefined;
    };
    if (r.deletedAt ?? r.DeletedAt) return undefined;
    const complexFormEntryId = s('complexFormEntryId', 'ComplexFormEntryId');
    const componentEntryId = s('componentEntryId', 'ComponentEntryId');
    if (!complexFormEntryId || !componentEntryId) return undefined;
    return {
      id: '',
      complexFormEntryId,
      componentEntryId,
      complexFormHeadword: s('complexFormHeadword', 'ComplexFormHeadword'),
      componentHeadword: s('componentHeadword', 'ComponentHeadword'),
      componentSenseId: s('componentSenseId', 'ComponentSenseId'),
    } as IComplexFormComponent;
  }

  // Composite identity: two CFCs are "the same link" iff they share both endpoints (and any sense id).
  function cfcKey(cfc: {complexFormEntryId?: string; componentEntryId?: string; componentSenseId?: string | null | undefined}): string {
    return `${cfc.complexFormEntryId ?? ''}::${cfc.componentEntryId ?? ''}::${cfc.componentSenseId ?? ''}`;
  }

  // Synthesize the before/after list of links for THIS entry, given what the change payload says.
  // Perspective-aware: whether the CFC "belonged" to this entry before/after depends on which endpoint
  // (complexFormEntryId when this is the complex form; componentEntryId when this is the component)
  // points at us.
  //
  // `current` is the entry's live-state components/complexForms list — which can be **stale relative to
  // this commit**: a CFC that was added by this change and later removed will be absent from `current`
  // even though it was present right after this commit. We compensate by re-injecting endpoints implied
  // by the change payload into both sides. Deduped by cfcKey so a stale duplicate can't crash {#each}.
  function synthesizeLists(
    current: IComplexFormComponent[],
    entryId: string,
    isComplexForm: boolean,
    beforeCfc: IComplexFormComponent | undefined,
    afterCfc: IComplexFormComponent | undefined,
  ): {before: IComplexFormComponent[]; after: IComplexFormComponent[]} {
    const belongs = (cfc: IComplexFormComponent | undefined) =>
      cfc && (isComplexForm ? cfc.complexFormEntryId : cfc.componentEntryId) === entryId;
    const belongsBefore = belongs(beforeCfc);
    const belongsAfter = belongs(afterCfc);

    // Re-inject afterCfc into `after` if it belongs but was later removed from current.
    let after = current;
    if (belongsAfter) {
      const afterKey = cfcKey(afterCfc!);
      if (!after.some((c) => cfcKey(c) === afterKey)) after = [...after, afterCfc!];
    }

    let before: IComplexFormComponent[];
    if (belongsBefore && !belongsAfter) {
      // Removal from THIS entry's list — the CFC left our side, so re-introduce it in the pre-change list.
      before = [...after, beforeCfc!];
    } else if (belongsBefore && belongsAfter) {
      // Present on both sides (an update/reorder that kept us as one endpoint) — swap in the pre-change form.
      const afterKey = cfcKey(afterCfc!);
      let swapped = false;
      before = after.map((c) => {
        if (cfcKey(c) === afterKey) { swapped = true; return beforeCfc!; }
        return c;
      });
      if (!swapped) before = [...before, beforeCfc!];
    } else if (!belongsBefore && belongsAfter) {
      // Addition to THIS entry's list — drop the newly-linked cfc from the pre-change list.
      const afterKey = cfcKey(afterCfc!);
      before = after.filter((c) => cfcKey(c) !== afterKey);
    } else {
      // Neither snapshot points at us: this entry surfaced from the affected-entries walk but the change
      // doesn't directly touch its link list. Show its current list as unchanged.
      before = after;
    }
    return {before: dedupe(before), after: dedupe(after)};
  }

  function dedupe(list: IComplexFormComponent[]): IComplexFormComponent[] {
    // eslint-disable-next-line svelte/prefer-svelte-reactivity
    const seen = new Set<string>();
    return list.filter((c) => {
      const k = cfcKey(c);
      if (seen.has(k)) return false;
      seen.add(k);
      return true;
    });
  }
</script>

{#snippet entryButton(entry: IEntry)}
  {@const deleted = Boolean(entry.deletedAt)}
  <DropdownMenu.Root>
    <DropdownMenu.Trigger class={cn('text-base w-fit mr-2 justify-between flex-wrap whitespace-break-spaces text-start min-h-max py-1.5', deleted && 'pointer-events-none')}>
      {#snippet child({props})}
        <Button {...props} variant="secondary" size="sm">
          <Headwords {entry} showHomograph placeholder={$t`Untitled`} />
          {#if !deleted}
            <Icon icon="i-mdi-dots-vertical" />
          {:else}
            <span class="text-destructive font-normal">
              ({$t`Deleted`})
            </span>
          {/if}
        </Button>
      {/snippet}
    </DropdownMenu.Trigger>
    <DropdownMenu.Content align="start">
      <DropdownMenu.Group>
        {#if multiWindowService}
          <DropdownMenu.Item class="cursor-pointer" onSelect={() => multiWindowService.openEntryInNewWindow(entry.id)}>
            <Icon icon="i-mdi-open-in-new" />
            {$t`Open in new Window`}
          </DropdownMenu.Item>
        {/if}
        <DropdownMenu.Item class="cursor-pointer" onclick={e => e.preventDefault()}>
          {#snippet child({props})}
            <Link {...props} to="./browse?{entryBrowseParams(entry.id)}">
              <Icon icon="i-mdi-link" />
              {$t`Go to ${pt($t`Entry`, $t`Word`, viewService.currentView)}`}
            </Link>
          {/snippet}
        </DropdownMenu.Item>
      </DropdownMenu.Group>
    </DropdownMenu.Content>
  </DropdownMenu.Root>
{/snippet}

{#if context.affectedEntries.length === 1 && context.entityType !== 'ComplexFormComponent'}
  <div class="flex flex-wrap gap-2 mb-3 items-center">
    {@render entryButton(context.affectedEntries[0])}
  </div>
{/if}

{#if context.entityType === 'ComplexFormComponent' && context.affectedEntries.length}
  {@const beforeCfc = cfcAny(context.previousSnapshot)}
  {@const afterCfc = cfcAny(context.snapshot)}
  {@const anyCfc = afterCfc ?? beforeCfc}
  <!-- A reorder change has both snapshots with identical endpoints (only .order changed); the component
       entry's "Complex forms" list is unaffected, so only show the complex-form side. -->
  {@const isReorder = !!beforeCfc && !!afterCfc
    && beforeCfc.complexFormEntryId === afterCfc.complexFormEntryId
    && beforeCfc.componentEntryId === afterCfc.componentEntryId
    && (beforeCfc.componentSenseId ?? null) === (afterCfc.componentSenseId ?? null)}
  {@const uniqueEntries = context.affectedEntries
    .filter((e, idx, arr) => arr.findIndex((o) => o.id === e.id) === idx)
    .filter((e) => !isReorder || (anyCfc && e.id === anyCfc.complexFormEntryId))}
  <PreviewViewScope>
    <Editor.Root>
      <Editor.Grid>
        {#each uniqueEntries as entry, i (entry.id)}
          {@const isComplexForm = anyCfc ? entry.id === anyCfc.complexFormEntryId : false}
          {@const fieldId = isComplexForm ? 'components' : 'complexForms'}
          {@const currentRaw = (isComplexForm ? entry.components : entry.complexForms) ?? []}
          <!-- Sort by order so a reorder change is visually reflected in the after list. `order` isn't on the TS type but Harmony stores it. -->
          {@const current = [...currentRaw].sort((a, b) => orderOf(a) - orderOf(b))}
          {@const {before, after} = synthesizeLists(current, entry.id, isComplexForm, beforeCfc, afterCfc)}
          <div class={cn('col-span-full', i > 0 && 'mt-4')}>{@render entryButton(entry)}</div>
          <Editor.SubGrid class="gap-2" style="grid-template-areas: '{fieldId} {fieldId} {fieldId}'">
            <Editor.Field.Root {fieldId}>
              <Editor.Field.Title
                name={$tvt(isComplexForm ? entityConfig.entry.components.label : entityConfig.entry.complexForms.label)}
                helpId={isComplexForm ? entityConfig.entry.components.helpId : entityConfig.entry.complexForms.helpId} />
              <Editor.Field.Body>
                <DiffEntryLinkList
                  {before}
                  {after}
                  getEntryId={(link) => (isComplexForm ? link.componentEntryId : link.complexFormEntryId) ?? ''}
                  getKey={(link) => cfcKey(link)}
                  getHeadword={(link) => (isComplexForm ? link.componentHeadword : link.complexFormHeadword) || undefined}
                  touched={(link) => !!anyCfc && cfcKey(link) === cfcKey(anyCfc)} />
              </Editor.Field.Body>
            </Editor.Field.Root>
          </Editor.SubGrid>
        {/each}
      </Editor.Grid>
    </Editor.Root>
  </PreviewViewScope>
{:else}
  <PreviewViewScope>
    {#if !context.snapshot && !context.previousSnapshot}
      <div class="text-muted-foreground p-4 text-center">
        {$t`Preview not available for this type of change`}
      </div>
    {:else if context.entityType === 'Entry'}
      <Editor.Root>
        <Editor.Grid>
          <DiffEntryPrimitive before={context.previousSnapshot as IEntry | undefined} after={context.snapshot as IEntry | undefined} />
        </Editor.Grid>
      </Editor.Root>
    {:else if context.entityType === 'Sense'}
      <Editor.Root>
        <Editor.Grid>
          <DiffSensePrimitive before={context.previousSnapshot as ISense | undefined} after={context.snapshot as ISense | undefined} />
        </Editor.Grid>
      </Editor.Root>
    {:else if context.entityType === 'ExampleSentence'}
      <Editor.Root>
        <Editor.Grid>
          <DiffExamplePrimitive before={context.previousSnapshot as IExampleSentence | undefined} after={context.snapshot as IExampleSentence | undefined} />
        </Editor.Grid>
      </Editor.Root>
    {:else if context.entityType === 'PartOfSpeech'
      || context.entityType === 'SemanticDomain'
      || context.entityType === 'Publication'
      || context.entityType === 'ComplexFormType'
      || context.entityType === 'MorphType'
      || context.entityType === 'WritingSystem'
      || context.entityType === 'CustomView'}
      <Editor.Root>
        <Editor.Grid>
          <DiffVocabPrimitive before={context.previousSnapshot} after={context.snapshot} />
        </Editor.Grid>
      </Editor.Root>
    {:else}
      <div class="whitespace-pre-wrap font-mono text-sm">
        {formatJsonForUi(context.snapshot ?? context.previousSnapshot ?? {})}
      </div>
    {/if}
  </PreviewViewScope>
{/if}
