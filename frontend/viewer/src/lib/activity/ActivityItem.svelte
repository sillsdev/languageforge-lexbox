<script lang="ts" module>
  import type {IChangeContext, IChangeEntity, IEntry, IProjectActivity, ISense} from '$lib/dotnet-types';

  export class ChangeWithLazyContext {

    #context?: Promise<IChangeContext>;

    constructor(
      public readonly change: IChangeEntity,
      public readonly activity: IProjectActivity,
      private readonly contextLoader: () => Promise<IChangeContext>,
    ) {}

    get lazyContext(): Promise<IChangeContext> {
      this.#context ??= this.contextLoader();
      return this.#context;
    }
  };
</script>

<script lang="ts">
  import {FormatDate, FormatRelativeDate} from '$lib/components/ui/format';
  import * as Tabs from '$lib/components/ui/tabs';
  import {useHistoryService} from '$lib/services/history-service';
  import {T, t} from 'svelte-i18n-lingui';
  import {VList} from 'virtua/svelte';
  import ActivityItemChangePreview from './ActivityItemChangePreview.svelte';
  import PreviewViewScope from './PreviewViewScope.svelte';
  import CollapsedEntryDiff from './CollapsedEntryDiff.svelte';
  import CollapsedSenseDiff from './CollapsedSenseDiff.svelte';
  import ChangeSummary from './ChangeSummary.svelte';
  import {changeType, describeActivity, factCategory, recognizeTreeCommit, type ChangeFactWithSubject} from './change-summary';
  import {FACT_GLYPH} from './fact-glyph';
  import {formatJsonForUi} from './utils';
  import AuthorLabel from './AuthorLabel.svelte';
  import EntryHeadwordButton from './EntryHeadwordButton.svelte';
  import type {HTMLAttributes} from 'svelte/elements';
  import {cn} from '$lib/utils';
  import * as Popover from '$lib/components/ui/popover';
  import {Button, XButton} from '$lib/components/ui/button';
  import HistoryView from '$lib/history/HistoryView.svelte';
  import {Icon} from '$lib/components/ui/icon';
  import {usePartsOfSpeech} from '$project/data';
  import {assembleEntryAtCommit, assembleSenseAtCommit} from './assemble-entry';

  type Props = HTMLAttributes<HTMLDivElement> & {
    activity: IProjectActivity;
    showHistoryButton?: boolean;
    showClose?: boolean;
    onClose?: () => void;
  }

  const {
    activity,
    showHistoryButton = false,
    showClose = false,
    onClose,
    class: className,
    ...restProps
  }: Props = $props();

  const historyService = useHistoryService();
  const partsOfSpeech = usePartsOfSpeech();
  let openHistoryId = $state<string>()

  const changes = $derived(!historyService.loaded ? undefined : activity.changes.map(change => {
    return new ChangeWithLazyContext(change, activity, () => historyService.loadChangeContext(activity.commitId, change.index));
  }));

  type TreeAssembled =
    | {kind: 'entryTree'; entry: IEntry}
    | {kind: 'senseTree'; sense: ISense; entry: IEntry | undefined};
  type TreeUnit = {
    kind: 'entryTree' | 'senseTree';
    rootEntryId: string;
    changes: ChangeWithLazyContext[];
    headerFact: ChangeFactWithSubject | undefined;
    historyId: string;
    lazyAssembled: Promise<TreeAssembled | undefined>;
  };
  type SingleUnit = {kind: 'single'; change: ChangeWithLazyContext};
  type ActivityUnit = TreeUnit | SingleUnit;

  // The commit's changes grouped by root entry: a group that builds one entry (its creation + its own
  // senses/fields) or one sense (its creation + that sense's examples) collapses to one assembled-preview
  // card, sitting at its first change's position; every other change stays a per-change card in place.
  const units = $derived.by((): ActivityUnit[] | undefined => {
    if (!changes) return undefined;
    // eslint-disable-next-line svelte/prefer-svelte-reactivity
    const groups = new Map<string, ChangeWithLazyContext[]>();
    for (const change of changes) {
      const rootEntryId = rootOf(change);
      if (!rootEntryId) continue;
      let members = groups.get(rootEntryId);
      if (!members) groups.set(rootEntryId, members = []);
      members.push(change);
    }
    // eslint-disable-next-line svelte/prefer-svelte-reactivity
    const trees = new Map<string, TreeUnit>();
    for (const [rootEntryId, members] of groups) {
      const tree = toTreeUnit(rootEntryId, members);
      if (tree) trees.set(rootEntryId, tree);
    }
    return changes.flatMap((change): ActivityUnit[] => {
      const rootEntryId = rootOf(change);
      const tree = rootEntryId ? trees.get(rootEntryId) : undefined;
      if (!tree) return [{kind: 'single', change}];
      return tree.changes[0] === change ? [tree] : [];
    });
  });

  function rootOf(change: ChangeWithLazyContext): string | undefined {
    return activity.changeInfo[change.change.index]?.rootEntryId;
  }

  function toTreeUnit(rootEntryId: string, members: ChangeWithLazyContext[]): TreeUnit | undefined {
    const types = members.map(c => changeType(c.change.change));
    const hasCreateEntry = types.includes('CreateEntryChange');
    if (hasCreateEntry && members.length > 1) return entryTreeUnit(rootEntryId, members);
    const senseCreates = members.filter((_, i) => types[i] === 'CreateSenseChange');
    if (!hasCreateEntry && senseCreates.length === 1
      && types.every(t => t === 'CreateSenseChange' || t === 'CreateExampleSentenceChange')) {
      return senseTreeUnit(rootEntryId, senseCreates[0].change.entityId, members);
    }
    return undefined;
  }

  function entryTreeUnit(rootEntryId: string, members: ChangeWithLazyContext[]): TreeUnit {
    return treeUnit('entryTree', rootEntryId, members, rootEntryId, contexts => {
      const entry = assembleEntryAtCommit(rootEntryId, contexts, partsOfSpeech.current);
      return entry ? {kind: 'entryTree', entry} : undefined;
    });
  }

  function senseTreeUnit(rootEntryId: string, senseId: string, members: ChangeWithLazyContext[]): TreeUnit {
    return treeUnit('senseTree', rootEntryId, members, senseId, contexts => {
      const sense = assembleSenseAtCommit(senseId, contexts, partsOfSpeech.current);
      if (!sense) return undefined;
      // The parent entry (for the headword button) isn't created in this group, so it's pulled from a
      // change context's affectedEntries rather than assembled.
      const affected = contexts.flatMap(c => c.affectedEntries);
      return {kind: 'senseTree', sense, entry: affected.find(e => e.id === rootEntryId) ?? affected[0]};
    });
  }

  function treeUnit(
    kind: TreeUnit['kind'],
    rootEntryId: string,
    members: ChangeWithLazyContext[],
    historyId: string,
    assemble: (contexts: IChangeContext[]) => TreeAssembled | undefined,
  ): TreeUnit {
    let assembled: Promise<TreeAssembled | undefined> | undefined;
    return {
      kind,
      rootEntryId,
      changes: members,
      headerFact: groupHeaderFact(members),
      historyId,
      get lazyAssembled() {
        assembled ??= Promise.all(members.map(c => c.lazyContext)).then(assemble);
        return assembled;
      },
    };
  }

  // The group's one-line summary — the whole tree collapses to its lead sentence ("Created entry X"),
  // since the card body below shows every field of the assembled tree anyway.
  function groupHeaderFact(members: ChangeWithLazyContext[]): ChangeFactWithSubject | undefined {
    const groupChanges = members.map(c => c.change);
    const groupChangeInfos = members.map(c => activity.changeInfo[c.change.index]);
    const groupChangeTypes = [...new Set(groupChanges.map(c => changeType(c.change)).filter((t): t is string => !!t))];
    return recognizeTreeCommit(groupChanges, groupChangeInfos, groupChangeTypes)
      ?? describeActivity(groupChanges, groupChangeInfos)[0];
  }

  // The one entry a change concerns, for the header's entry menu button. Complex-form-component changes
  // concern two entries — their preview panels carry their own labeled headword buttons instead.
  function singleAffectedEntry(context: IChangeContext): IEntry | undefined {
    if (context.entityType === 'ComplexFormComponent') return undefined;
    return context.affectedEntries.length === 1 ? context.affectedEntries[0] : undefined;
  }
</script>

<div {...restProps} class={cn(className, 'grid gap-2 grid-rows-[auto_1fr] h-full min-w-0 min-h-0')}>
  {#if activity}
    <div class="text-sm flex flex-wrap justify-between items-center gap-2">
      {#if showClose && onClose}
        <XButton onclick={onClose} size="icon" />
      {/if}
      <span>
        <span>
          {$t`Author:`}
          <AuthorLabel class="font-semibold" authorId={activity.metadata.authorId} authorName={activity.metadata.authorName} />
        </span>
        {#if activity.changes.length > 1}
          <span>{$t`– (${activity.changes.length} changes)`}</span>
        {/if}
      </span>
      <span>
        <FormatDate date={activity.timestamp}
                    options={{ dateStyle: 'medium', timeStyle: 'medium' }} />
      </span>
      <span class="whitespace-nowrap inline-flex items-center gap-1">
        {#if activity.metadata.extraMetadata['SyncDate']}
          <span>
            <Popover.Root>
              <Popover.InfoTrigger>
                <span class="inline-flex gap-2 items-center">
                  <Icon icon="i-mdi-cloud-outline" class="size-4 shrink-0" />
                  <span>
                    <T msg="Synced: #">
                      <FormatRelativeDate
                        class="font-semibold"
                        date={new Date(activity.metadata.extraMetadata['SyncDate'])} />
                    </T>
                  </span>
                </span>
              </Popover.InfoTrigger>
              <Popover.Content class="w-auto p-2 text-sm text-center max-w-48">
                {$t`The time when you uploaded or downloaded these changes`}
              </Popover.Content>
            </Popover.Root>
          </span>
        {:else}
          <span class="inline-flex gap-2 items-center font-semibold" title={$t`These changes have not been uploaded yet. Ensure you're online and logged in to share your changes.`}>
            <Icon icon="i-mdi-cloud-off-outline" class="size-4 shrink-0 text-muted-foreground" />
            {$t`Not synced`}
          </span>
        {/if}
        <Popover.Root>
          <Popover.Trigger>
            {#snippet child({props})}
              <Button {...props} size="icon-xs" variant="ghost" icon="i-mdi-code-json" title={$t`Commit details`} />
            {/snippet}
          </Popover.Trigger>
          <Popover.Content class="w-auto max-w-96 max-h-80 overflow-auto">
            <div class="whitespace-pre-wrap break-words font-mono text-xs">
              {formatJsonForUi({commitId: activity.commitId, timestamp: activity.timestamp, ...activity.metadata})}
            </div>
          </Popover.Content>
        </Popover.Root>
      </span>
    </div>
    
    {#if openHistoryId}
    <!-- this is a dialog so it doesn't matter where it is in the DOM -->
        <HistoryView bind:open={() => !!openHistoryId, (open) => (open ? undefined : openHistoryId = undefined)} id={openHistoryId} selectedCommitId={activity.commitId}/>
    {/if}
    {#if units}
      {@render changeList(units)}
    {/if}
  {/if}
</div>

<!-- The standard header of every change card: the change-kind glyph plus the same readable sentence the
     activity list shows (the raw change type stays inspectable in the Details tab), and the card's actions —
     the entry menu (generic label; the sentence already names the headword) and History — when the caller
     can name their targets. Deleted entries get no entry menu (nothing to navigate to or edit). The sentence
     and the actions are separate wrap units: on a narrow pane the actions drop to their own right-aligned
     line instead of crushing the sentence into a one-word-per-line column. -->
{#snippet changeHeader(summaryEntry: ChangeFactWithSubject, remaining: number, historyId: string | undefined, entry: IEntry | undefined = undefined)}
  {@const glyph = FACT_GLYPH[factCategory(summaryEntry.fact)]}
  <div class="px-4 pt-2 flex flex-wrap items-center gap-x-1.5 gap-y-1">
    <span class="flex grow basis-52 items-center gap-1.5 min-w-0">
      {#if glyph}<Icon icon={glyph.icon} class="size-4 shrink-0 {glyph.class}" />{/if}
      <span class="min-w-0 line-clamp-2 text-muted-foreground">
        <ChangeSummary fact={summaryEntry.fact} subject={summaryEntry.subject} target={summaryEntry.target} />
        {#if remaining > 0}<span class="ms-1">{$t`(+${remaining} more)`}</span>{/if}
      </span>
    </span>
    {#if (entry && !entry.deletedAt) || (showHistoryButton && historyId)}
      <span class="ms-auto flex items-center gap-1.5">
        {#if entry && !entry.deletedAt}
          <EntryHeadwordButton {entry} generic />
        {/if}
        {#if showHistoryButton && historyId}
          <Button variant="secondary" icon="i-mdi-history" onclick={() => openHistoryId = historyId}>
            {$t`History`}
          </Button>
        {/if}
      </span>
    {/if}
  </div>
{/snippet}

<!-- A collapsed change group (create-entry / create-sense tree) shown as a single card matching the
     per-change cards: same header (the group reads as one change), same tab layout and actions. Details is
     the group's change JSON. -->
{#snippet treeCard(unit: TreeUnit, assembled: TreeAssembled)}
  <div class="change">
    {#if unit.headerFact}
      {@render changeHeader(unit.headerFact, 0, unit.historyId, assembled.entry)}
    {/if}
    <Tabs.Root value="preview" class="px-2 mt-2 grow">
      <Tabs.List class="w-full">
        <Tabs.Trigger class="flex-1" value="preview">{$t`Preview`}</Tabs.Trigger>
        <Tabs.Trigger class="flex-1" value="change">{$t`Details`}</Tabs.Trigger>
      </Tabs.List>
      <div class="pt-1 pb-4 px-2">
        <Tabs.Content value="preview">
          <PreviewViewScope>
            {#if assembled.kind === 'senseTree'}
              <CollapsedSenseDiff sense={assembled.sense} />
            {:else}
              <CollapsedEntryDiff entry={assembled.entry} />
            {/if}
          </PreviewViewScope>
        </Tabs.Content>
        <Tabs.Content value="change">
          <div class="whitespace-pre-wrap break-words font-mono text-sm">
            {formatJsonForUi(unit.changes.map(c => c.change))}
          </div>
        </Tabs.Content>
      </div>
    </Tabs.Root>
  </div>
{/snippet}

<!-- Per-card failure containment: a change whose context fails to load shows an inline error card
     instead of rejecting out of the {#await} and leaving a permanently blank row. -->
{#snippet loadError(error: unknown)}
  <div class="change px-4 py-3">
    <div class="flex items-center gap-2 text-muted-foreground">
      <Icon icon="i-mdi-alert-circle-outline" class="size-4 shrink-0 text-destructive" />
      <span>{$t`Failed to load this change`}</span>
    </div>
    {#if error instanceof Error && error.message}
      <div class="mt-1 ps-6 text-xs text-muted-foreground break-words">{error.message}</div>
    {/if}
  </div>
{/snippet}

{#snippet changeList(items: ActivityUnit[])}
  <div class="change-list flex flex-col gap-4 overflow-auto border rounded">
    {#key items}
      <VList
        class="space-y-2"
        data={items}
        itemSize={700}
        bufferSize={700}
        getKey={(item) => item.kind === 'single'
          ? `${item.change.change.commitId}:${item.change.change.index}`
          : `tree:${item.rootEntryId}`}>
        {#snippet children(unit)}
          {#if unit.kind === 'single'}
            {@render changeCard(unit.change)}
          {:else}
            {#await unit.lazyAssembled}
              <div class="h-[700px]"></div>
            {:then assembled}
              {#if assembled}
                {@render treeCard(unit, assembled)}
              {:else}
                {#each unit.changes as change (change.change.index)}
                  {@render changeCard(change)}
                {/each}
              {/if}
            {:catch error}
              {@render loadError(error)}
            {/await}
          {/if}
        {/snippet}
      </VList>
    {/key}
  </div>
{/snippet}

{#snippet changeCard(changeWithContext: ChangeWithLazyContext)}
  {@const {change, lazyContext} = changeWithContext}
  {@const summaryEntries = describeActivity([change], [activity.changeInfo[change.index]])}
  {#await lazyContext}
    <!-- determines how many rows are initially visible,
     which in turn determines how many changes will be loaded.
     Too big is not much of a problem, it will just stagger subsequent loads -->
    <div class="h-[700px]"></div>
  {:then context}
    <div class="change">
      {#if summaryEntries.length}
        {@render changeHeader(summaryEntries[0], summaryEntries.length - 1, context.snapshot?.id ?? context.previousSnapshot?.id, singleAffectedEntry(context))}
      {:else}
        <div class="px-4 pt-2 flex font-semibold items-center">
          <span class="grow">{context.changeName}</span>
        </div>
      {/if}
      <Tabs.Root value="preview" class="px-2 mt-2 grow">
        <Tabs.List class="w-full">
          <Tabs.Trigger class="flex-1" value="preview">
            {$t`Preview`}
          </Tabs.Trigger>
          <Tabs.Trigger class="flex-1" value="change">{$t`Details`}</Tabs.Trigger>
        </Tabs.List>
        <div class="pt-1 pb-4 px-2">
          <Tabs.Content value="preview">
            <ActivityItemChangePreview {context} {change} />
          </Tabs.Content>
          <Tabs.Content value="change">
            <div class="whitespace-pre-wrap break-words font-mono text-sm">
              {formatJsonForUi(change)}
            </div>
          </Tabs.Content>
        </div>
      </Tabs.Root>
    </div>
  {:catch error}
    {@render loadError(error)}
  {/await}
{/snippet}

<style lang="postcss">
  @reference "#app.css";
  :global(.change-list .sentinel) {
    @apply -mt-4; /* make gap-4 not apply to the infinite-scroll end detector */
  }
  :global(.change-list :not(:last-child) > .change) {
    @apply border-b-2 pb-2 mb-1;
  }
</style>
