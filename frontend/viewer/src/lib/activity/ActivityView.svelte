<script lang="ts">
  import {useHistoryService} from '$lib/services/history-service';
  import {t} from 'svelte-i18n-lingui';
  import {useProjectContext} from '$project/project-context.svelte';
  import {resource} from 'runed';
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import ListItem from '$lib/components/ListItem.svelte';
  import {VList} from 'virtua/svelte';
  import {FormatRelativeDate} from '$lib/components/ui/format';
  import ActivityItem from './ActivityItem.svelte';
  import type {IProjectActivity} from '$lib/dotnet-types';

  const historyService = useHistoryService();
  const projectContext = useProjectContext();

  const THRESHOLD = 20;
  const BATCH_SIZE = THRESHOLD * 4;
  let loadCount = $state(BATCH_SIZE);

  const activity = resource(
    [() => projectContext.projectCode, () => loadCount],
    async ([projectCode, loadCount], [_, prevLoadCount]): Promise<IProjectActivity[]> => {
      if (!projectCode) return [];

      prevLoadCount ??= 0;
      const skip = prevLoadCount;
      const take = loadCount - prevLoadCount;
      const data = (await historyService.activity(projectCode, skip, take));
      console.debug('Activity data', skip, take, data);
      if (!Array.isArray(data)) {
        console.error('Invalid history data', data);
        return [];
      }

      const activityData = [...activity.current, ...data];
      selectedRow ??= activityData[0];
      return activityData;
    },
    {
      initialValue: [],
    },
  );

  let selectedRow = $state<IProjectActivity>();
  let vlist = $state<VList<IProjectActivity>>();
  function onListScroll() {
    if (!vlist) return;
    const scrollOffset = vlist.getScrollOffset();
    const endIndex = vlist.findItemIndex(scrollOffset + vlist.getViewportSize());
    if (endIndex + THRESHOLD >= loadCount) {
      loadCount += BATCH_SIZE;
    }
  }
</script>

{#if activity.current.length}
  <div class="h-full m-4 grid gap-x-6 gap-y-1 overflow-hidden"
       style="grid-template-rows: auto minmax(0,100%); minmax(min-content, 1fr) minmax(min-content, 2fr); grid-template-columns: 1fr 2fr">

    <SidebarTrigger icon="i-mdi-menu" class="aspect-square p-0" />
    <div class="gap-4 overflow-hidden row-start-2">

            <VList bind:this={vlist} data={activity.current ?? []}
             class="h-full p-0.5 md:pr-3 after:h-12 after:block"
             onscroll={onListScroll}
              getKey={row => row.commitId} bufferSize={400}>
        {#snippet children(row)}
          <ListItem
            onclick={() => selectedRow = row}
            selected={selectedRow?.commitId === row.commitId}
            class="mb-2">
            <span>{row.changeName}</span>
            <div class="text-sm text-muted-foreground flex flex-wrap gap-x-2 justify-between">
              <span>
                <FormatRelativeDate date={row.timestamp}
                        actualDateOptions={{ dateStyle: 'medium', timeStyle: 'short' }}/>
              </span>
              <span>
                {row.metadata.authorName}
              </span>
            </div>
          </ListItem>
        {/snippet}
      </VList>
      {#if activity.current.length === 0}
        <div class="p-4 text-center opacity-75">{$t`No activity found`}</div>
      {/if}
    </div>
    {#if selectedRow}
      <ActivityItem class="sub-grid row-span-2 col-start-2" activity={selectedRow} />
    {/if}
  </div>
{/if}
