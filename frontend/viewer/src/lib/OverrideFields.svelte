<script lang="ts">
  import {initView, useCurrentView} from '$lib/views/view-service';
  import type {FieldId} from '$lib/entry-editor/field-data';
  import type {FieldView, View} from './views/view-data';
  import type {Snippet} from 'svelte';
  import {watch} from 'runed';

  interface Props {
    shownFields?: FieldId[];
    respectOrder?: boolean;
    overrides?: View['overrides']
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
  watch(() => [shownFields, respectOrder, $currentView, overrides], ([shownFields, respectOrder, currentView, overrides]) => {
    $overrideView = {
      ...currentView,
      fields: Object.fromEntries((Object.entries(currentView.fields) as Array<[FieldId, FieldView]>).map(([id, field]) => {
          return [id, {
            ...field,
            show: shownFields.includes(id),
            order: respectOrder ? shownFields.indexOf(id) : field.order
          }];
        })
      ) as Record<FieldId, FieldView>,
      overrides: {
        ...currentView.overrides,
        ...overrides
      }
    };
  });
</script>

{@render children?.()}
