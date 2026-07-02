<script lang="ts" module>
  import type {IChangeContext, IChangeEntity, IEntry, IProjectActivity} from '$lib/dotnet-types';

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
  import EntryEditor from '$lib/entry-editor/object-editors/EntryEditor.svelte';
  import {formatJsonForUi} from './utils';
  import type {HTMLAttributes} from 'svelte/elements';
  import {cn} from '$lib/utils';
  import * as Popover from '$lib/components/ui/popover';
  import {Button} from '$lib/components/ui/button';
  import HistoryView from '$lib/history/HistoryView.svelte';
  import {Icon} from '$lib/components/ui/icon';
  import {usePartsOfSpeech} from '$project/data';
  import {assembleEntryAtCommit} from './assemble-entry';

  type Props = HTMLAttributes<HTMLDivElement> & {
    activity: IProjectActivity;
    showHistoryButton?: boolean;
  }

  const {
    activity,
    showHistoryButton = false,
    class: className,
    ...restProps
  }: Props = $props();

  const historyService = useHistoryService();
  const partsOfSpeech = usePartsOfSpeech();
  let openHistoryId = $state<string>()

  const changes = $derived(!historyService.loaded ? undefined : activity.changes.map(change => {
    return new ChangeWithLazyContext(change, activity, () => historyService.loadChangeContext(activity.commitId, change.index));
  }));

  // A commit that only builds one entry (creation + its own senses/fields) collapses to the finished entry.
  const collapseToEntry = $derived(
    !!changes && changes.length > 1
    && activity.changeTypes.includes('CreateEntryChange')
    && activity.changeInfo.length > 0
    && activity.changeInfo.every(ci => !!ci.rootEntryId && ci.rootEntryId === activity.changeInfo[0].rootEntryId),
  );

  // Assemble the entry as it stood AT this commit from the commit's own change snapshots, rather than showing
  // its current (possibly since-edited) state. Every part of a create-entry commit is created in it, so the
  // per-change snapshots piece together the historical entry. See assembleEntryAtCommit.
  const collapsedEntry = $derived.by((): Promise<IEntry | undefined> => {
    if (!collapseToEntry || !changes) return Promise.resolve(undefined);
    const entryId = activity.changeInfo[0]?.rootEntryId;
    if (!entryId) return Promise.resolve(undefined);
    return Promise.all(changes.map(c => c.lazyContext))
      .then(contexts => assembleEntryAtCommit(entryId, contexts, partsOfSpeech.current));
  });
</script>

<div {...restProps} class={cn(className, 'grid gap-2 grid-rows-[auto_1fr] h-full min-w-0 min-h-0')}>
  {#if activity}
    <div class="text-sm flex flex-wrap justify-between gap-2">
      <span>
        <span>
          {$t`Author:`}
          {#if activity.metadata.authorName}
            <span class="font-semibold">{activity.metadata.authorName}</span>
          {:else}
            <span class="opacity-75 italic">{$t`Unknown`}</span>
          {/if}
        </span>
        {#if activity.changes.length > 1}
          <span>{$t`– (${activity.changes.length} changes)`}</span>
        {/if}
      </span>
      <span>
        <FormatDate date={activity.timestamp}
                    options={{ dateStyle: 'medium', timeStyle: 'medium' }} />
      </span>
      <span class="whitespace-nowrap">
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
      </span>
      {#if activity.changeTypeLabels.length}
        <div class="w-full text-xs text-muted-foreground truncate" title={activity.changeTypeLabels.join(', ')}>{activity.changeTypeLabels.join(', ')}</div>
      {/if}
    </div>
    
    {#if openHistoryId}
    <!-- this is a dialog so it doesn't matter where it is in the DOM -->
        <HistoryView bind:open={() => !!openHistoryId, (open) => (open ? undefined : openHistoryId = undefined)} id={openHistoryId} selectedCommitId={activity.commitId}/>
    {/if}
    {#if changes}
      {#if collapseToEntry}
        {#await collapsedEntry}
          <div class="h-[700px] border rounded"></div>
        {:then entry}
          {#if entry}
            <div class="overflow-auto border rounded p-3 min-w-0 min-h-0">
              <PreviewViewScope>
                <EntryEditor {entry} readonly modalMode canAddSense={false} canAddExample={false} />
              </PreviewViewScope>
            </div>
          {:else}
            {@render changeList(changes)}
          {/if}
        {/await}
      {:else}
        {@render changeList(changes)}
      {/if}
    {/if}
  {/if}
</div>

{#snippet changeList(items: ChangeWithLazyContext[])}
  <div class="change-list flex flex-col gap-4 overflow-auto border rounded">
    {#key items}
      <VList
        class="space-y-2"
        data={items}
        itemSize={700}
        bufferSize={700}
        getKey={(item) => `${item.change.commitId}:${item.change.index}`}>
        {#snippet children(changeWithContext)}
          {@const {change, lazyContext} = changeWithContext}
          {#await lazyContext}
            <!-- determines how many rows are initially visible,
             which in turn determines how many changes will be loaded.
             Too big is not much of a problem, it will just stagger subsequent loads -->
            <div class="h-[700px]"></div>
          {:then context}
            <div class="change">
              <div class="px-4 pt-2 flex font-semibold items-center">
                <span class="grow">{context.changeName}</span>

                {#if showHistoryButton}
                  <Button icon="i-mdi-history" onclick={() => openHistoryId = context.snapshot?.id ?? context.previousSnapshot?.id}>
                    {$t`History`}
                  </Button>
                {/if}
              </div>
              <Tabs.Root value="preview" class="px-2 mt-2 grow">
                <Tabs.List class="w-full">
                  <Tabs.Trigger class="flex-1" value="preview">
                    {$t`Preview`}
                  </Tabs.Trigger>
                  <Tabs.Trigger class="flex-1" value="change">{$t`Details`}</Tabs.Trigger>
                </Tabs.List>
                <div class="pt-1 pb-4 px-2">
                  <Tabs.Content value="preview">
                    <ActivityItemChangePreview {context} />
                  </Tabs.Content>
                  <Tabs.Content value="change">
                    <div class="whitespace-pre-wrap break-words font-mono text-sm">
                      {formatJsonForUi(change)}
                    </div>
                  </Tabs.Content>
                </div>
              </Tabs.Root>
            </div>
          {/await}
        {/snippet}
      </VList>
    {/key}
  </div>
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
