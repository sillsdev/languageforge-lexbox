<script lang="ts" module>
  import type {IChangeContext, IChangeEntity, IProjectActivity} from '$lib/dotnet-types';

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
  import {formatJsonForUi} from './utils';
  import type {HTMLAttributes} from 'svelte/elements';
  import {cn} from '$lib/utils';
  import * as Popover from '$lib/components/ui/popover';

  type Props = HTMLAttributes<HTMLDivElement> & {
    activity: IProjectActivity;
  }

  const {
    activity,
    class: className,
    ...restProps
  }: Props = $props();

  const historyService = useHistoryService();

  const changes = $derived(!historyService.loaded ? undefined : activity.changes.map(change => {
    return new ChangeWithLazyContext(change, activity, () => historyService.loadChangeContext(activity.commitId, change.index));
  }));
</script>

<div {...restProps} class={cn(className, 'grid gap-2 grid-rows-[auto_1fr] h-full')}>
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
                <T msg="Synced: #">
                  <FormatRelativeDate
                    class="font-semibold"
                    date={new Date(activity.metadata.extraMetadata['SyncDate'])} />
                </T>
              </Popover.InfoTrigger>
              <Popover.Content class="w-auto p-2 text-sm text-center max-w-48">
                {$t`The time when you uploaded or downloaded these changes`}
              </Popover.Content>
            </Popover.Root>
          </span>
        {:else}
          <span class="text-red-500 font-semibold" title={$t`These changes have not been uploaded yet. Ensure you're online and logged in to share your changes.`}>
            {$t`Not synced`}
          </span>
        {/if}
      </span>
    </div>
    {#if changes}
      <div
        class="change-list flex flex-col gap-4 overflow-auto border rounded">
        {#key changes}
          <VList
            class="space-y-2"
            data={changes}
            overscan={2}
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
                  <div class="px-4 pt-2 flex font-semibold">
                    <span>{context.changeName}</span>
                  </div>
                  <Tabs.Root value="preview" class="px-2 mt-2">
                    <Tabs.List class="w-full">
                      <Tabs.Trigger class="flex-1" value="preview">
                        {$t`Preview`}
                      </Tabs.Trigger>
                      <Tabs.Trigger class="flex-1" value="change">{$t`Details`}</Tabs.Trigger>
                    </Tabs.List>
                    <div class="pt-1 pb-4 px-2">
                      <Tabs.Content value="preview">
                        <ActivityItemChangePreview {activity} {context} />
                      </Tabs.Content>
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
  :global(.change-list .sentinel) {
    @apply -mt-4; /* make gap-4 not apply to the infinite-scroll end detector */
  }
  :global(.change-list :not(:last-child) > .change) {
    @apply border-b-2 pb-2 mb-1;
  }
</style>
