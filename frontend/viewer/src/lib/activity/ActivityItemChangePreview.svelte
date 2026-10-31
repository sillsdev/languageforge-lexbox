<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import {type IActivityChangeInfo, type IChangeContext, type IChangeEntity, type IComplexFormComponent, type IEntry, type IExampleSentence, type IObjectWithId, type ISense} from '$lib/dotnet-types';
  import {t} from 'svelte-i18n-lingui';
  import {formatJsonForUi} from './utils';
  import {useViewService} from '$lib/views/view-service.svelte';
  import Headwords from '$lib/components/dictionary/Headwords.svelte';
  import DiffEntryPrimitive from '$lib/entry-editor/diff-view/DiffEntryPrimitive.svelte';
  import DiffSensePrimitive from '$lib/entry-editor/diff-view/DiffSensePrimitive.svelte';
  import DiffExamplePrimitive from '$lib/entry-editor/diff-view/DiffExamplePrimitive.svelte';
  import DiffFieldRows from '$lib/entry-editor/diff-view/DiffFieldRows.svelte';
  import {isObjectType, objectDiffRows} from '$lib/entry-editor/diff-view/diff-fields';
  import DiffAnchor from '$lib/entry-editor/diff-view/DiffAnchor.svelte';
  import {entryAnchor, exampleAnchor, objectAnchor, senseAnchor, type AnchorText} from '$lib/entry-editor/diff-view/anchor-text';
  import {useWritingSystemService} from '$project/data';
  import ActivityChangeHeader, {type Crumb} from './ActivityChangeHeader.svelte';
  import {changeBadge, changeKind, isReorder, parentCrumb} from './change-header';
  import {sinceThen} from './since-then';

  type Props = {
    context: IChangeContext,
    /** The change itself, to tell a reorder from an edit */
    change?: IChangeEntity,
    /** The activity list's current names for what the change is about */
    info?: IActivityChangeInfo,
    /** 'change' diffs the change itself, 'since' diffs its result against the entity as it is now */
    view?: 'change' | 'since',
  }

  const { context, change, info, view = 'change' }: Props = $props();

  const viewService = useViewService();
  const writingSystemService = useWritingSystemService();

  const affectedEntry = $derived(context.affectedEntries.length === 1 ? context.affectedEntries[0] : undefined);

  // A deleted entity's snapshot is its last state marked deleted: the diff shows it as gone.
  function existing(snapshot: IObjectWithId | undefined): IObjectWithId | undefined {
    return snapshot && !snapshot.deletedAt ? snapshot : undefined;
  }
  // An edit to an already-deleted entity (one synced in from a client that didn't know about the delete) still
  // diffs: hiding it would hide that user's work.
  const editOfDeleted = $derived(!!context.previousSnapshot?.deletedAt && !!context.snapshot?.deletedAt);
  // No before means the commit created the entity, no after means it deleted it.
  const before = $derived(editOfDeleted ? context.previousSnapshot : existing(context.previousSnapshot));
  const after = $derived(editOfDeleted ? context.snapshot : existing(context.snapshot));
  const anySnapshot = $derived(context.snapshot ?? context.previousSnapshot);

  const since = $derived(view === 'since' ? sinceThen(context.entityType, context.snapshot, context.currentSnapshot, viewService.currentView) : undefined);
  // What the diff compares: the change's before and after, or the result of the change and the entity now
  const diffBefore = $derived(view === 'since' ? since?.before : before);
  const diffAfter = $derived(view === 'since' ? since?.after : after);

  const kind = $derived(changeKind(context.entityType));
  const reorder = $derived(isReorder(change));
  const showNote = $derived(view === 'change' && !reorder);
  const badge = $derived(anySnapshot && changeBadge(kind, {before, after, reorder}));

  // The identity across the same span the diff shows
  const anchor = $derived.by((): AnchorText | undefined => {
    const currentView = viewService.currentView;
    switch (context.entityType) {
      case 'Entry': return entryAnchor(writingSystemService, currentView, diffBefore as IEntry | undefined, diffAfter as IEntry | undefined);
      case 'Sense': return senseAnchor(writingSystemService, currentView, diffBefore as ISense | undefined, diffAfter as ISense | undefined);
      case 'ExampleSentence': return exampleAnchor(writingSystemService, currentView, diffBefore as IExampleSentence | undefined, diffAfter as IExampleSentence | undefined);
      default: return isObjectType(context.entityType) ? objectAnchor(writingSystemService, context.entityType, diffBefore, diffAfter) : undefined;
    }
  });

  const cfc = $derived(kind === 'complexFormComponent' ? anySnapshot as IComplexFormComponent | undefined : undefined);
  const complexForm = $derived(cfc && context.affectedEntries.find(e => e.id === cfc.complexFormEntryId));
  const component = $derived(cfc && context.affectedEntries.find(e => e.id === cfc.componentEntryId));

  // Something deleted, by this change or since, can't be gone to
  const linkTo = $derived.by(() => {
    // A component link names the component entry, which outlives the link
    if (kind === 'complexFormComponent') return component && !component.deletedAt ? component : undefined;
    return after && affectedEntry && !affectedEntry.deletedAt ? affectedEntry : undefined;
  });
  // Not on Later changes, which already diffs against now. Only when the headword moved on, or it would repeat the anchor
  const now = $derived.by(() => {
    if (view !== 'change' || kind !== 'entry' || !before || !after || !affectedEntry || affectedEntry.deletedAt || !anchor) return undefined;
    const current = entryAnchor(writingSystemService, viewService.currentView, undefined, affectedEntry);
    const changed = (current.after || undefined) !== (anchor.after || undefined) || (current.afterHomograph ?? 0) !== (anchor.afterHomograph ?? 0);
    return changed ? affectedEntry : undefined;
  });

  const crumbs = $derived.by((): Crumb[] => {
    if (kind === 'complexFormComponent' && complexForm && !complexForm.deletedAt) return [{entry: complexForm}];
    return parentCrumb(kind, info).map(text => ({text}));
  });
</script>

{#snippet anchorIdentity()}
  <DiffAnchor {...anchor} />
{/snippet}

{#snippet componentIdentity()}
  {#if component}
    <Headwords entry={component} placeholder={$t`Untitled`} />
  {:else}
    <span>{cfc?.componentHeadword || $t`Untitled`}</span>
  {/if}
{/snippet}

{#snippet sinceNote()}
  {#if since}
    <p class="text-sm mb-2 text-muted-foreground">
      {#if since.status === 'deleted'}
        {$t`Struck: as it was after this change. It has since been deleted.`}
      {:else}
        {$t`Struck: as it was after this change. Highlighted: as it is now.`}
      {/if}
    </p>
  {/if}
{/snippet}

{#if anchor || cfc}
  <div class="mb-3">
    <ActivityChangeHeader label={context.changeName} {crumbs} {badge} {linkTo} {now}
                          identity={cfc ? componentIdentity : anchorIdentity} />
  </div>
{/if}

{#if !anySnapshot}
  <div class="text-muted-foreground p-4 text-center">
    {$t`Preview not available`}
  </div>
{:else if view === 'since' && (!since || since.status === 'unchanged')}
  <!-- the tab is hidden or disabled then -->
{:else if context.entityType === 'Entry'}
  {@render sinceNote()}
  <Editor.Root>
    <Editor.Grid>
      <DiffEntryPrimitive mode="changes" anchor={false} noVisibleChangesNote={showNote} before={diffBefore as IEntry | undefined} after={diffAfter as IEntry | undefined}/>
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'Sense'}
  {@render sinceNote()}
  <Editor.Root>
    <Editor.Grid>
      <DiffSensePrimitive mode="changes" anchor={false} noVisibleChangesNote={showNote} before={diffBefore as ISense | undefined} after={diffAfter as ISense | undefined}/>
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'ExampleSentence'}
  {@render sinceNote()}
  <Editor.Root>
    <Editor.Grid>
      <DiffExamplePrimitive mode="changes" anchor={false} noVisibleChangesNote={showNote} before={diffBefore as IExampleSentence | undefined} after={diffAfter as IExampleSentence | undefined}/>
    </Editor.Grid>
  </Editor.Root>
{:else if isObjectType(context.entityType)}
  {@render sinceNote()}
  <Editor.Root>
    <Editor.Grid>
      <DiffFieldRows mode="changes" noVisibleChangesNote={showNote} rows={objectDiffRows[context.entityType]} before={diffBefore} after={diffAfter}/>
    </Editor.Grid>
  </Editor.Root>
{:else if kind === 'complexFormComponent'}
  <!-- The header says it all: which link, between which entries -->
{:else}
  <div class="whitespace-pre-wrap font-mono text-sm">
    {formatJsonForUi(anySnapshot)}
  </div>
{/if}
