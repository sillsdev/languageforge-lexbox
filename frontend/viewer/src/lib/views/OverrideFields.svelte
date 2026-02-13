<script lang="ts">
  import {initView, useCurrentView} from '$lib/views/view-service';
  import type {FieldId} from '$lib/entry-editor/field-data';
  import type {FieldView, Overrides, ViewFields} from './view-data';
  import type {Snippet} from 'svelte';
  import {watch} from 'runed';

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
      fields: overrideEntityFields(currentView.fields, shownFields, respectOrder),
      overrides: {
        ...currentView.overrides,
        ...overrides
      }
    };
  });

  function overrideEntityFields(fields: ViewFields, shownFields: FieldId[], respectOrder: boolean): ViewFields {
    return {
      entry: overrideRecord(fields.entry, shownFields, respectOrder),
      sense: overrideRecord(fields.sense, shownFields, respectOrder),
      example: overrideRecord(fields.example, shownFields, respectOrder),
    };
  }

  function overrideRecord<T extends string>(record: Record<T, FieldView>, shownFields: FieldId[], respectOrder: boolean): Record<T, FieldView> {
    return Object.fromEntries(
      (Object.entries(record) as [T, FieldView][]).map(([id, field]) => [id, {
        ...field,
        show: shownFields.includes(id as FieldId),
        order: respectOrder ? shownFields.indexOf(id as FieldId) : field.order
      }])
    ) as Record<T, FieldView>;
  }
</script>

{@render children?.()}
