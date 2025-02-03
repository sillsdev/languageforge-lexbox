<script lang="ts">

  import {initView, useCurrentView} from '$lib/services/view-service';
  import type {FieldIds} from '$lib/entry-editor/field-data';

  export let shownFields: FieldIds[] = [];
  export let respectOrder: boolean = false;

  const currentView = useCurrentView();
  const overrideView = initView();
  $:{
    $overrideView = {
      ...$currentView,
      fields: Object.fromEntries(Object.entries($currentView.fields).map(([id, field]) => {
          return [id, {
            ...field,
            show: shownFields.includes(id),
            order: respectOrder ? shownFields.indexOf(id) : field.order
          }];
        })
      )
    };
  }
</script>

<slot/>
