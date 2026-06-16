<script lang="ts">
  import {useHistoryService} from '$lib/services/history-service';
  import {t} from 'svelte-i18n-lingui';
  import {resource} from 'runed';
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import {ActivitySort} from '$lib/dotnet-types';
  import * as Select from '$lib/components/ui/select';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {Switch} from '$lib/components/ui/switch';
  import {Button, buttonVariants} from '$lib/components/ui/button';
  import {cn} from '$lib/utils';
  import {
    ALL_AUTHORS,
    ALL_CHANGE_TYPES,
    UNKNOWN_AUTHOR,
    authorFilterKey,
    createDefaultActivityFilters,
    type ActivityFilters,
  } from './utils';

  type Props = {
    filters?: ActivityFilters;
  };

  let {filters = $bindable(createDefaultActivityFilters())}: Props = $props();

  const historyService = useHistoryService();

  const authors = resource(
    () => historyService.loaded,
    async (loaded) => {
      if (!loaded) return [];
      const data = await historyService.listActivityAuthors();
      return Array.isArray(data) ? data : [];
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
</script>

<div class="flex flex-wrap items-center gap-2">
  <SidebarTrigger icon="i-mdi-menu" class="aspect-square p-0 shrink-0" />
  <Select.Root type="single" value={filters.authorFilter} onValueChange={v => filters.authorFilter = v}>
    <Select.Trigger class="w-44">
      {#if filters.authorFilter === ALL_AUTHORS}
        {$t`All authors`}
      {:else if filters.authorFilter === UNKNOWN_AUTHOR}
        {$t`Unknown`}
      {:else}
        {@const author = authors.current.find(a => authorFilterKey(a) === filters.authorFilter)}
        {author?.authorName ?? filters.authorFilter}
      {/if}
    </Select.Trigger>
    <Select.Content>
      <Select.Item value={ALL_AUTHORS} label={$t`All authors`}>{$t`All authors`}</Select.Item>
      {#each authors.current as author (authorFilterKey(author))}
        {@const key = authorFilterKey(author)}
        <Select.Item value={key} label={author.authorName ?? $t`Unknown`}>
          {author.authorName ?? $t`Unknown`}
          <span class="text-muted-foreground ml-1">({author.commitCount})</span>
        </Select.Item>
      {/each}
    </Select.Content>
  </Select.Root>

  <Select.Root type="single" value={filters.changeTypeFilter} onValueChange={v => filters.changeTypeFilter = v}>
    <Select.Trigger class="w-48">
      {#if filters.changeTypeFilter === ALL_CHANGE_TYPES}
        {$t`All change types`}
      {:else}
        {changeTypes.current.find(ct => ct.key === filters.changeTypeFilter)?.label ?? filters.changeTypeFilter}
      {/if}
    </Select.Trigger>
    <Select.Content>
      <Select.Item value={ALL_CHANGE_TYPES} label={$t`All change types`}>{$t`All change types`}</Select.Item>
      {#each changeTypes.current as changeType (changeType.key)}
        <Select.Item value={changeType.key} label={changeType.label}>
          {changeType.label}
          <span class="text-muted-foreground ml-1">({changeType.commitCount})</span>
        </Select.Item>
      {/each}
    </Select.Content>
  </Select.Root>

  <Switch bind:checked={filters.excludeFieldWorks} label={$t`Hide FieldWorks`} class="shrink-0" />

  <ResponsiveMenu.Root>
    <ResponsiveMenu.Trigger class={cn(buttonVariants({variant: 'secondary', size: 'xs'}), 'h-8')}>
      {#snippet child({props})}
        <Button {...props} size="xs" variant="secondary">
          {sortLabels[filters.sort]}
        </Button>
      {/snippet}
    </ResponsiveMenu.Trigger>
    <ResponsiveMenu.Content align="start">
      {#each Object.values(ActivitySort) as sortOption (sortOption)}
        <ResponsiveMenu.Item
          onSelect={() => filters.sort = sortOption}
          class={cn(filters.sort === sortOption && 'bg-muted')}>
          {sortLabels[sortOption]}
        </ResponsiveMenu.Item>
      {/each}
    </ResponsiveMenu.Content>
  </ResponsiveMenu.Root>
</div>
