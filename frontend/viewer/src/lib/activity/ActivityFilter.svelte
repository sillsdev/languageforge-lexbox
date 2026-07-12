<script lang="ts">
  import {useHistoryService} from '$lib/services/history-service';
  import {t} from 'svelte-i18n-lingui';
  import {resource} from 'runed';
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import {ActivitySort} from '$lib/dotnet-types';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {Button, buttonVariants} from '$lib/components/ui/button';
  import {badgeVariants} from '$lib/components/ui/badge';
  import {cn} from '$lib/utils';
  import {Icon} from '$lib/components/ui/icon';
  import type {IconClass} from '$lib/icon-class';
  import {
    compareActivityAuthors,
    createDefaultActivityFilters,
    type ActivityFilters,
  } from './utils';
  import AuthorFilter from './AuthorFilter.svelte';
  import ChangeTypeFilter from './ChangeTypeFilter.svelte';
  import type {Snippet} from 'svelte';

  type Props = {
    filters?: ActivityFilters;
    /** Optional right-aligned content rendered on the same row as the sort menu (e.g. the list-mode icon). */
    trailing?: Snippet;
  };

  let {filters = $bindable(createDefaultActivityFilters()), trailing}: Props = $props();

  const historyService = useHistoryService();

  const authors = resource(
    () => historyService.loaded,
    async (loaded) => {
      if (!loaded) return [];
      const data = await historyService.listActivityAuthors();
      if (Array.isArray(data)) {
        return data.sort(compareActivityAuthors);
      }
      return [];
    },
    {initialValue: []},
  );

  const changeTypes = resource(
    () => historyService.loaded,
    async (loaded) => {
      if (!loaded) return [];
      const data = await historyService.listActivityChangeTypes();
      return Array.isArray(data) ? data : [];
    },
    {initialValue: []},
  );

  const sortLabels = $derived<Record<ActivitySort, string>>({
    [ActivitySort.NewestFirst]: $t`Newest first`,
    [ActivitySort.OldestFirst]: $t`Oldest first`,
    [ActivitySort.SyncedNewestFirst]: $t`Synced newest`,
    [ActivitySort.SyncedOldestFirst]: $t`Synced oldest`,
  });

  const sortIcons: Record<ActivitySort, IconClass> = {
    [ActivitySort.NewestFirst]: 'i-mdi-sort-clock-descending',
    [ActivitySort.OldestFirst]: 'i-mdi-sort-clock-ascending',
    [ActivitySort.SyncedNewestFirst]: 'i-mdi-cloud-arrow-down',
    [ActivitySort.SyncedOldestFirst]: 'i-mdi-cloud-arrow-up',
  };

</script>

<div class="flex flex-col gap-2 mb-1">
  <div class="flex flex-wrap gap-2">
    <SidebarTrigger icon="i-mdi-menu" class="aspect-square p-0 shrink-0" />

    <AuthorFilter
      authors={authors.current}
      selected={filters.authorFilterKeys}
      onSelectionChange={(keys) => filters.authorFilterKeys = keys} />

    <ChangeTypeFilter
      changeTypes={changeTypes.current}
      selected={filters.changeTypeFilterKeys}
      onSelectionChange={(keys) => filters.changeTypeFilterKeys = keys} />
  </div>

  <!-- Sort menu on the left, optional trailing element (list-mode icon) on the right — mirrors the entry
       browser layout (SortMenu ↔ EntryListViewOptions) so the two views feel consistent. -->
  <div class="flex items-center justify-between gap-2">
    <ResponsiveMenu.Root>
      <ResponsiveMenu.Trigger class={cn(buttonVariants({variant: 'secondary', size: 'xs'}), badgeVariants({variant: 'secondary'}), 'border-none h-7')}>
        {#snippet child({props})}
          <Button {...props} icon={sortIcons[filters.sort]} iconProps={{class: 'size-4'}}>
            {sortLabels[filters.sort]}
          </Button>
        {/snippet}
      </ResponsiveMenu.Trigger>
      <ResponsiveMenu.Content align="start">
        {#each Object.values(ActivitySort) as sortOption (sortOption)}
          <ResponsiveMenu.Item
            onSelect={() => filters.sort = sortOption}
            class={cn(filters.sort === sortOption && 'bg-muted')}>
            <Icon icon={sortIcons[sortOption]} />
            {sortLabels[sortOption]}
          </ResponsiveMenu.Item>
        {/each}
      </ResponsiveMenu.Content>
    </ResponsiveMenu.Root>
    {#if trailing}{@render trailing()}{/if}
  </div>
</div>
