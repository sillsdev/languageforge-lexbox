<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import {Switch} from '$lib/components/ui/switch';
  import {type IActivityChangeInfo, type IChangeContext, type IChangeEntity, type IComplexFormComponent, type IEntry, type IExampleSentence, type IObjectWithId, type IProjectActivity, type ISense} from '$lib/dotnet-types';
  import EntryEditorPrimitive from '$lib/entry-editor/object-editors/EntryEditorPrimitive.svelte';
  import ExampleEditorPrimitive from '$lib/entry-editor/object-editors/ExampleEditorPrimitive.svelte';
  import SenseEditorPrimitive from '$lib/entry-editor/object-editors/SenseEditorPrimitive.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {formatJsonForUi} from './utils';
  import FormatRelativeDate from '$lib/components/ui/format/format-relative-date.svelte';
  import {useViewService} from '$lib/views/view-service.svelte';
  import Headwords from '$lib/components/dictionary/Headwords.svelte';
  import {cn} from '$lib/utils';
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

  type Props = {
    activity: IProjectActivity,
    context: IChangeContext,
    /** The change itself, to tell a reorder from an edit */
    change?: IChangeEntity,
    /** The activity list's current names for what the change is about */
    info?: IActivityChangeInfo,
  }

  const { activity, context, change, info }: Props = $props();

  const viewService = useViewService();
  const writingSystemService = useWritingSystemService();

  const affectedEntry = $derived(context.affectedEntries.length === 1 ? context.affectedEntries[0] : undefined);
  // Only these snapshot types can be found again inside the affected entry, so only they get the current-version toggle
  const hasCurrentVersion = $derived(['Entry', 'Sense', 'ExampleSentence'].includes(context.entityType));

  // A deleted entity's snapshot is its last state marked deleted: the diff shows it as gone.
  function existing(snapshot: IObjectWithId | undefined): IObjectWithId | undefined {
    return snapshot && !snapshot.deletedAt ? snapshot : undefined;
  }
  // No before means the commit created the entity, no after means it deleted it.
  const before = $derived(existing(context.previousSnapshot));
  const after = $derived(existing(context.snapshot));
  const anySnapshot = $derived(context.snapshot ?? context.previousSnapshot);

  let currentEntity = $derived.by(() => {
    if (!affectedEntry || !anySnapshot) return undefined;

    if (context.entityType === 'Entry') {
      return affectedEntry;
    } else if (context.entityType === 'Sense') {
      const senseId = anySnapshot.id;
      return affectedEntry.senses.find((s) => s.id === senseId);
    } else if (context.entityType === 'ExampleSentence') {
      const exampleId = anySnapshot.id;
      for (const sense of affectedEntry.senses) {
        const example = sense.exampleSentences.find((ex) => ex.id === exampleId);
        if (example) return example;
      }
    }
    return undefined;
  });
  let selectedShowCurrent = $derived(false);
  let showCurrent = $derived(currentEntity && selectedShowCurrent);

  const kind = $derived(changeKind(context.entityType));
  const reorder = $derived(isReorder(change));
  const badge = $derived(anySnapshot && changeBadge(kind, {before, after, reorder}));

  // The historical identity: what the entity was called before and after this change
  const anchor = $derived.by((): AnchorText | undefined => {
    const view = viewService.currentView;
    switch (context.entityType) {
      case 'Entry': return entryAnchor(writingSystemService, view, before as IEntry | undefined, after as IEntry | undefined);
      case 'Sense': return senseAnchor(writingSystemService, view, before as ISense | undefined, after as ISense | undefined);
      case 'ExampleSentence': return exampleAnchor(writingSystemService, view, before as IExampleSentence | undefined, after as IExampleSentence | undefined);
      default: return isObjectType(context.entityType) ? objectAnchor(writingSystemService, context.entityType, before, after) : undefined;
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
  const nowDeleted = $derived(kind === 'entry' && !!after && !!affectedEntry?.deletedAt);
  // Only shown when the headword moved on since, or "Now" would just repeat the anchor
  const now = $derived.by(() => {
    if (kind !== 'entry' || !before || !after || !affectedEntry || affectedEntry.deletedAt || !anchor) return undefined;
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

{#if anchor || cfc}
  <div class="mb-3">
    <ActivityChangeHeader label={context.changeName} {crumbs} {badge} {linkTo} {now} {nowDeleted}
                          identity={cfc ? componentIdentity : anchorIdentity} />
  </div>
{/if}

{#if hasCurrentVersion && affectedEntry}
  <div class="flex flex-wrap gap-2 mb-3 items-center">
    <label class={cn(
      'w-fit flex items-center gap-2 border rounded p-2 px-4 bg-secondary/25',
      currentEntity && 'cursor-pointer')}>
      <FormatRelativeDate date={activity.timestamp}
                          live
                          actualDateOptions={{ dateStyle: 'medium', timeStyle: 'short' }}/>
      <Switch disabled={!currentEntity} bind:checked={selectedShowCurrent} class="text-destructive" />
      {#if currentEntity}
        <span>{$t`Current version`}</span>
      {:else}
        <span class="text-destructive">{$t`Deleted`}</span>
      {/if}
    </label>
  </div>
{/if}

{#if !anySnapshot}
  <div class="text-muted-foreground p-4 text-center">
    {$t`Preview not available`}
  </div>
{:else if showCurrent && context.entityType === 'Entry'}
  <Editor.Root>
    <Editor.Grid>
      <EntryEditorPrimitive modalMode readonly entry={currentEntity as IEntry}/>
    </Editor.Grid>
  </Editor.Root>
{:else if showCurrent && context.entityType === 'Sense'}
  <Editor.Root>
    <Editor.Grid>
        <SenseEditorPrimitive readonly sense={currentEntity as ISense}/>
    </Editor.Grid>
  </Editor.Root>
{:else if showCurrent && context.entityType === 'ExampleSentence'}
  <Editor.Root>
    <Editor.Grid>
        <ExampleEditorPrimitive readonly example={currentEntity as IExampleSentence}/>
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'Entry'}
  <Editor.Root>
    <Editor.Grid>
      <DiffEntryPrimitive mode="changes" anchor={false} noVisibleChangesNote={!reorder} before={before as IEntry | undefined} after={after as IEntry | undefined}/>
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'Sense'}
  <Editor.Root>
    <Editor.Grid>
      <DiffSensePrimitive mode="changes" anchor={false} noVisibleChangesNote={!reorder} before={before as ISense | undefined} after={after as ISense | undefined}/>
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'ExampleSentence'}
  <Editor.Root>
    <Editor.Grid>
      <DiffExamplePrimitive mode="changes" anchor={false} noVisibleChangesNote={!reorder} before={before as IExampleSentence | undefined} after={after as IExampleSentence | undefined}/>
    </Editor.Grid>
  </Editor.Root>
{:else if isObjectType(context.entityType)}
  <Editor.Root>
    <Editor.Grid>
      <DiffFieldRows mode="changes" noVisibleChangesNote={!reorder} rows={objectDiffRows[context.entityType]} {before} {after}/>
    </Editor.Grid>
  </Editor.Root>
{:else if kind === 'complexFormComponent'}
  <!-- The header says it all: which link, between which entries -->
{:else}
  <div class="whitespace-pre-wrap font-mono text-sm">
    {formatJsonForUi(anySnapshot)}
  </div>
{/if}
