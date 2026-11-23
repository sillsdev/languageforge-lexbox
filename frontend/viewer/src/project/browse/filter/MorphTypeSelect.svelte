<script lang="ts">
  import * as Select from '$lib/components/ui/select';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import {MorphTypeKind} from '$lib/dotnet-types';
  import {useWritingSystemService} from '$project/data';
  import {useMorphTypesService} from '$project/data/morph-types.svelte';
  import {applyMultiSelectValue, isAllFilterSelection, resolveFilterKeys, type MultiFilterSelection} from '$lib/activity/utils';

  const ALL = '__all__';

  let {value = $bindable(), describedBy, selectionLabel}: {
    value: MultiFilterSelection,
    describedBy?: string,
    selectionLabel?: string,
  } = $props();

  const morphTypesService = useMorphTypesService();
  const wsService = useWritingSystemService();

  const options = $derived.by(() => {
    const morphTypes = morphTypesService.current.filter(mt => !mt.deletedAt && mt.kind !== MorphTypeKind.Unknown);
    return morphTypes
      .map(mt => ({kind: mt.kind, label: wsService.pickBestAlternative(mt.name, 'analysis') || mt.kind}))
      .sort((a, b) => a.label.localeCompare(b.label));
  });
  const allKinds = $derived(options.map(o => o.kind as string));
  const selected = $derived(resolveFilterKeys(value, allKinds));
  // The selection can hold kinds this project doesn't list (e.g. view defaults)
  const selectedLabels = $derived(options.filter(o => selected.includes(o.kind)).map(o => o.label));
  const showingAll = $derived(selected.length === 0 || isAllFilterSelection(value, allKinds));

  function onValueChange(newValue: string[]) {
    value = applyMultiSelectValue(newValue, allKinds, ALL, value);
  }
</script>

<Select.Root type="multiple" value={selected} {onValueChange}>
  <Select.Trigger class="flex-1" aria-describedby={describedBy}>
    {#if selectionLabel}
      {selectionLabel}
    {:else if showingAll}
      <span class="text-muted-foreground">{$t`Any morph type`}</span>
    {:else if selectedLabels.length <= 3}
      {selectedLabels.join(', ')}
    {:else}
      {$t`${selectedLabels.length} morph types`}
    {/if}
  </Select.Trigger>
  <Select.Content>
    <Select.Item value={ALL} label={$t`All morph types`}>
      {#snippet selectedIndicator()}
        {#if isAllFilterSelection(value, allKinds)}
          <Icon icon="i-mdi-check" class="size-4" />
        {:else if selected.length > 0}
          <Icon icon="i-mdi-minus" class="size-4" />
        {/if}
      {/snippet}
      <span class="font-bold">{$t`All morph types`}</span>
    </Select.Item>
    {#each options as option (option.kind)}
      <Select.Item value={option.kind} label={option.label}>
        {option.label}
      </Select.Item>
    {/each}
  </Select.Content>
</Select.Root>
