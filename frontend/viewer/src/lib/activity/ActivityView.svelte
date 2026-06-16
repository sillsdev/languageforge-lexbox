<script lang="ts">
  import {useHistoryService} from '$lib/services/history-service';
  import {t} from 'svelte-i18n-lingui';
  import {Debounced, resource} from 'runed';
  import ListItem from '$lib/components/ListItem.svelte';
  import {VList} from 'virtua/svelte';
  import {FormatRelativeDate} from '$lib/components/ui/format';
  import ActivityItem from './ActivityItem.svelte';
  import ActivityFilter from './ActivityFilter.svelte';
  import Loading from '$lib/components/Loading.svelte';
  import {Icon} from '$lib/components/ui/icon';
  import {AppNotification} from '$lib/notifications/notifications';
  import type {IProjectActivity} from '$lib/dotnet-types';
  import {
    ALL_CHANGE_TYPES,
    MIN_VISIBLE_FILTERED,
    createDefaultActivityFilters,
    emptyActivityLoad,
    filterActivityByChangeType,
    serverQueryKey,
    toServerQuery,
    type ActivityFilters,
    type ActivityLoad,
  } from './utils';

  const historyService = useHistoryService();

  const THRESHOLD = 20;
  const BATCH_SIZE = THRESHOLD * 2;

  let filters = $state<ActivityFilters>(createDefaultActivityFilters());
  let pageCount = $state(1);

  const queryKey = $derived.by(() => serverQueryKey(filters));
  const serverQuery = $derived.by(() => toServerQuery(filters));

  const activity = resource(
    [() => queryKey, () => pageCount, () => serverQuery, () => historyService.loaded],
    async ([key, pages, query, loaded], _previous, current): Promise<ActivityLoad> => {
      if (!loaded) return emptyActivityLoad;

      if (pages > 1 && current.data?.queryKey !== key) {
        return current.data ?? emptyActivityLoad;
      }

      const skip = (pages - 1) * BATCH_SIZE;
      const data = await historyService.activity(skip, BATCH_SIZE, query);
      if (key !== queryKey || pages !== pageCount) {
        return current.data ?? emptyActivityLoad;
      }
      if (!Array.isArray(data)) {
        console.error('Invalid history data', data);
        return {items: [], hasMorePages: false, queryKey: key};
      }
      const prev =
        pages > 1 && current.data?.queryKey === key
          ? current.data.items
          : [];
      return {
        items: [...prev, ...data],
        hasMorePages: data.length >= BATCH_SIZE,
        queryKey: key,
      };
    },
    {initialValue: emptyActivityLoad},
  );

  const loading = new Debounced(() => activity.loading, 0);
  const loadedQueryKey = $derived(activity.current?.queryKey ?? '');
  const awaitingFreshData = $derived(loadedQueryKey !== queryKey);
  const hasMorePages = $derived(
    awaitingFreshData ? true : (activity.current?.hasMorePages ?? true),
  );

  $effect(() => {
    if (activity.error) {
      AppNotification.error($t`Failed to load activity`, activity.error.message);
    }
  });

  const visibleActivity = $derived.by(() => {
    if (awaitingFreshData) {
      return null;
    }
    return filterActivityByChangeType(activity.current?.items ?? [], filters.changeTypeFilter);
  });

  let selectedRow = $state<IProjectActivity>();
  let vlist = $state<VList<IProjectActivity>>();

  $effect(() => {
    const visible = visibleActivity;
    if (!visible?.length) {
      selectedRow = undefined;
      return;
    }
    if (!selectedRow || !visible.some(a => a.commitId === selectedRow?.commitId)) {
      selectedRow = visible[0];
    }
  });

  $effect(() => {
    if (filters.changeTypeFilter === ALL_CHANGE_TYPES || activity.loading || visibleActivity === null || !hasMorePages) return;
    const filtered = visibleActivity.length;
    const loaded = activity.current?.items.length ?? 0;
    if (filtered < MIN_VISIBLE_FILTERED && loaded >= (pageCount - 1) * BATCH_SIZE && loaded > 0) {
      pageCount += 1;
    }
  });

  let prevQueryKey = '';
  $effect.pre(() => {
    const key = queryKey;
    if (prevQueryKey && prevQueryKey !== key) {
      pageCount = 1;
    }
    prevQueryKey = key;
  });

  function onListScroll() {
    if (!vlist || visibleActivity === null) return;
    const scrollOffset = vlist.getScrollOffset();
    const endIndex = vlist.findItemIndex(scrollOffset + vlist.getViewportSize());
    if (endIndex + THRESHOLD >= visibleActivity.length && hasMorePages && !activity.loading) {
      pageCount += 1;
    }
  }
</script>

<div class="h-full m-4 grid gap-x-6 gap-y-1 overflow-hidden"
     style="grid-template-rows: auto minmax(0,100%); grid-template-columns: 1fr 2fr">

  <div class="col-span-2 flex flex-wrap items-center gap-2">
    <ActivityFilter bind:filters />
    {#if loading.current}
      <Loading class="size-5 text-muted-foreground" aria-label={$t`Loading activity`} />
    {/if}
  </div>

  <div class="gap-4 overflow-hidden row-start-2 relative">
    {#if activity.error && awaitingFreshData}
      <div class="flex h-full items-center justify-center gap-2 text-muted-foreground">
        <Icon icon="i-mdi-alert-circle-outline" />
        <p>{$t`Failed to load activity`}</p>
      </div>
    {:else if awaitingFreshData}
      <div class="flex h-full items-center justify-center">
        <Loading class="size-8 text-muted-foreground" aria-label={$t`Loading activity`} />
      </div>
    {:else if visibleActivity && visibleActivity.length}
      <VList bind:this={vlist} data={visibleActivity}
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
                {row.metadata.authorName ?? $t`Unknown`}
              </span>
            </div>
          </ListItem>
        {/snippet}
      </VList>
    {:else if !loading.current}
      <div class="p-4 text-center opacity-75">{$t`No activity matches these filters`}</div>
    {/if}
  </div>

  {#if selectedRow}
    <ActivityItem class="sub-grid row-span-2 col-start-2" activity={selectedRow} />
  {/if}
</div>
