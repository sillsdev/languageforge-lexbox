<script lang="ts">

  import {initView, useCurrentView} from '$lib/views/view-service';
  import type {FieldId} from '$lib/entry-editor/field-data';
  import type {FieldView} from './views/view-data';

  export let shownFields: FieldId[] = [];
  export let respectOrder: boolean = false;

  const currentView = useCurrentView();
  const overrideView = initView();
  $: {
    $overrideView = {
      ...$currentView,
      fields: Object.fromEntries((Object.entries($currentView.fields) as Array<[FieldId, FieldView]>).map(([id, field]) => {
          return [id, {
            ...field,
            show: shownFields.includes(id),
            order: respectOrder ? shownFields.indexOf(id) : field.order
          }];
        })
      ) as Record<FieldId, FieldView>
    };
  }
</script>

<slot/>
