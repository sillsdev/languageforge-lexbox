<script lang="ts" generics="T extends EntityType">
  import * as Checkbox from '$lib/components/ui/checkbox';

  import {type EntityType, getEntityConfig, entityFieldIds} from '$lib/views/entity-config';
  import type {IViewField, ViewBase} from '$lib/dotnet-types';
  import {pt, tvt} from '../view-text';

  interface Props {
    entityType: T;
    baseView: ViewBase;
    items?: IViewField[];
    onchange?: () => void;
  }

  let {entityType, baseView, items = $bindable<IViewField[]>([]), onchange = () => {}}: Props = $props();

  const entity = $derived(getEntityConfig(entityType));
  const fieldIds = $derived(entityFieldIds(entityType));

  function toCommittedFieldItems(next: IViewField[]): IViewField[] {
    return next.filter((item) => item.fieldId in entity).map((item) => ({fieldId: item.fieldId}));
  }

  function toggle(fieldId: string, checked: boolean) {
    if (checked) {
      if (!items.some((item) => item.fieldId === fieldId)) {
        items = toCommittedFieldItems([...items, {fieldId}]);
      }
    } else {
      items = toCommittedFieldItems(items.filter((item) => item.fieldId !== fieldId));
    }
    onchange();
  }

  function isChecked(fieldId: string): boolean {
    return items.some((item) => item.fieldId === fieldId);
  }
</script>

<div class="rounded-md border bg-muted/20 px-3 pt-3 md:pb-3">
  <div class="pb-1 md:pb-3 text-xs font-medium text-muted-foreground">
    {pt($tvt(entity.$label), baseView)}
  </div>
  <!-- If view-level field ordering is desired, an explicit flag should be introduced on the view. -->
  <Checkbox.Group>
    {#each fieldIds as fieldId (fieldId)}
      {@const checkboxId = `field-${entityType}-${fieldId as string}`}
      <Checkbox.Item
        id={checkboxId}
        checked={isChecked(fieldId)}
        onCheckedChange={(checked) => toggle(fieldId, !!checked)}
        label={pt($tvt(entity[fieldId].label), baseView)}
      />
    {/each}
  </Checkbox.Group>
</div>
