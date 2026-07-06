<script lang="ts">
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import type {IVariant} from '$lib/dotnet-types';
  import {useVariantTypes, useWritingSystemService} from '$project/data';

  type Props = {
    variant: IVariant;
    readonly: boolean;
    onchange?: () => void;
  }

  let {variant, readonly, onchange}: Props = $props();

  const variantTypes = useVariantTypes();
  const writingSystemService = useWritingSystemService();

  function hasType(id: string): boolean {
    return variant.types.some((type) => type.id === id);
  }

  function toggleType(id: string) {
    if (!variantTypes.current.some((vt) => vt.id === id)) return;
    if (hasType(id)) {
      variant.types = variant.types.filter((existing) => existing.id !== id);
    } else {
      // order 0 = "append": the backend picks the real order after the current last type
      variant.types = [...variant.types, {id, order: 0}];
    }
    onchange?.();
  }
</script>

{#if !readonly && variantTypes.current.length}
  <DropdownMenu.Sub>
    <DropdownMenu.SubTrigger>
      <Icon icon="i-mdi-tag-outline" />
      {$t`Variant type`}
    </DropdownMenu.SubTrigger>
    <DropdownMenu.SubContent>
      {#each variantTypes.current as type (type.id)}
        <DropdownMenu.CheckboxItem
          checked={hasType(type.id)}
          closeOnSelect={false}
          onCheckedChange={() => toggleType(type.id)}>
          {writingSystemService.pickBestAlternative(type.name, 'analysis')}
        </DropdownMenu.CheckboxItem>
      {/each}
    </DropdownMenu.SubContent>
  </DropdownMenu.Sub>
{/if}
