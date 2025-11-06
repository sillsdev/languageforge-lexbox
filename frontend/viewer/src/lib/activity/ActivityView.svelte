<script lang="ts">
  import { useHistoryService } from '$lib/services/history-service';
  import { t } from 'svelte-i18n-lingui';
  import { useProjectContext } from '$lib/project-context.svelte';
  import { resource } from 'runed';
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import ListItem from '$lib/components/ListItem.svelte';
  import {VList} from 'virtua/svelte';
  import {FormatRelativeDate} from '$lib/components/ui/format';
  import ActivityItem, {type Activity} from './ActivityItem.svelte';

  const historyService = useHistoryService();
  const projectContext = useProjectContext();

  const activity = resource(() => projectContext.projectCode, async (projectCode) => {
    if (!projectCode) return [] as Activity[];
    const data = (await historyService.activity(projectContext.projectCode)) as Activity[];
    console.debug('Activity data', data);
    if (!Array.isArray(data)) {
      console.error('Invalid history data', data);
      return [] as Activity[];
    }
    data.reverse();
    for (let i = 0; i < data.length; i++) {
      let row = data[i];
      row.previousTimestamp = data[i + 1]?.timestamp;
    }
    // Reverse the history so that the most recent changes are at the top
    return data.toReversed();
  },
  {
    initialValue: [] as Activity[],
  });

  let selectedRow = $derived(activity.current[0]);
</script>

{#if !activity.loading}
  <div class="h-full m-4 grid gap-x-6 gap-y-1 overflow-hidden"
       style="grid-template-rows: auto minmax(0,100%); minmax(min-content, 1fr) minmax(min-content, 2fr); grid-template-columns: 1fr 2fr">

    <SidebarTrigger icon="i-mdi-menu" iconProps={{ class: 'size-5' }} class="aspect-square p-0" size="xs"/>
    <div class="gap-4 overflow-hidden row-start-2">

      <VList data={activity.current ?? []}
             class="h-full p-0.5 md:pr-3 after:h-12 after:block"
             getKey={row => row.commitId} overscan={10}>
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

    <ActivityItem activity={selectedRow} />

  </div>
{/if}
