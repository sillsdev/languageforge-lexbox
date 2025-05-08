<script lang="ts">
  import EntryOrSensePicker, { type EntrySenseSelection } from '../EntryOrSensePicker.svelte';
  import { randomId } from '$lib/utils';
  import EntryOrSenseItemList from '../EntryOrSenseItemList.svelte';
  import {Button} from '$lib/components/ui/button';
  import type { IEntry, ISense, IComplexFormComponent } from '$lib/dotnet-types';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
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

  const writingSystemService = useWritingSystemService();

  function addComponent(selection: EntrySenseSelection) {
    const component: IComplexFormComponent = {
      id: randomId(),
      complexFormEntryId: entry.id,
      complexFormHeadword: writingSystemService.headword(entry),
      componentEntryId: selection.entry.id,
      componentSenseId: selection.sense?.id,
      componentHeadword: writingSystemService.headword(selection.entry),
    };
    value = [...value, component];
    onchange?.(value);
  }

  function disableEntry(e: IEntry): false | { disableSenses?: true, reason: string } {
    if (e.id === entry.id) return { reason: $t`Current Entry`, disableSenses: true };
    if (entry.components.some((c) => c.componentEntryId === e.id && !c.componentSenseId)) return { reason: $t`Component` };
    if (entry.complexForms.some((cf) => cf.complexFormEntryId === e.id)) return { reason: $t`Complex Form`, disableSenses: true };
    return false;
  }

  function disableSense(s: ISense, e: IEntry): false | string {
    if (entry.components.some((c) => c.componentEntryId === e.id && c.componentSenseId === s.id)) return $t`Component`;
    return false;
  }
</script>

<EntryOrSenseItemList bind:items={value} {readonly} orderable {onchange} getEntryId={(e) => e.componentEntryId} getHeadword={(e) => e.componentHeadword}>
  {#snippet actions()}
    <EntryOrSensePicker title={$t`Add component to complex form`} pick={(e) => addComponent(e)}
      {disableEntry} {disableSense}>
      {#snippet trigger({ props })}
        <Button {...props} icon="i-mdi-plus" size="xs">
          {$t`Add Component`}
        </Button>
      {/snippet}
    </EntryOrSensePicker>
  {/snippet}
</EntryOrSenseItemList>
