<script lang="ts">
  import {initView, useCurrentView} from '$lib/views/view-service';
  import type {EntityViewFields, FieldView, Overrides} from './view-data';
  import type {Snippet} from 'svelte';
  import {watch} from 'runed';
  import type {FieldId} from './fields';

  interface Props {
    shownFields?: FieldId[];
    respectOrder?: boolean;
    overrides?: Overrides
    children?: Snippet;
  }

  let {
    shownFields = [],
    respectOrder = false,
    children,
    overrides = {}
  }: Props = $props();

  const currentView = useCurrentView();
  const overrideView = initView(undefined, false);
  watch(() => [shownFields, respectOrder, $currentView, overrides] as const, ([shownFields, respectOrder, currentView, overrides]) => {
    $overrideView = {
      ...currentView,
      fields: {
        entry: overrideEntityFields(currentView.fields.entry, shownFields, respectOrder),
        sense: overrideEntityFields(currentView.fields.sense, shownFields, respectOrder),
        example: overrideEntityFields(currentView.fields.example, shownFields, respectOrder),
      },
      overrides: {
        ...currentView.overrides,
        ...overrides
      }
    };
  });

  function overrideEntityFields<T extends EntityViewFields>(entityFields: T, shownFields: FieldId[], respectOrder: boolean): T {
    return Object.fromEntries(
      (Object.entries(entityFields) as [FieldId, FieldView][]).map(([id, field]) => [id, {
        ...field,
        show: shownFields.includes(id),
        order: respectOrder ? shownFields.indexOf(id) : field.order
      }])
    ) as T;
  }
</script>

{@render children?.()}
