<script lang="ts">
  import * as Select from '$lib/components/ui/select';
  import {Icon} from '$lib/components/ui/icon';
  import {plural, t} from 'svelte-i18n-lingui';
  import {MorphTypeKind} from '$lib/dotnet-types';
  import {useWritingSystemService} from '$project/data';
  import {useMorphTypesService} from '$project/data/morph-types.svelte';
  import {MORPH_TYPE_GROUPS, OTHER_MORPH_TYPES} from '$project/data/morph-type-groups';
  import {checkState, selectedKinds, toggleAll, toggleGroup, toggleKind, type CheckState, type MorphTypeFilter} from './morph-type-filter';

  const ALL = '__all__';

  let {value = $bindable(), labelledBy, describedBy}: {value: MorphTypeFilter, labelledBy?: string, describedBy?: string} = $props();
  const triggerId = $props.id();

  const morphTypesService = useMorphTypesService();
  const wsService = useWritingSystemService();

  const options = $derived(morphTypesService.current
    .filter(mt => !mt.deletedAt && mt.kind !== MorphTypeKind.Unknown)
    .map(mt => ({kind: mt.kind, label: wsService.pickBestAlternative(mt.name, 'analysis') || mt.kind}))
    .sort((a, b) => a.label.localeCompare(b.label)));
  const listed = $derived(options.map(o => o.kind));

  const groups = $derived.by(() => {
    const grouped = new Set(MORPH_TYPE_GROUPS.flatMap(g => g.kinds));
    const other = {...OTHER_MORPH_TYPES, kinds: listed.filter(k => !grouped.has(k))};
    return [...MORPH_TYPE_GROUPS, other]
      .map(g => ({...g, label: $t(g.label), options: options.filter(o => g.kinds.includes(o.kind))}))
      .filter(g => g.options.length > 0);
  });

  const selected = $derived(selectedKinds(value, listed));
  const selectedListed = $derived(listed.filter(k => selected.includes(k)));
  const allCheck: CheckState = $derived(value === 'all' ? 'all' : checkState(selected, listed));
  const groupChecks = $derived(new Map(groups.map(g => [g.key, checkState(selected, g.options.map(o => o.kind))])));
  const selectedGroup = $derived(groups.find(g =>
    selectedListed.length === g.options.length && selectedListed.every(k => g.kinds.includes(k))));

  // Fully checked header rows are in the value too, so bits-ui marks them aria-selected
  const selectValue = $derived([
    ...selected,
    ...(allCheck === 'all' ? [ALL] : []),
    ...groups.filter(g => groupChecks.get(g.key) === 'all').map(g => g.key),
  ]);

  function onValueChange(newValue: string[]) {
    const toggled = [...newValue.filter(v => !selectValue.includes(v)), ...selectValue.filter(v => !newValue.includes(v))];
    const group = groups.find(g => toggled.includes(g.key));
    const kind = listed.find(k => toggled.includes(k));
    if (toggled.includes(ALL)) value = toggleAll(value, listed);
    else if (group) value = toggleGroup(value, group.kinds, listed);
    else if (kind) value = toggleKind(value, kind, listed);
  }
</script>

{#snippet checkIcon(check: CheckState | undefined)}
  {#if check === 'all'}
    <Icon icon="i-mdi-check" class="size-4" />
  {:else if check === 'some'}
    <Icon icon="i-mdi-minus" class="size-4" />
  {/if}
{/snippet}

<Select.Root type="multiple" value={selectValue} {onValueChange}>
  <!-- Labelled by the label plus itself, so the selection stays part of the name -->
  <Select.Trigger id={triggerId} class="flex-1" aria-labelledby={labelledBy ? `${labelledBy} ${triggerId}` : undefined} aria-describedby={describedBy}>
    {#if allCheck === 'all' || selected.length === 0}
      <span class="text-muted-foreground">{$t`Any morph type`}</span>
    {:else if selectedGroup}
      {selectedGroup.label}
    {:else if selectedListed.length > 0 && selectedListed.length <= 3}
      {options.filter(o => selected.includes(o.kind)).map(o => o.label).join(', ')}
    {:else}
      {$plural(selectedListed.length || selected.length, {one: '# morph type', other: '# morph types'})}
    {/if}
  </Select.Trigger>
  <Select.Content>
    <Select.Item value={ALL} label={$t`All morph types`}>
      {#snippet selectedIndicator()}{@render checkIcon(allCheck)}{/snippet}
      <span class="font-bold">{$t`All morph types`}</span>
    </Select.Item>
    {#each groups as group (group.key)}
      <Select.Group>
        <Select.Item value={group.key} label={group.label}>
          {#snippet selectedIndicator()}{@render checkIcon(groupChecks.get(group.key))}{/snippet}
          <span class="font-bold">{group.label}</span>
        </Select.Item>
        {#each group.options as option (option.kind)}
          <Select.Item value={option.kind} label={option.label} class="ps-6">
            {option.label}
          </Select.Item>
        {/each}
      </Select.Group>
    {/each}
  </Select.Content>
</Select.Root>
