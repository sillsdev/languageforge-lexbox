<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import {type IActivityChangeInfo, type IChangeContext, type IChangeEntity, type IComplexFormComponent, type IEntry, type IExampleSentence, type ISense} from '$lib/dotnet-types';
  import {t} from 'svelte-i18n-lingui';
  import {formatJsonForUi} from './utils';
  import {useViewService} from '$lib/views/view-service.svelte';
  import Headwords from '$lib/components/dictionary/Headwords.svelte';
  import DiffEntryPrimitive from '$lib/entry-editor/diff-view/DiffEntryPrimitive.svelte';
  import DiffSensePrimitive from '$lib/entry-editor/diff-view/DiffSensePrimitive.svelte';
  import DiffExamplePrimitive from '$lib/entry-editor/diff-view/DiffExamplePrimitive.svelte';
  import DiffFieldRows from '$lib/entry-editor/diff-view/DiffFieldRows.svelte';
  import {objectDiffRows, type ObjectType} from '$lib/entry-editor/diff-view/diff-fields';
  import DiffAnchor from '$lib/entry-editor/diff-view/DiffAnchor.svelte';
  import {entryAnchor, exampleAnchor, objectAnchor, senseAnchor, type AnchorText} from '$lib/entry-editor/diff-view/anchor-text';
  import {useWritingSystemService} from '$project/data';
  import ActivityChangeHeader, {type Crumb} from './ActivityChangeHeader.svelte';
  import {changeBadge, changeKind, isReorder, parentCrumb} from './change-header';
  import {existing, type SinceThen} from './since-then';
  import {cn} from '$lib/utils';

  type Props = {
    context: IChangeContext,
    /** The change itself, to tell a reorder from an edit */
    change?: IChangeEntity,
    /** The activity list's current names for what the change is about */
    info?: IActivityChangeInfo,
    /** Given for the Later changes tab: diffs the change's result against the entity as it is now */
    since?: SinceThen,
  }

  const { context, change, info, since }: Props = $props();

  const viewService = useViewService();
  const writingSystemService = useWritingSystemService();

  const affectedEntry = $derived(context.affectedEntries.length === 1 ? context.affectedEntries[0] : undefined);

  // An edit to an already-deleted entity (one synced in from a client that didn't know about the delete) still
  // diffs: hiding it would hide that user's work.
  const editOfDeleted = $derived(!!context.previousSnapshot?.deletedAt && !!context.snapshot?.deletedAt);
  // No before means the commit created the entity, no after means it deleted it.
  const before = $derived(editOfDeleted ? context.previousSnapshot : existing(context.previousSnapshot));
  const after = $derived(editOfDeleted ? context.snapshot : existing(context.snapshot));
  const anySnapshot = $derived(context.snapshot ?? context.previousSnapshot);

  // What the diff compares: the change's before and after, or the result of the change and the entity now
  const diffBefore = $derived(since ? since.before : before);
  const diffAfter = $derived(since ? since.after : after);
  // Created or deleted whole: values show plain, the badge and an edge rule say which (see diffAdded)
  const gone = $derived(!!diffBefore && !diffAfter);
  const fresh = $derived(!diffBefore && !!diffAfter);

  const kind = $derived(changeKind(context.entityType));
  const reorder = $derived(isReorder(change));
  const showNote = $derived(!since && !reorder);
  // Later changes describes the entity as it stands now, not what this change did to it
  const badge = $derived(since
    ? (since.status === 'deleted' ? 'deleted' : undefined)
    : anySnapshot && changeBadge(kind, {before, after, reorder}));

  // The identity across the same span the diff shows
  const anchor = $derived.by((): AnchorText | undefined => {
    const currentView = viewService.currentView;
    switch (kind) {
      case 'entry': return entryAnchor(writingSystemService, currentView, diffBefore as IEntry | undefined, diffAfter as IEntry | undefined);
      case 'sense': return senseAnchor(writingSystemService, currentView, diffBefore as ISense | undefined, diffAfter as ISense | undefined);
      case 'example': return exampleAnchor(writingSystemService, currentView, diffBefore as IExampleSentence | undefined, diffAfter as IExampleSentence | undefined);
      case 'object': return objectAnchor(writingSystemService, context.entityType as ObjectType, diffBefore, diffAfter);
      default: return undefined;
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
    if (since || kind !== 'entry' || !before || !linkTo || !anchor) return undefined;
    const current = entryAnchor(writingSystemService, viewService.currentView, undefined, linkTo);
    const changed = (current.after || undefined) !== (anchor.after || undefined) || (current.afterHomograph ?? 0) !== (anchor.afterHomograph ?? 0);
    return changed ? linkTo : undefined;
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
        {$t`Compares the version right after this change with the last version before deletion.`}
      {:else}
        {$t`Compares the version right after this change with the current version.`}
      {/if}
    </p>
  {/if}
{/snippet}

<div class={cn(gone && 'diff-gone', fresh && 'diff-new')}>
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
  {:else if kind === 'complexFormComponent'}
    <!-- The header says it all: which link, between which entries -->
  {:else if kind === 'unknown'}
    <div class="whitespace-pre-wrap font-mono text-sm">
      {formatJsonForUi(anySnapshot)}
    </div>
  {:else}
    {@render sinceNote()}
    <Editor.Root class={cn((gone || fresh) && 'border-s-2 ps-3', gone && 'border-destructive/40 dark:border-destructive/60', fresh && 'border-emerald-500/40 dark:border-emerald-500/60')}>
      <Editor.Grid>
        {#if kind === 'entry'}
          <DiffEntryPrimitive mode="changes" anchor={false} noVisibleChangesNote={showNote} before={diffBefore as IEntry | undefined} after={diffAfter as IEntry | undefined}/>
        {:else if kind === 'sense'}
          <DiffSensePrimitive mode="changes" anchor={false} noVisibleChangesNote={showNote} before={diffBefore as ISense | undefined} after={diffAfter as ISense | undefined}/>
        {:else if kind === 'example'}
          <DiffExamplePrimitive mode="changes" anchor={false} noVisibleChangesNote={showNote} before={diffBefore as IExampleSentence | undefined} after={diffAfter as IExampleSentence | undefined}/>
        {:else}
          <DiffFieldRows mode="changes" noVisibleChangesNote={showNote} rows={objectDiffRows[context.entityType as ObjectType]} before={diffBefore} after={diffAfter}/>
        {/if}
      </Editor.Grid>
    </Editor.Root>
  {/if}
</div>
