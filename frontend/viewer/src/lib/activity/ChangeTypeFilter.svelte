<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import type {IActivityChangeType} from '$lib/dotnet-types';
  import ResponsivePopup from '$lib/components/responsive-popup/responsive-popup.svelte';
  import {Button} from '$lib/components/ui/button';
  import {Checkbox} from '$lib/components/ui/checkbox';
  import {Input} from '$lib/components/ui/input';
  import {Icon} from '$lib/components/ui/icon';
  import {SvelteSet} from 'svelte/reactivity';
  import {pt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {changeTypeSection, CHANGE_TYPE_SECTIONS, type ChangeTypeSection} from './change-type-groups';

  // Grouped tri-state filter over the generated change types (NN/g filter-hierarchy + PatternFly grouped
  // checkbox select). Selection semantics: empty = no filter (everything shows); a non-empty set narrows
  // the feed. Toggles apply instantly — the caller debounces the query, not this component.
  let {changeTypes, selected, onSelectionChange}: {
    changeTypes: readonly IActivityChangeType[];
    selected: readonly string[];
    onSelectionChange: (keys: string[]) => void;
  } = $props();

  const viewService = useViewService();

  let open = $state(false);
  let search = $state('');
  const expanded = new SvelteSet<ChangeTypeSection>();

  function sectionLabel(section: ChangeTypeSection): string {
    const labels: Record<ChangeTypeSection, string> = {
      entry: pt($t`Entries`, $t`Words`, viewService.currentView),
      sense: pt($t`Senses`, $t`Meanings`, viewService.currentView),
      example: $t`Examples`,
      media: $t`Audio & media`,
      comments: $t`Comments`,
      vocabulary: $t`Lists & settings`,
    };
    return labels[section];
  }

  type Section = {id: ChangeTypeSection; label: string; types: IActivityChangeType[]};
  const sections = $derived.by((): Section[] =>
    CHANGE_TYPE_SECTIONS.map((id) => ({
      id,
      label: sectionLabel(id),
      types: changeTypes
        .filter((ct) => changeTypeSection(ct.key) === id)
        .toSorted((a, b) => a.label.localeCompare(b.label)),
    })).filter((s) => s.types.length > 0));

  const selectedSet = $derived(new Set(selected));
  const query = $derived(search.trim().toLowerCase());

  function matches(ct: IActivityChangeType): boolean {
    return !query || ct.label.toLowerCase().includes(query);
  }
  function visibleTypes(section: Section): IActivityChangeType[] {
    return query ? section.types.filter(matches) : section.types;
  }
  function selectedCount(section: Section): number {
    return section.types.filter((ct) => selectedSet.has(ct.key)).length;
  }

  // Collapsed by default so ~6 header rows orient the eye; a group with a PARTIAL selection auto-expands
  // when the popover opens (active state must never hide), and searching shows every match regardless.
  function onOpenChange(nowOpen: boolean) {
    if (!nowOpen) return;
    search = '';
    expanded.clear();
    for (const section of sections) {
      const count = selectedCount(section);
      if (count > 0 && count < section.types.length) expanded.add(section.id);
    }
  }
  $effect(() => onOpenChange(open));

  function isExpanded(section: Section): boolean {
    return !!query || expanded.has(section.id);
  }

  function apply(keys: Set<string>) {
    onSelectionChange([...keys]);
  }

  function toggleType(key: string) {
    // eslint-disable-next-line svelte/prefer-svelte-reactivity -- transient copy, applied and discarded
    const next = new Set(selectedSet);
    if (next.has(key)) next.delete(key);
    else next.add(key);
    apply(next);
  }

  function toggleSection(section: Section) {
    // eslint-disable-next-line svelte/prefer-svelte-reactivity -- transient copy, applied and discarded
    const next = new Set(selectedSet);
    const allSelected = selectedCount(section) === section.types.length;
    for (const ct of section.types) {
      if (allSelected) next.delete(ct.key);
      else next.add(ct.key);
    }
    apply(next);
  }

  function only(keys: string[]) {
    apply(new Set(keys));
  }

  function reset() {
    apply(new Set());
  }

  // Trigger summary: nothing selected → plain label; exactly one leaf (or exactly one whole group) → its
  // name; anything else → a count. Never truncated name lists.
  const triggerLabel = $derived.by(() => {
    if (selected.length === 0) return $t`Activity type`;
    if (selected.length === 1) {
      return changeTypes.find((ct) => ct.key === selected[0])?.label ?? selected[0];
    }
    const fullSection = sections.find(
      (s) => s.types.length === selected.length && s.types.every((ct) => selectedSet.has(ct.key)));
    if (fullSection) return fullSection.label;
    return $t`${selected.length} activity types`;
  });
  const active = $derived(selected.length > 0);
</script>

{#snippet sectionRows(section: Section)}
  {@const count = selectedCount(section)}
  {@const types = visibleTypes(section)}
  {#if types.length}
    <div class="group/header flex items-center gap-2 rounded-sm px-2 py-1.5 hover:bg-accent">
      <Checkbox
        checked={count === section.types.length}
        indeterminate={count > 0 && count < section.types.length}
        onCheckedChange={() => toggleSection(section)}
        aria-label={section.label} />
      <button type="button" class="flex grow items-center gap-1 text-start text-sm font-medium"
        aria-expanded={isExpanded(section)}
        onclick={() => expanded.has(section.id) ? expanded.delete(section.id) : expanded.add(section.id)}>
        <span class="grow">{section.label}</span>
        {#if count > 0 && count < section.types.length}
          <span class="text-xs text-muted-foreground">{count}/{section.types.length}</span>
        {/if}
        <Icon icon={isExpanded(section) ? 'i-mdi-chevron-up' : 'i-mdi-chevron-down'} class="size-4 text-muted-foreground" />
      </button>
      <button type="button"
        class="invisible text-xs text-muted-foreground hover:text-foreground group-hover/header:visible"
        onclick={() => only(section.types.map((ct) => ct.key))}>
        {$t`Only`}
      </button>
    </div>
    {#if isExpanded(section)}
      {#each types as ct (ct.key)}
        <div class="group/row flex items-center gap-2 rounded-sm py-1.5 pe-2 ps-7 hover:bg-accent">
          <Checkbox checked={selectedSet.has(ct.key)} onCheckedChange={() => toggleType(ct.key)} aria-label={ct.label} />
          <button type="button" class="grow text-start text-sm" onclick={() => toggleType(ct.key)}>
            {ct.label}
          </button>
          <button type="button"
            class="invisible text-xs text-muted-foreground hover:text-foreground group-hover/row:visible"
            onclick={() => only([ct.key])}>
            {$t`Only`}
          </button>
        </div>
      {/each}
    {/if}
  {/if}
{/snippet}

<div class="flex w-44 max-w-full grow items-center">
  <ResponsivePopup bind:open title={$t`Activity type`} contentProps={{class: 'w-80 p-0', align: 'start'}}>
    {#snippet trigger({props})}
      <Button {...props} variant="outline"
        class="min-w-0 grow justify-between font-normal {active ? 'border-primary/50 font-medium' : ''} {active ? 'rounded-e-none border-e-0' : ''}">
        <span class="truncate">{triggerLabel}</span>
        <Icon icon="i-mdi-chevron-down" class="size-4 shrink-0 opacity-50" />
      </Button>
    {/snippet}
    <div class="flex max-h-96 flex-col">
      <div class="p-2 pb-1">
        <Input type="search" placeholder={$t`Search activity types`} bind:value={search} />
      </div>
      <div class="min-h-0 grow overflow-y-auto p-1" role="group" aria-label={$t`Activity type`}>
        {#each sections as section (section.id)}
          {@render sectionRows(section)}
        {/each}
        {#if query && sections.every((s) => visibleTypes(s).length === 0)}
          <div class="p-3 text-center text-sm text-muted-foreground">{$t`No matching activity types`}</div>
        {/if}
      </div>
      {#if active}
        <div class="border-t p-1">
          <Button variant="ghost" size="sm" class="w-full" onclick={reset}>{$t`Reset`}</Button>
        </div>
      {/if}
    </div>
  </ResponsivePopup>
  {#if active}
    <!-- Clear the whole filter without opening the popover (PatternFly active-filter convention). -->
    <Button variant="outline" size="icon" class="shrink-0 rounded-s-none border-primary/50"
      aria-label={$t`Clear activity type filter`} onclick={reset}>
      <Icon icon="i-mdi-close" class="size-4" />
    </Button>
  {/if}
</div>
