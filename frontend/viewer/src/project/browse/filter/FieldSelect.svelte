<script lang="ts" module>
  import type {WritingSystemSelection} from '$project/data';

  export type SelectedField = {
    id: string;
    ws: WritingSystemSelection;
  }
</script>

<script lang="ts">
  import * as Select from '$lib/components/ui/select';
  import {t} from 'svelte-i18n-lingui';
  import {pt, tvt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {entityConfig} from '$lib/views/entity-config';

  const viewService = useViewService();
  let fields: LabeledSelectedField[] = $derived([
    { id: 'lexemeForm', label: pt($tvt(entityConfig.entry.lexemeForm.label), viewService.currentView), ws: 'vernacular-no-audio' },
    { id: 'citationForm', label: pt($tvt(entityConfig.entry.citationForm.label), viewService.currentView), ws: 'vernacular-no-audio' },
    { id: 'senses.gloss', label: $t(entityConfig.sense.gloss.label), ws: 'analysis-no-audio' },
  ]);

  let {value = $bindable()}: { value: SelectedField | null } = $props();

  type LabeledSelectedField = SelectedField & { label: string };

  const labeledValue = $derived(fields.find(f => f.id === value?.id) ?? fields[0]);
  // svelte-ignore state_referenced_locally
  value ??= labeledValue;
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
