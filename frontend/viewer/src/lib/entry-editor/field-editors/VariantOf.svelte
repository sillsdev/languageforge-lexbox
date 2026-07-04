<script lang="ts">
  import EntryOrSensePicker, {type EntrySenseSelection} from '../EntryOrSensePicker.svelte';
  import {randomId} from '$lib/utils';
  import EntryOrSenseItemList from '../EntryOrSenseItemList.svelte';
  import VariantTypesMenuItems from './VariantTypesMenuItems.svelte';
  import type {IEntry, ISense, IVariant} from '$lib/dotnet-types';
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

  function addVariantOf(selection: EntrySenseSelection) {
    // FLEx assigns the Unspecified Variant type when the user doesn't pick one
    const unspecified = variantTypes.current.find((vt) => vt.id === UNSPECIFIED_VARIANT_TYPE_ID);
    const variant: IVariant = {
      id: randomId(),
      variantEntryId: entry.id,
      variantHeadword: writingSystemService.headword(entry),
      mainEntryId: selection.entry.id,
      mainSenseId: selection.sense?.id,
      mainHeadword: writingSystemService.headword(selection.entry),
      types: unspecified ? [unspecified] : [],
      hideMinorEntry: false,
      comment: {},
    };
    value = [...value, variant];
    onchange?.(value);
  }

  function disableEntry(e: IEntry): false | { disableSenses?: true, reason: string } {
    if (e.id === entry.id) return { reason: pt($t`Current Entry`, $t`Current Word`, viewService.currentView), disableSenses: true };
    if (entry.variantOf.some((v) => v.mainEntryId === e.id && !v.mainSenseId)) return { reason: $t`Variant of` };
    // the reverse link would make a cycle, which FieldWorks rejects
    if (entry.variants.some((v) => v.variantEntryId === e.id)) return { reason: $t`Variant`, disableSenses: true };
    return false;
  }

  function disableSense(s: ISense, e: IEntry): false | string {
    if (entry.variantOf.some((v) => v.mainEntryId === e.id && v.mainSenseId === s.id)) return $t`Variant of`;
    return false;
  }
</script>

<EntryOrSenseItemList bind:items={value} {readonly} {onchange} getEntryId={(v) => v.mainEntryId} getHeadword={(v) => v.mainHeadword}>
  {#snippet extraMenuItems(variant)}
    <VariantTypesMenuItems {variant} {readonly} onchange={() => onchange?.(value)} />
  {/snippet}
  {#snippet actions()}
    <EntryOrSensePicker title={$t`Add variant of`} pick={(e) => addVariantOf(e)}
      {disableEntry} {disableSense}>
      {#snippet trigger({ props })}
        <Button {...props} icon="i-mdi-plus" size="xs">
          {$t`Variant of`}
        </Button>
      {/snippet}
    </EntryOrSensePicker>
  {/snippet}
</EntryOrSenseItemList>
