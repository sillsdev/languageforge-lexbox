<script lang="ts" module>
  import type {WritingSystemSelection} from '$project/data';

  export type SelectedField = {
    id: string;
    ws: WritingSystemSelection;
  }

  export type FilterField = SelectedField & { label: string };
</script>

<script lang="ts">
  import * as Select from '$lib/components/ui/select';
  import {untrack} from 'svelte';
  import {t} from 'svelte-i18n-lingui';

  let {value = $bindable(), fields}: { value: SelectedField | null, fields: FilterField[] } = $props();

  const labeledValue = $derived(fields.find(f => f.id === value?.id) ?? fields[0]);
  // Reactively fall back to the default field so the parent can reset the selection by setting value = null.
  $effect(() => {
    if (!value) value = untrack(() => labeledValue);
  });
</script>

<Select.Root type="single" bind:value={() => (value ?? fields[0]).id, (newId) => value = fields.find(f => f.id === newId) ?? fields[0]}>
  <Select.Trigger class="flex-1">
    {labeledValue.label}
  </Select.Trigger>
  <Select.Content>
    <Select.Group>
      <Select.GroupHeading>{$t`Field`}</Select.GroupHeading>
      {#each fields as field (field.id)}
        <Select.Item value={field.id} label={field.label}>
          {field.label}
        </Select.Item>
      {/each}
    </Select.Group>
  </Select.Content>
</Select.Root>
