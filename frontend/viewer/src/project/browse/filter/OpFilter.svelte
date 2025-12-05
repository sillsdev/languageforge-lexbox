<script lang="ts" module>
  export type Op = 'starts-with' | 'contains' | 'ends-with' | 'equals' | 'not-equals';
</script>

<script lang="ts">
  import * as Select from '$lib/components/ui/select';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import type {IconClass} from '$lib/icon-class';
  let {value = $bindable()}: {value: string} = $props();
  const ops: {label: string, value: Op, icon: IconClass}[] = $derived([
    {label: $t`Starts with`, value: 'starts-with', icon: 'i-mdi-contain-start'},
    {label: $t`Contains`, value: 'contains', icon: 'i-mdi-contain'},
    {label: $t`Ends with`, value: 'ends-with', icon: 'i-mdi-contain-end'},
    {label: $t`Equals`, value: 'equals', icon: 'i-mdi-equal'},
    {label: $t`Not equal`, value: 'not-equals', icon: 'i-mdi-not-equal-variant'},
  ]);
</script>

<Select.Root type="single" bind:value>
  <Select.Trigger class="w-13" downIcon={null}>
    <Icon icon={ops.find(o => o.value === value)?.icon ?? 'i-mdi-close'}/>
  </Select.Trigger>
  <Select.Content>
    <Select.Group>
      <Select.GroupHeading>{$t`Filter By`}</Select.GroupHeading>
      {#each ops as op (op.value)}
        <Select.Item value={op.value}>
          <Icon icon={op.icon} class="mr-2"/>
          {op.label}
        </Select.Item>
      {/each}
    </Select.Group>
  </Select.Content>
</Select.Root>
