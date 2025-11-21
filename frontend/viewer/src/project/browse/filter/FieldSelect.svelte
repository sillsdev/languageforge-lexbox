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
  import {pt} from '$lib/views/view-text';
  import {useCurrentView} from '$lib/views/view-service';

  const currentView = useCurrentView();
  let fields: LabeledSelectedField[] = $derived([
    { id: 'lexemeForm', label: pt($t`Lexeme Form`, $t`Word`, $currentView), ws: 'vernacular-no-audio' },
    { id: 'citationForm', label: pt($t`Citation Form`, $t`Display as`, $currentView), ws: 'vernacular-no-audio' },
    { id: 'senses.gloss', label: $t`Gloss`, ws: 'analysis-no-audio' },
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
