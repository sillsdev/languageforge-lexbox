<script lang="ts">
  import EntryOrSensePicker, {type EntrySenseSelection} from '../EntryOrSensePicker.svelte';
  import {randomId} from '$lib/utils';
  import EntryOrSenseItemList from '../EntryOrSenseItemList.svelte';
  import VariantTypesMenuItems from './VariantTypesMenuItems.svelte';
  import type {IEntry, IVariant} from '$lib/dotnet-types';
  import {UNSPECIFIED_VARIANT_TYPE_ID, useVariantTypes, useWritingSystemService} from '$project/data';
  import {Button} from '$lib/components/ui/button';
  import {t} from 'svelte-i18n-lingui';
  import {pt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import type {ReadonlyDeep} from 'type-fest';

  type Props = {
    value: IVariant[];
    readonly: boolean;
    entry: ReadonlyDeep<IEntry>;
    onchange: (value: IVariant[]) => void;
  }

  let {
    value = $bindable(),
    readonly,
    entry,
    onchange,
  }: Props = $props();

  const writingSystemService = useWritingSystemService();
  const variantTypes = useVariantTypes();
  const viewService = useViewService();

  function addVariant(selection: EntrySenseSelection) {
    // FLEx assigns the Unspecified Variant type when the user doesn't pick one
    const unspecified = variantTypes.current.find((vt) => vt.id === UNSPECIFIED_VARIANT_TYPE_ID);
    const variant: IVariant = {
      id: randomId(),
      variantEntryId: selection.entry.id,
      variantHeadword: writingSystemService.headword(selection.entry),
      mainEntryId: entry.id,
      mainHeadword: writingSystemService.headword(entry),
      types: unspecified ? [{id: unspecified.id, order: 0}] : [],
      hideMinorEntry: false,
      comment: {},
    };
    value = [...value, variant];
    onchange?.(value);
  }

  function disableEntry(e: IEntry): false | { reason: string } {
    if (e.id === entry.id) return { reason: pt($t`Current Entry`, $t`Current Word`, viewService.currentView) };
    if (entry.variants.some((v) => v.variantEntryId === e.id)) return { reason: $t`Variant` };
    // the reverse link would make a cycle, which FieldWorks rejects
    if (entry.variantOf.some((v) => v.mainEntryId === e.id)) return { reason: $t`Variant of` };
    return false;
  }
</script>

<EntryOrSenseItemList bind:items={value} {readonly} {onchange} getEntryId={(v) => v.variantEntryId} getHeadword={(v) => v.variantHeadword}>
  {#snippet extraMenuItems(variant)}
    <!-- reassign so the nested types edit reliably re-renders (see EntryEditor's `entry = entry`) -->
    <VariantTypesMenuItems {variant} {readonly} onchange={() => { value = [...value]; onchange?.(value); }} />
  {/snippet}
  {#snippet actions()}
    <EntryOrSensePicker title={$t`Add variant`} mode="only-entries" pick={(e) => addVariant(e)}
      {disableEntry}>
      {#snippet trigger({ props })}
        <Button {...props} icon="i-mdi-plus" size="xs">
          {$t`Variant`}
        </Button>
      {/snippet}
    </EntryOrSensePicker>
  {/snippet}
</EntryOrSenseItemList>
