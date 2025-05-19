<script lang="ts">
  import EntryOrSensePicker, { type EntrySenseSelection } from '../EntryOrSensePicker.svelte';
  import { randomId } from '$lib/utils';
  import EntryOrSenseItemList from '../EntryOrSenseItemList.svelte';
  import type { IEntry, IComplexFormComponent } from '$lib/dotnet-types';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {Button} from '$lib/components/ui/button';
  import {t} from 'svelte-i18n-lingui';

  type Props = {
    value: IComplexFormComponent[];
    readonly: boolean;
    entry: IEntry;
    onchange: (value: IComplexFormComponent[]) => void;
  }

  let {
    value = $bindable(),
    readonly,
    entry,
    onchange,
  }: Props = $props();

  let writingSystemService = useWritingSystemService();

  function addComplexForm(selection: EntrySenseSelection) {
    const complexForm: IComplexFormComponent = {
      id: randomId(),
      complexFormEntryId: selection.entry.id,
      complexFormHeadword: writingSystemService.headword(selection.entry),
      componentEntryId: entry.id,
      componentHeadword: writingSystemService.headword(entry),
    };
    value = [...value, complexForm];
    onchange?.(value);
  }

  function disableEntry(e: IEntry): false | { reason: string, andSenses?: true } {
    if (e.id === entry.id) return { reason: $t`Current Entry` };
    if (entry.components.some((c) => c.componentEntryId === e.id)) return { reason: $t`Component` };
    if (entry.complexForms.some((cf) => cf.complexFormEntryId === e.id)) return { reason: $t`Complex Form` };
    return false;
  }
</script>

<EntryOrSenseItemList bind:items={value} {readonly} {onchange} getEntryId={(e) => e.complexFormEntryId} getHeadword={(e) => e.complexFormHeadword}>
  {#snippet actions()}
    <EntryOrSensePicker title={$t`Add complex form`} mode="only-entries" pick={(e) => addComplexForm(e)}
      {disableEntry}>
      {#snippet trigger({ props })}
        <Button {...props} icon="i-mdi-plus" size="xs">
          {$t`Add Complex Form`}
        </Button>
      {/snippet}
    </EntryOrSensePicker>
  {/snippet}
</EntryOrSenseItemList>
