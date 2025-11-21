<script lang="ts">
  import EntryOrSensePicker, {type EntrySenseSelection} from '../EntryOrSensePicker.svelte';
  import {randomId} from '$lib/utils';
  import EntryOrSenseItemList from '../EntryOrSenseItemList.svelte';
  import type {IEntry, IComplexFormComponent} from '$lib/dotnet-types';
  import {useWritingSystemService} from '$project/data';
  import {Button} from '$lib/components/ui/button';
  import {t} from 'svelte-i18n-lingui';
  import {pt} from '$lib/views/view-text';
  import {useCurrentView} from '$lib/views/view-service';
  import type {ReadonlyDeep} from 'type-fest';

  type Props = {
    value: IComplexFormComponent[];
    readonly: boolean;
    entry: ReadonlyDeep<IEntry>;
    onchange: (value: IComplexFormComponent[]) => void;
  }

  let {
    value = $bindable(),
    readonly,
    entry,
    onchange,
  }: Props = $props();

  let writingSystemService = useWritingSystemService();
  const currentView = useCurrentView();

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

  function disableEntry(e: IEntry): false | { reason: string } {
    if (e.id === entry.id) return { reason: pt($t`Current Entry`, $t`Current Word`, $currentView) };
    if (entry.components.some((c) => c.componentEntryId === e.id)) return { reason: pt($t`Component`, $t`Part`, $currentView) };
    if (entry.complexForms.some((cf) => cf.complexFormEntryId === e.id)) return { reason: pt($t`Complex Form`, $t`Part of`, $currentView) };
    return false;
  }
</script>

<EntryOrSenseItemList bind:items={value} {readonly} {onchange} getEntryId={(e) => e.complexFormEntryId} getHeadword={(e) => e.complexFormHeadword}>
  {#snippet actions()}
    <EntryOrSensePicker title={pt($t`Add complex form`, $t`Add part of`, $currentView)} mode="only-entries" pick={(e) => addComplexForm(e)}
      {disableEntry}>
      {#snippet trigger({ props })}
        <Button {...props} icon="i-mdi-plus" size="xs">
          {pt($t`Complex Form`, $t`Part of`, $currentView)}
        </Button>
      {/snippet}
    </EntryOrSensePicker>
  {/snippet}
</EntryOrSenseItemList>
