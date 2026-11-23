<script lang="ts" module>
  import {MorphTypeKind} from '$lib/dotnet-types';

  export const WORDS_AND_PHRASES: string[] = [
    MorphTypeKind.Root, MorphTypeKind.BoundRoot,
    MorphTypeKind.Stem, MorphTypeKind.BoundStem,
    MorphTypeKind.Particle,
    MorphTypeKind.Phrase, MorphTypeKind.DiscontiguousPhrase,
  ];

  const AFFIXES: string[] = [
    MorphTypeKind.Prefix, MorphTypeKind.Suffix, MorphTypeKind.Infix,
    MorphTypeKind.Circumfix, MorphTypeKind.Simulfix, MorphTypeKind.Suprafix,
    MorphTypeKind.PrefixingInterfix, MorphTypeKind.InfixingInterfix, MorphTypeKind.SuffixingInterfix,
  ];

  const CLITICS: string[] = [MorphTypeKind.Clitic, MorphTypeKind.Enclitic, MorphTypeKind.Proclitic];
</script>

<script lang="ts">
  import * as Select from '$lib/components/ui/select';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import {useWritingSystemService} from '$project/data';
  import {useMorphTypesService} from '$project/data/morph-types.svelte';
  import {resolveFilterKeys, type MultiFilterSelection} from '$lib/activity/utils';

  const ALL = '__all__';

  let {value = $bindable(), describedBy}: {value: MultiFilterSelection, describedBy?: string} = $props();

  const morphTypesService = useMorphTypesService();
  const wsService = useWritingSystemService();

  type Option = {kind: string, label: string};
  type Group = {key: string, label: string, kinds: string[], options: Option[]};

  const options = $derived(morphTypesService.current
    .filter(mt => !mt.deletedAt && mt.kind !== MorphTypeKind.Unknown)
    .map(mt => ({kind: mt.kind, label: wsService.pickBestAlternative(mt.name, 'analysis') || mt.kind}))
    .sort((a, b) => a.label.localeCompare(b.label)));
  const allKinds = $derived(options.map(o => o.kind));

  const groups: Group[] = $derived.by(() => {
    const defined = [
      {key: '__words__', label: $t`Words and phrases`, kinds: WORDS_AND_PHRASES},
      {key: '__affixes__', label: $t`Affixes`, kinds: AFFIXES},
      {key: '__clitics__', label: $t`Clitics`, kinds: CLITICS},
    ];
    const grouped = new Set(defined.flatMap(g => g.kinds));
    const other = {key: '__other__', label: $t`Other`, kinds: allKinds.filter(k => !grouped.has(k))};
    return [...defined, other]
      .map(g => ({...g, options: options.filter(o => g.kinds.includes(o.kind))}))
      .filter(g => g.options.length > 0);
  });

  // The selection can hold kinds this project doesn't list (e.g. the Lite default)
  const selected = $derived(resolveFilterKeys(value, allKinds));
  const selectedListed = $derived(allKinds.filter(k => selected.includes(k)));

  type Check = 'all' | 'some' | 'none';
  function checkState(kinds: string[]): Check {
    const count = kinds.filter(k => selected.includes(k)).length;
    return count === 0 ? 'none' : count === kinds.length ? 'all' : 'some';
  }
  const allCheck = $derived(value === 'all' ? 'all' : checkState(allKinds));

  // Names the selection when it is exactly one group
  const exactGroup = $derived(groups.find(g =>
    selectedListed.length === g.options.length && selectedListed.every(k => g.kinds.includes(k))));

  function normalize(kinds: string[]): MultiFilterSelection {
    return allKinds.length > 0 && allKinds.every(k => kinds.includes(k)) ? 'all' : kinds;
  }

  function onValueChange(newValue: string[]) {
    if (newValue.includes(ALL)) {
      value = allCheck === 'all' ? [] : 'all';
      return;
    }
    const group = groups.find(g => newValue.includes(g.key));
    if (group) {
      const groupKinds = [...new Set([...group.kinds, ...group.options.map(o => o.kind)])];
      const without = selected.filter(k => !groupKinds.includes(k));
      value = checkState(group.options.map(o => o.kind)) === 'all' ? without : normalize([...without, ...groupKinds]);
      return;
    }
    value = normalize(newValue);
  }
</script>

{#snippet checkIcon(check: Check)}
  {#if check === 'all'}
    <Icon icon="i-mdi-check" class="size-4" />
  {:else if check === 'some'}
    <Icon icon="i-mdi-minus" class="size-4" />
  {/if}
{/snippet}

<Select.Root type="multiple" value={selected} {onValueChange}>
  <Select.Trigger class="flex-1" aria-describedby={describedBy}>
    {#if allCheck === 'all' || selected.length === 0}
      <span class="text-muted-foreground">{$t`Any morph type`}</span>
    {:else if exactGroup}
      {exactGroup.label}
    {:else if selectedListed.length <= 3}
      {options.filter(o => selected.includes(o.kind)).map(o => o.label).join(', ')}
    {:else}
      {$t`${selectedListed.length} morph types`}
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
          {#snippet selectedIndicator()}{@render checkIcon(checkState(group.options.map(o => o.kind)))}{/snippet}
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
