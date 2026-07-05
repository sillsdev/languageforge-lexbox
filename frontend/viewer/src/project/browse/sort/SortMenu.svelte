<script lang="ts" module>
  import {SortField} from '$lib/dotnet-types';
  import {msg} from 'svelte-i18n-lingui';
  import type {IconClass} from '$lib/icon-class';

  export type SortDirection = 'asc' | 'desc';

  const sortLabels = {
    [SortField.SearchRelevance]: msg`Best match`,
    [SortField.Headword]: msg`Headword`,
    [SortField.Gloss]: msg`Gloss`,
  } as const;
  const glossLiteLabel = msg`Meaning`;

  const sortIcons: Partial<Record<SortField, Record<SortDirection, IconClass>>> = {
    [SortField.Headword]: {
      asc: 'i-mdi-sort-alphabetical-ascending',
      desc: 'i-mdi-sort-alphabetical-descending'
    },
    [SortField.Gloss]: {
      asc: 'i-mdi-sort-alphabetical-ascending',
      desc: 'i-mdi-sort-alphabetical-descending'
    }
  };
</script>

<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {badgeVariants} from '$lib/components/ui/badge';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {cn} from '$lib/utils';
  import {watch, type Getter} from 'runed';
  import {Icon} from '$lib/components/ui/icon';
  import {sortOptions, type SortConfig} from './options';
  import {Button, buttonVariants} from '$lib/components/ui/button';
  import {pt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';

  type Props = {
    value?: SortConfig;
    autoSelector: Getter<SortField>;
  };

  let {
    value = $bindable(),
    autoSelector,
  }: Props = $props();

  const viewService = useViewService();

  let selectedSortField = $state<SortField>();
  let direction = $state<SortDirection>('asc');
  const autoSort = $derived(autoSelector());
  const sortField = $derived(selectedSortField ?? autoSort);
  watch(() => ({ sortField, direction }), ({ sortField, direction }) => {
    value = { field: sortField, dir: direction } as SortConfig;
  });
</script>

{#snippet sortLabel(field: SortField)}
  {#if field === SortField.Gloss}
    {pt($t(sortLabels[field]), $t(glossLiteLabel), viewService.currentView)}
  {:else}
    {$t(sortLabels[field])}
  {/if}
{/snippet}

<ResponsiveMenu.Root>
  <ResponsiveMenu.Trigger class={cn(buttonVariants({variant: 'secondary', size: 'xs'}), badgeVariants({ variant: 'secondary' }), 'border-none h-7')}>
    {#snippet child({props})}
      <Button {...props}
        icon={sortIcons[sortField]?.[direction] ?? 'i-mdi-arrow-down'}
        iconProps={{ class: 'size-4' }}>
        {@render sortLabel(sortField)}
      </Button>
    {/snippet}
  </ResponsiveMenu.Trigger>
  <ResponsiveMenu.Content align="start">
    <ResponsiveMenu.Item
        onSelect={() => {
          selectedSortField = undefined;
          direction = 'asc';
        }}
        class={cn(!selectedSortField && 'bg-muted')}
        >
        {$t`Auto`}
        <span class="text-muted-foreground ml-auto">
          ({@render sortLabel(autoSort)})
        </span>
    </ResponsiveMenu.Item>
    {#each sortOptions as option (option)}
      {@const icon = sortIcons[option.field]?.[option.dir]}
      <ResponsiveMenu.Item
        onSelect={() => {
          selectedSortField = option.field;
          direction = option.dir;
        }}
        class={cn(selectedSortField === option.field && direction === option.dir && 'bg-muted')}
        >
        {#if icon}
          <Icon {icon} />
        {/if}
        {@render sortLabel(option.field)}
      </ResponsiveMenu.Item>
    {/each}
  </ResponsiveMenu.Content>
</ResponsiveMenu.Root>
