<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import type {IActivityChangeType} from '$lib/dotnet-types';
  import {Checkbox} from '$lib/components/ui/checkbox';
  import {Icon} from '$lib/components/ui/icon';
  import {SvelteSet} from 'svelte/reactivity';
  import {pt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {changeTypeSection, CHANGE_TYPE_SECTIONS, type ChangeTypeSection} from './change-type-groups';
  import FacetFilter from './FacetFilter.svelte';
  import FacetFilterRow from './FacetFilterRow.svelte';

  // Grouped tri-state filter over the generated change types (NN/g filter-hierarchy + PatternFly grouped
  // checkbox select). Selection semantics: empty = no filter (everything shows); a non-empty set narrows
  // the feed. Toggles apply instantly — the caller debounces the query, not this component.
  let {changeTypes, selected, onSelectionChange}: {
    changeTypes: readonly IActivityChangeType[];
    selected: readonly string[];
    onSelectionChange: (keys: string[]) => void;
  } = $props();

  const viewService = useViewService();

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

  function visibleTypes(section: Section): IActivityChangeType[] {
    return query ? section.types.filter((ct) => ct.label.toLowerCase().includes(query)) : section.types;
  }
  function selectedCount(section: Section): number {
    return section.types.filter((ct) => selectedSet.has(ct.key)).length;
  }

  // Collapsed by default so ~6 header rows orient the eye; a group with a PARTIAL selection auto-expands
  // when the popover opens (active state must never hide), and searching shows every match regardless.
  function onOpen() {
    expanded.clear();
    for (const section of sections) {
      const count = selectedCount(section);
      if (count > 0 && count < section.types.length) expanded.add(section.id);
    }
  }

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
</script>

<FacetFilter
  title={$t`Activity type`}
  {triggerLabel}
  active={selected.length > 0}
  onReset={() => apply(new Set())}
  clearLabel={$t`Clear activity type filter`}
  showSearch
  searchPlaceholder={$t`Search activity types`}
  bind:search
  {onOpen}
  isEmpty={!!query && sections.every((s) => visibleTypes(s).length === 0)}
  emptyText={$t`No matching activity types`}>
  {#each sections as section (section.id)}
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
          <FacetFilterRow indent
            checked={selectedSet.has(ct.key)}
            onToggle={() => toggleType(ct.key)}
            onOnly={() => only([ct.key])}
            label={ct.label} />
        {/each}
      {/if}
    {/if}
  {/each}
</FacetFilter>
