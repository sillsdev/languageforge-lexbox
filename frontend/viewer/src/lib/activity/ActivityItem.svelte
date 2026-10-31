<script lang="ts" module>
  import type {IActivityChangeInfo, IChangeContext, IChangeEntity, IProjectActivity} from '$lib/dotnet-types';

  export class ChangeWithLazyContext {

    #context?: Promise<IChangeContext>;
    // On the item, not the row component, so the open tab survives virtual scrolling
    tab = $state('preview');

    constructor(
      public readonly change: IChangeEntity,
      public readonly activity: IProjectActivity,
      /** Current names for what the change is about, resolved with the activity page */
      public readonly info: IActivityChangeInfo,
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
  import {formatJsonForUi} from './utils';
  import AuthorLabel from './AuthorLabel.svelte';
  import type {HTMLAttributes} from 'svelte/elements';
  import {cn} from '$lib/utils';
  import * as Popover from '$lib/components/ui/popover';
  import {Button} from '$lib/components/ui/button';
  import HistoryView from '$lib/history/HistoryView.svelte';
  import {Icon} from '$lib/components/ui/icon';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {sinceThen} from './since-then';

  const viewService = useViewService();

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
  let openHistoryId = $state<string>()

  const changes = $derived(!historyService.loaded ? undefined : activity.changes.map(({entity, info}) => {
    return new ChangeWithLazyContext(entity, activity, info, () => historyService.loadChangeContext(activity.commitId, entity.index));
  }));
</script>

<div {...restProps} class={cn(className, 'grid gap-2 grid-rows-[auto_1fr] h-full')}>
  {#if activity}
    <div class="text-sm flex flex-wrap justify-between gap-2">
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
    </div>
    
    {#if openHistoryId}
    <!-- this is a dialog so it doesn't matter where it is in the DOM -->
        <HistoryView bind:open={() => !!openHistoryId, (open) => (open ? undefined : openHistoryId = undefined)} id={openHistoryId} selectedCommitId={activity.commitId}/>
    {/if}
    {#if changes}
      <div
        class="change-list flex flex-col gap-4 overflow-auto border rounded">
        {#key changes}
          <VList
            class="space-y-2"
            data={changes}
            bufferSize={1400}
            getKey={(item) => `${item.change.commitId}:${item.change.index}`}>
            {#snippet children(changeWithContext)}
              {@const {change, info, lazyContext} = changeWithContext}
              {#await lazyContext}
                <!-- determines how many rows are initially visible,
                 which in turn determines how many changes will be loaded.
                 Too big is not much of a problem, it will just stagger subsequent loads -->
                <div class="h-[700px]"></div>
              {:then context}
                {@const since = sinceThen(context.entityType, context.snapshot, context.currentSnapshot, viewService.currentView)}
                {@const noLaterChanges = since?.status === 'unchanged'}
                <div class="change">
                  <div class="px-4 pt-2 flex font-semibold items-center">
                    <span class="grow">{context.changeName}</span>

                    {#if showHistoryButton}
                      <Button icon="i-mdi-history" onclick={() => openHistoryId = context.snapshot?.id}>
                        {$t`History`}
                      </Button>
                    {/if}
                  </div>
                  <!-- Blocks opening No later changes here: bits-ui's disabled would drop it from the tab order, so screen readers never hear it -->
                  <Tabs.Root bind:value={() => changeWithContext.tab, (tab) => { if (tab !== 'since' || !noLaterChanges) changeWithContext.tab = tab; }}
                             class="px-2 mt-2 grow">
                    <Tabs.List class="w-full h-auto">
                      <Tabs.Trigger class="flex-1 h-auto min-h-8 whitespace-normal" value="preview">
                        {$t`This change`}
                      </Tabs.Trigger>
                      {#if since}
                        <Tabs.Trigger value="since" aria-disabled={noLaterChanges || undefined}
                                      class="flex-1 h-auto min-h-8 whitespace-normal aria-disabled:text-muted-foreground aria-disabled:cursor-default">
                          {#if since.status === 'deleted'}
                            {$t`Deleted later`}
                          {:else if noLaterChanges}
                            {$t`No later changes`}
                          {:else}
                            {$t`Later changes`}
                          {/if}
                        </Tabs.Trigger>
                      {/if}
                      <Tabs.Trigger class="flex-1 h-auto min-h-8 whitespace-normal" value="change">{$t`Details`}</Tabs.Trigger>
                    </Tabs.List>
                    <div class="pt-1 pb-4 px-2">
                      <Tabs.Content value="preview">
                        <ActivityItemChangePreview {context} {change} {info} />
                      </Tabs.Content>
                      {#if since && !noLaterChanges}
                        <Tabs.Content value="since">
                          <ActivityItemChangePreview {context} {change} {info} view="since" />
                        </Tabs.Content>
                      {/if}
                      <Tabs.Content value="change">
                        <div class="whitespace-pre-wrap font-mono text-sm">
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
    {/if}
  {/if}
</div>

<style lang="postcss">
  @reference "#app.css";
  :global(.change-list .sentinel) {
    @apply -mt-4; /* make gap-4 not apply to the infinite-scroll end detector */
  }
  :global(.change-list :not(:last-child) > .change) {
    @apply border-b-2 pb-2 mb-1;
  }
</style>
