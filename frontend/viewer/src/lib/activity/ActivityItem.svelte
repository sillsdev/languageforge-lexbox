<script lang="ts" module>
  import type {IChangeContext, IChangeEntity, IProjectActivity} from '$lib/dotnet-types';

  export type ChangeWithContext = {
    change: IChangeEntity;
    context: IChangeContext;
    activity: IProjectActivity;
  };
</script>

<script lang="ts">
  import {FormatDate, FormatRelativeDate} from '$lib/components/ui/format';
  import * as Tabs from '$lib/components/ui/tabs';
  import {useHistoryService} from '$lib/services/history-service';
  import {resource} from 'runed';
  import {T, t} from 'svelte-i18n-lingui';
  import {VList} from 'virtua/svelte';
  import ActivityItemChangePreview from './ActivityItemChangePreview.svelte';
  import {formatJsonForUi} from './utils';

  const { activity }: { activity: Activity } = $props();

  const historyService = useHistoryService();

  const changes = $derived(!historyService.loaded ? undefined : resource(() => activity, async (activity) => {
    const contextPromises = activity.changes.map(async change => {
      const context = await historyService.loadChangeContext(activity.commitId, change.index);
      return { change, context, activity };
    });
    return await Promise.all(contextPromises);
  }));
</script>

<div class="grid grid-cols-subgrid grid-rows-subgrid col-start-2 row-span-2">
  {#if activity}
    <div class="col-start-2 row-start-1 text-sm flex flex-wrap justify-between gap-2">
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
      <span class="basis-48 whitespace-nowrap shrink">
        {#if activity.metadata.extraMetadata['SyncDate']}
          <span class="float-right" title={$t`The time when you uploaded or downloaded these changes`}>
            <T msg="Synced #">
              <FormatRelativeDate date={new Date(activity.metadata.extraMetadata['SyncDate'])}
                                  showActualDate={true}
                                  actualDateOptions={{ dateStyle: 'medium', timeStyle: 'short' }}/>
            </T>
          </span>
        {/if}
      </span>
    </div>
    {#if changes?.current}
      <div
        class="change-list col-start-2 row-start-2 flex flex-col gap-4 overflow-auto border rounded">
        <VList class="space-y-2" data={changes.current}>
          {#snippet children(changeWithContext)}
            {@const {change, context} = changeWithContext}
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
                    <ActivityItemChangePreview change={changeWithContext} />
                  </Tabs.Content>
                  <Tabs.Content value="change">
                    <div class="whitespace-pre-wrap font-mono text-sm">
                      {formatJsonForUi(change)}
                    </div>
                  </Tabs.Content>
                </div>
              </Tabs.Root>
            </div>
          {/snippet}
        </VList>
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
