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
  import {useProjectStorage} from '$lib/storage/project-storage.svelte';
  import ActivityListViewOptions, {type ActivityListViewMode} from './ActivityListViewOptions.svelte';
  import ChangeSummary from './ChangeSummary.svelte';
  import {summarizeActivity, groupBySubject, factCategory, type FactCategory} from './change-summary';
  import type {IconClass} from '$lib/icon-class';
  import type {ChangeFact, ChangeFactWithSubject} from './change-summary';
  import {
    createDefaultActivityFilters,
    emptyActivityLoad,
    hasActiveServerSideFilters,
    MIN_VISIBLE_FILTERED,
    serverQueryKey,
    toServerQuery,
    type ActivityFilters,
    type ActivityLoad,
  } from './utils';
  import AuthorLabel from './AuthorLabel.svelte';

  const historyService = useHistoryService();

  const activityListViewMode = useProjectStorage().activityListViewMode;
  const activityMode = $derived((activityListViewMode.current as ActivityListViewMode | undefined) ?? 'simple');

  // Change-kind gutter glyph for Detailed mode — a coloured icon per fact for fast visual grepping down a
  // commit (added / removed / changed / reordered). Kept out of Simple mode, which is a plain-sentence skim.
  const FACT_GLYPH: Record<FactCategory, {icon: IconClass; class: string} | undefined> = {
    added: {icon: 'i-mdi-plus', class: 'text-emerald-600 dark:text-emerald-400'},
    removed: {icon: 'i-mdi-minus', class: 'text-destructive'},
    changed: {icon: 'i-mdi-pencil-outline', class: 'text-muted-foreground'},
    reordered: {icon: 'i-mdi-swap-vertical', class: 'text-muted-foreground'},
    other: undefined,
  };
  function glyphFor(fact: ChangeFact) {
    return FACT_GLYPH[factCategory(fact)];
  }

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
    return activity.current?.items ?? [];
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
    if (!hasActiveServerSideFilters(filters) || activity.loading || visibleActivity === null || !hasMorePages) return;
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

<!-- One Detailed-mode change line: a change-kind glyph + the summary. `grouped` hides the subject token
     (the subject is already the group header) AND reserves an icon gutter so wrapped lines hang-indent
     under the text — a bulleted list of the group's changes. Ungrouped rows are one-offs, so the icon
     just sits inline before the text and wrap flows naturally to the left edge. -->
{#snippet factLine(entry: ChangeFactWithSubject, grouped: boolean)}
  {@const glyph = glyphFor(entry.fact)}
  {#if grouped}
    <div class="flex items-center gap-1.5">
      <span class="w-3.5 shrink-0 flex justify-center">
        {#if glyph}<Icon icon={glyph.icon} class="size-3.5 {glyph.class}" />{/if}
      </span>
      <span class="min-w-0"><ChangeSummary fact={entry.fact} subject={entry.subject} target={entry.target} hideSubject /></span>
    </div>
  {:else}
    <div class="min-w-0">
      {#if glyph}<Icon icon={glyph.icon} class="size-3.5 me-1 align-middle {glyph.class}" />{/if}<ChangeSummary fact={entry.fact} subject={entry.subject} target={entry.target} />
    </div>
  {/if}
{/snippet}

<div class="h-full m-4 grid gap-x-6 gap-y-1 overflow-hidden"
     style="grid-template-rows: auto 1fr; grid-template-columns: minmax(8rem,25%) minmax(0,2fr)">

  <div>
    <ActivityFilter bind:filters>
      {#snippet trailing()}
        <ActivityListViewOptions bind:mode={() => activityMode, (v) => void activityListViewMode.set(v)} />
      {/snippet}
    </ActivityFilter>
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
          {@const summary = summarizeActivity(row.changes, row.changeInfo, row.changeTypes, activityMode === 'detailed')}
          <ListItem
            onclick={() => selectedRow = row}
            selected={selectedRow?.commitId === row.commitId}
            class="mb-2">
            {#if summary.entries.length === 0}
              <span>{row.changeName}</span>
            {:else if activityMode === 'detailed'}
              <!-- Detailed groups facts by subject: the headword appears once as a bold header, its changes
                   listed beneath along an indent rail. Base text muted so verbs recede; subject header and
                   data chips render foreground. This is what makes Detailed a change-inspector rather than
                   a longer version of Simple. -->
              {@const groups = groupBySubject(summary.entries)}
              <div class="space-y-1 text-muted-foreground">
                {#each groups as group, gi (gi)}
                  {#if group.subject && group.facts.length > 1}
                    <div>
                      <div class="font-semibold text-foreground">{group.subject}</div>
                      <div class="ms-1 space-y-0.5 border-s border-border ps-2">
                        {#each group.facts as entry, i (i)}
                          {@render factLine(entry, true)}
                        {/each}
                      </div>
                    </div>
                  {:else}
                    {#each group.facts as entry, i (i)}
                      {@render factLine(entry, false)}
                    {/each}
                  {/if}
                {/each}
                {#if summary.remaining > 0}
                  <div class="ps-5">{$t`(+${summary.remaining} more)`}</div>
                {/if}
              </div>
            {:else}
              <div class="flex items-baseline gap-1 min-w-0 text-muted-foreground">
                <span class="min-w-0 truncate">
                  <ChangeSummary fact={summary.entries[0].fact} subject={summary.entries[0].subject} target={summary.entries[0].target} />
                </span>
                {#if summary.remaining > 0}
                  <span class="shrink-0">{$t`(+${summary.remaining} more)`}</span>
                {/if}
              </div>
            {/if}
            <div class="mt-2 text-sm text-muted-foreground flex flex-wrap gap-x-2 justify-between items-center">
              <AuthorLabel class="min-w-0" authorId={row.metadata.authorId} authorName={row.metadata.authorName} />
              <span class="flex items-center gap-1">
                {#if !row.metadata.extraMetadata['SyncDate']}
                  <Icon
                    icon="i-mdi-cloud-off-outline"
                    class="size-4 shrink-0 text-muted-foreground"
                    title={$t`These changes have not been uploaded yet. Ensure you're online and logged in to share your changes.`} />
                {/if}
                <FormatRelativeDate date={row.timestamp}
                        actualDateOptions={{ dateStyle: 'medium', timeStyle: 'short' }}/>
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
    <ActivityItem class="sub-grid row-span-2 col-start-2" activity={selectedRow} showHistoryButton />
  {/if}
</div>
